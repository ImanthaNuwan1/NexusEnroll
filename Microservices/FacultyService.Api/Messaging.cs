using System;
using System.Collections.Generic;
using System.Linq;
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

namespace NexusEnroll.FacultyService
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
        private readonly string _queueName = "faculty-service-queue";

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
                    "faculty.created",
                    "faculty.deleted",
                    "user.statuschanged",
                    "course.created",
                    "course.updated",
                    "course.deleted",
                    "student.enrolled",
                    "student.dropped",
                    "course.changeapproved",
                    "course.changerejected",
                    "grades.approved"
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

                _logger.LogInformation("FacultyService Event Consumer started. Subscribed to routing keys.");

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;

                    _logger.LogInformation("FacultyService received event: {RoutingKey}", routingKey);

                    try
                    {
                        await ProcessMessageAsync(routingKey, message);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event {RoutingKey} in FacultyService", routingKey);
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
                _logger.LogCritical(ex, "Fatal error running RabbitMQ Consumer background service in FacultyService.");
            }
        }

        private async Task ProcessMessageAsync(string routingKey, string jsonMessage)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FacultyDbContext>();

            switch (routingKey)
            {
                case "faculty.created":
                    var facultyEvent = JsonSerializer.Deserialize<FacultyCreatedEvent>(jsonMessage);
                    if (facultyEvent != null)
                    {
                        var exists = await dbContext.Faculties.AnyAsync(f => f.UserId == facultyEvent.UserId);
                        if (!exists)
                        {
                            dbContext.Faculties.Add(new Faculty
                            {
                                UserId = facultyEvent.UserId,
                                FullName = facultyEvent.FullName,
                                Email = facultyEvent.Email,
                                Phone = facultyEvent.Phone,
                                EmployeeNumber = facultyEvent.EmployeeNumber,
                                Department = facultyEvent.Department,
                                Rank = facultyEvent.Rank,
                                IsActive = true
                            });
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Synced new faculty profile: {UserId}", facultyEvent.UserId);
                        }
                    }
                    break;

                case "faculty.deleted":
                    var deleteFacultyEvent = JsonSerializer.Deserialize<FacultyDeletedEvent>(jsonMessage);
                    if (deleteFacultyEvent != null)
                    {
                        var faculty = await dbContext.Faculties.FindAsync(deleteFacultyEvent.FacultyId);
                        if (faculty != null)
                        {
                            dbContext.Faculties.Remove(faculty);
                            
                            // Nullify instructor assignments in local courses
                            var assignedCourses = await dbContext.Courses
                                .Where(c => c.InstructorId == deleteFacultyEvent.FacultyId)
                                .ToListAsync();
                            foreach (var c in assignedCourses)
                            {
                                c.InstructorId = null;
                                c.InstructorName = "Unassigned";
                            }

                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Deleted faculty profile & synced unassignments: {UserId}", deleteFacultyEvent.FacultyId);
                        }
                    }
                    break;

                case "user.statuschanged":
                    var statusEvent = JsonSerializer.Deserialize<UserStatusChangedEvent>(jsonMessage);
                    if (statusEvent != null && statusEvent.Role == "Faculty")
                    {
                        var faculty = await dbContext.Faculties.FindAsync(statusEvent.UserId);
                        if (faculty != null)
                        {
                            faculty.IsActive = statusEvent.IsActive;
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Synced faculty {UserId} active state to {IsActive}", statusEvent.UserId, statusEvent.IsActive);
                        }
                    }
                    break;

                case "course.created":
                    var courseEvent = JsonSerializer.Deserialize<CourseCreatedEvent>(jsonMessage);
                    if (courseEvent != null)
                    {
                        var exists = await dbContext.Courses.AnyAsync(c => c.CourseId == courseEvent.CourseId);
                        if (!exists)
                        {
                            dbContext.Courses.Add(new Course
                            {
                                CourseId = courseEvent.CourseId,
                                CourseCode = courseEvent.CourseCode,
                                CourseName = courseEvent.CourseName,
                                Description = courseEvent.Description,
                                Department = courseEvent.Department,
                                InstructorId = courseEvent.InstructorId,
                                InstructorName = courseEvent.InstructorName,
                                Credits = courseEvent.Credits,
                                Semester = courseEvent.Semester,
                                Capacity = courseEvent.Capacity,
                                EnrolledCount = courseEvent.EnrolledCount,
                                Days = courseEvent.Days,
                                StartTime = courseEvent.StartTime,
                                EndTime = courseEvent.EndTime,
                                Location = courseEvent.Location,
                                Status = courseEvent.Status,
                                GradeStatus = GradeSubmissionStatus.NotSubmitted
                            });

                            if (!string.IsNullOrEmpty(courseEvent.InstructorId))
                            {
                                var faculty = await dbContext.Faculties.FindAsync(courseEvent.InstructorId);
                                faculty?.AssignCourse(courseEvent.CourseId);
                            }

                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Synced course replica: {CourseId}", courseEvent.CourseId);
                        }
                    }
                    break;

                case "course.updated":
                    var updateCourseEvent = JsonSerializer.Deserialize<CourseUpdatedEvent>(jsonMessage);
                    if (updateCourseEvent != null)
                    {
                        var course = await dbContext.Courses.FindAsync(updateCourseEvent.CourseId);
                        if (course != null)
                        {
                            var oldInstructorId = course.InstructorId;
                            
                            course.CourseCode = updateCourseEvent.CourseCode;
                            course.CourseName = updateCourseEvent.CourseName;
                            course.Description = updateCourseEvent.Description;
                            course.Department = updateCourseEvent.Department;
                            course.InstructorId = updateCourseEvent.InstructorId;
                            course.InstructorName = updateCourseEvent.InstructorName;
                            course.Credits = updateCourseEvent.Credits;
                            course.Semester = updateCourseEvent.Semester;
                            course.Capacity = updateCourseEvent.Capacity;
                            course.EnrolledCount = updateCourseEvent.EnrolledCount;
                            course.Days = updateCourseEvent.Days;
                            course.StartTime = updateCourseEvent.StartTime;
                            course.EndTime = updateCourseEvent.EndTime;
                            course.Location = updateCourseEvent.Location;
                            course.Status = updateCourseEvent.Status;

                            // Sync instructor assignments in local faculties
                            if (oldInstructorId != updateCourseEvent.InstructorId)
                            {
                                if (!string.IsNullOrEmpty(oldInstructorId))
                                {
                                    var oldFac = await dbContext.Faculties.FindAsync(oldInstructorId);
                                    oldFac?.UnassignCourse(course.CourseId);
                                }
                                if (!string.IsNullOrEmpty(updateCourseEvent.InstructorId))
                                {
                                    var newFac = await dbContext.Faculties.FindAsync(updateCourseEvent.InstructorId);
                                    newFac?.AssignCourse(course.CourseId);
                                }
                            }

                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Updated course replica: {CourseId}", updateCourseEvent.CourseId);
                        }
                    }
                    break;

                case "course.deleted":
                    var deleteCourseEvent = JsonSerializer.Deserialize<CourseDeletedEvent>(jsonMessage);
                    if (deleteCourseEvent != null)
                    {
                        var course = await dbContext.Courses.FindAsync(deleteCourseEvent.CourseId);
                        if (course != null)
                        {
                            if (!string.IsNullOrEmpty(course.InstructorId))
                            {
                                var fac = await dbContext.Faculties.FindAsync(course.InstructorId);
                                fac?.UnassignCourse(course.CourseId);
                            }

                            dbContext.Courses.Remove(course);
                            
                            // Remove all roster entries for deleted course
                            var roster = await dbContext.RosterEntries.Where(re => re.CourseId == deleteCourseEvent.CourseId).ToListAsync();
                            dbContext.RosterEntries.RemoveRange(roster);

                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Deleted course replica & cleanups: {CourseId}", deleteCourseEvent.CourseId);
                        }
                    }
                    break;

                case "student.enrolled":
                    var enrolledEvent = JsonSerializer.Deserialize<StudentEnrolledEvent>(jsonMessage);
                    if (enrolledEvent != null)
                    {
                        var exists = await dbContext.RosterEntries
                            .AnyAsync(re => re.CourseId == enrolledEvent.CourseId && re.StudentId == enrolledEvent.StudentId);
                        
                        if (!exists)
                        {
                            dbContext.RosterEntries.Add(new RosterEntry
                            {
                                CourseId = enrolledEvent.CourseId,
                                StudentId = enrolledEvent.StudentId,
                                StudentName = enrolledEvent.StudentName,
                                StudentNumber = "S" + enrolledEvent.StudentId.Substring(Math.Max(0, enrolledEvent.StudentId.Length - 4)), // fallback student number format
                                Grade = null
                            });

                            // Increment enrolled count on local course replica
                            var course = await dbContext.Courses.FindAsync(enrolledEvent.CourseId);
                            if (course != null)
                            {
                                course.EnrolledCount++;
                            }

                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Synced roster enrollment: {StudentId} -> {CourseId}", enrolledEvent.StudentId, enrolledEvent.CourseId);
                        }
                    }
                    break;

                case "student.dropped":
                    var droppedEvent = JsonSerializer.Deserialize<StudentDroppedEvent>(jsonMessage);
                    if (droppedEvent != null)
                    {
                        var entry = await dbContext.RosterEntries
                            .FirstOrDefaultAsync(re => re.CourseId == droppedEvent.CourseId && re.StudentId == droppedEvent.StudentId);
                        
                        if (entry != null)
                        {
                            dbContext.RosterEntries.Remove(entry);

                            var course = await dbContext.Courses.FindAsync(droppedEvent.CourseId);
                            if (course != null && course.EnrolledCount > 0)
                            {
                                course.EnrolledCount--;
                            }

                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Synced roster drop: {StudentId} -> {CourseId}", droppedEvent.StudentId, droppedEvent.CourseId);
                        }
                    }
                    break;

                case "course.changeapproved":
                    var approvedEvent = JsonSerializer.Deserialize<CourseChangeApprovedEvent>(jsonMessage);
                    if (approvedEvent != null)
                    {
                        var request = await dbContext.CourseChangeRequests.FindAsync(approvedEvent.RequestId);
                        if (request != null)
                        {
                            request.Status = ChangeRequestStatus.Approved;
                            
                            var course = await dbContext.Courses.FindAsync(approvedEvent.CourseId);
                            if (course != null)
                            {
                                ApplyFieldChange(course, approvedEvent.FieldChanged, approvedEvent.NewValue);
                            }

                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Synced approved change request state: {RequestId}", approvedEvent.RequestId);
                        }
                    }
                    break;

                case "course.changerejected":
                    var rejectedEvent = JsonSerializer.Deserialize<CourseChangeRejectedEvent>(jsonMessage);
                    if (rejectedEvent != null)
                    {
                        var request = await dbContext.CourseChangeRequests.FindAsync(rejectedEvent.RequestId);
                        if (request != null)
                        {
                            request.Status = ChangeRequestStatus.Rejected;
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Synced rejected change request state: {RequestId}", rejectedEvent.RequestId);
                        }
                    }
                    break;

                case "grades.approved":
                    var approvedGradesEvent = JsonSerializer.Deserialize<GradesApprovedEvent>(jsonMessage);
                    if (approvedGradesEvent != null)
                    {
                        var course = await dbContext.Courses.FindAsync(approvedGradesEvent.CourseId);
                        if (course != null)
                        {
                            course.GradeStatus = GradeSubmissionStatus.Submitted;
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Synced approved/finalized grades status for course {CourseId}", approvedGradesEvent.CourseId);
                        }
                    }
                    break;
            }
        }

        private static void ApplyFieldChange(Course course, string field, string newValue)
        {
            switch (field)
            {
                case "Capacity":
                    if (int.TryParse(newValue, out var cap)) course.Capacity = cap;
                    break;
                case "Description":
                    course.Description = newValue;
                    break;
                case "CourseName":
                    course.CourseName = newValue;
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
