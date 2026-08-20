using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using NexusEnroll.Shared;

namespace NexusEnroll.AdminService
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T @event, string routingKey) where T : IntegrationEvent;
    }

    public class RabbitMQEventPublisher : IEventPublisher, IAsyncDisposable
    {
        private readonly ILogger<RabbitMQEventPublisher> _logger;
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private readonly string _exchangeName;

        public RabbitMQEventPublisher(IConfiguration configuration, ILogger<RabbitMQEventPublisher> logger)
        {
            _logger = logger;
            _exchangeName = configuration["RabbitMQ:ExchangeName"] ?? "nexusenroll-exchange";
            
            var hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
            var userName = configuration["RabbitMQ:UserName"] ?? "guest";
            var password = configuration["RabbitMQ:Password"] ?? "guest";

            _connectionFactory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                AutomaticRecoveryEnabled = true
            };
        }

        private async Task EnsureConnectionAsync()
        {
            if (_connection == null || !_connection.IsOpen)
            {
                _connection = await _connectionFactory.CreateConnectionAsync();
                _logger.LogInformation("Connected to RabbitMQ broker at {HostName}", _connectionFactory.HostName);
            }
        }

        public async Task PublishAsync<T>(T @event, string routingKey) where T : IntegrationEvent
        {
            try
            {
                await EnsureConnectionAsync();
                await using var channel = await _connection.CreateChannelAsync();

                await channel.ExchangeDeclareAsync(
                    exchange: _exchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false
                );

                var json = JsonSerializer.Serialize(@event);
                var body = Encoding.UTF8.GetBytes(json);

                await channel.BasicPublishAsync(
                    exchange: _exchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    body: body
                );

                _logger.LogInformation("Successfully published event {EventId} ({EventType}) to routing key {RoutingKey}",
                    @event.EventId, typeof(T).Name, routingKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing integration event {EventId} of type {EventType}",
                    @event.EventId, typeof(T).Name);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection != null)
            {
                try
                {
                    await _connection.CloseAsync();
                    _connection.Dispose();
                    _logger.LogInformation("RabbitMQ connection closed gracefully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing RabbitMQ connection during dispose.");
                }
            }
        }
    }

    public class RabbitMQEventConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMQEventConsumer> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IChannel _channel;
        private readonly string _exchangeName;
        private readonly string _queueName = "admin-service-queue";

        public RabbitMQEventConsumer(
            IConfiguration configuration,
            IServiceScopeFactory scopeFactory,
            ILogger<RabbitMQEventConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _exchangeName = configuration["RabbitMQ:ExchangeName"] ?? "nexusenroll-exchange";

            var hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
            var userName = configuration["RabbitMQ:UserName"] ?? "guest";
            var password = configuration["RabbitMQ:Password"] ?? "guest";

            _connectionFactory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                AutomaticRecoveryEnabled = true
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await _channel.ExchangeDeclareAsync(
                    exchange: _exchangeName,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: stoppingToken
                );

                await _channel.QueueDeclareAsync(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: stoppingToken
                );

                var routingKeys = new[]
                {
                    "student.enrolled",
                    "student.dropped",
                    "waitlist.joined",
                    "faculty.changerequested"
                };

                foreach (var key in routingKeys)
                {
                    await _channel.QueueBindAsync(
                        queue: _queueName,
                        exchange: _exchangeName,
                        routingKey: key,
                        cancellationToken: stoppingToken
                    );
                }

                _logger.LogInformation("RabbitMQ Event Consumer started. Subscribed to student.enrolled, student.dropped, waitlist.joined, faculty.changerequested");

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;

                    _logger.LogInformation("Received message with routing key: {RoutingKey}", routingKey);

                    try
                    {
                        await ProcessMessageAsync(routingKey, message);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event with routing key {RoutingKey}", routingKey);
                        await _channel.BasicNackAsync(ea.DeliveryTag, false, true, stoppingToken);
                    }
                };

                await _channel.BasicConsumeAsync(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken
                );

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("RabbitMQ Consumer background service is stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Fatal error running RabbitMQ Consumer background service.");
            }
        }

        private async Task ProcessMessageAsync(string routingKey, string jsonMessage)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AdminDbContext>();

            switch (routingKey)
            {
                case "student.enrolled":
                    var enrolledEvent = JsonSerializer.Deserialize<StudentEnrolledEvent>(jsonMessage);
                    if (enrolledEvent != null)
                    {
                        var student = await dbContext.StudentProfiles.FindAsync(enrolledEvent.StudentId);
                        if (student != null && !student.EnrolledCourseIds.Contains(enrolledEvent.CourseId))
                        {
                            student.EnrolledCourseIds.Add(enrolledEvent.CourseId);
                        }

                        var course = await dbContext.Courses.FindAsync(enrolledEvent.CourseId);
                        if (course != null)
                        {
                            course.Enroll();
                        }

                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("Synchronized student enrollment for student {StudentId} in course {CourseId}", enrolledEvent.StudentId, enrolledEvent.CourseId);
                    }
                    break;

                case "student.dropped":
                    var droppedEvent = JsonSerializer.Deserialize<StudentDroppedEvent>(jsonMessage);
                    if (droppedEvent != null)
                    {
                        var student = await dbContext.StudentProfiles.FindAsync(droppedEvent.StudentId);
                        if (student != null)
                        {
                            student.EnrolledCourseIds.Remove(droppedEvent.CourseId);
                        }

                        var course = await dbContext.Courses.FindAsync(droppedEvent.CourseId);
                        if (course != null)
                        {
                            course.Drop();
                        }

                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("Synchronized student drop for student {StudentId} in course {CourseId}", droppedEvent.StudentId, droppedEvent.CourseId);
                    }
                    break;

                case "waitlist.joined":
                    var waitlistEvent = JsonSerializer.Deserialize<WaitlistJoinedEvent>(jsonMessage);
                    if (waitlistEvent != null)
                    {
                        var student = await dbContext.StudentProfiles.FindAsync(waitlistEvent.StudentId);
                        if (student != null && !student.WaitlistedCourseIds.Contains(waitlistEvent.CourseId))
                        {
                            student.WaitlistedCourseIds.Add(waitlistEvent.CourseId);
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Synchronized waitlist join for student {StudentId} in course {CourseId}", waitlistEvent.StudentId, waitlistEvent.CourseId);
                        }
                    }
                    break;

                case "faculty.changerequested":
                    var changeEventObj = JsonSerializer.Deserialize<CourseChangeRequestedEvent>(jsonMessage);
                    if (changeEventObj != null)
                    {
                        var existingRequest = await dbContext.CourseChangeRequests.FindAsync(changeEventObj.RequestId);
                        if (existingRequest == null)
                        {
                            var newRequest = new CourseChangeRequest
                                {
                                    RequestId = changeEventObj.RequestId,
                                    CourseId = changeEventObj.CourseId,
                                    RequestedByFacultyId = changeEventObj.FacultyId,
                                    FieldChanged = changeEventObj.FieldChanged,
                                    OldValue = changeEventObj.OldValue,
                                    NewValue = changeEventObj.NewValue,
                                    Status = ChangeRequestStatus.Pending,
                                    RequestedAt = changeEventObj.Timestamp
                                };
                            dbContext.CourseChangeRequests.Add(newRequest);
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Synchronized course change request {RequestId} to Admin DB.", changeEventObj.RequestId);
                        }
                    }
                    break;
            }
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
