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

namespace NexusEnroll.StudentService
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
        private readonly string _queueName = "student-service-queue";

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
                    "student.created",
                    "student.deleted",
                    "user.statuschanged",
                    "course.created",
                    "course.updated",
                    "course.deleted",
                    "grades.submitted",
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

                _logger.LogInformation("StudentService Event Consumer started. Subscribed to administrative and grading events.");

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (sender, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var routingKey = ea.RoutingKey;

                    _logger.LogInformation("StudentService received event: {RoutingKey}", routingKey);

                    try
                    {
                        await ProcessMessageAsync(routingKey, message);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing event {RoutingKey} in StudentService", routingKey);
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
                _logger.LogCritical(ex, "Fatal error running RabbitMQ Consumer background service in StudentService.");
            }
        }

        private async Task ProcessMessageAsync(string routingKey, string jsonMessage)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<StudentDbContext>();

            switch (routingKey)
            {
                case "student.created":
                    var studentEvent = JsonSerializer.Deserialize<StudentCreatedEvent>(jsonMessage);
                    if (studentEvent != null)
                    {
                        var exists = await dbContext.Students.AnyAsync(s => s.UserId == studentEvent.UserId);
                        if (!exists)
                        {
                            dbContext.Students.Add(new Student
                            {
                                UserId = studentEvent.UserId,
                                FullName = studentEvent.FullName,
                                Email = studentEvent.Email,
                                Phone = studentEvent.Phone,
                                StudentNumber = studentEvent.StudentNumber,
                                ProgramId = studentEvent.ProgramId,
                                EnrolledYear = studentEvent.EnrolledYear,
                                IsActive = true
                            });
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Synced new student profile: {UserId}", studentEvent.UserId);
                        }
                    }
                    break;

                case "student.deleted":
                    var deleteStudentEvent = JsonSerializer.Deserialize<StudentDeletedEvent>(jsonMessage);
                    if (deleteStudentEvent != null)
                    {
                        var student = await dbContext.Students
                            .Include(s => s.AcademicHistory)
                            .FirstOrDefaultAsync(s => s.UserId == deleteStudentEvent.StudentId);
                        if (student != null)
                        {
                            dbContext.Students.Remove(student);
                            
                            var studentEnrollments = await dbContext.Enrollments
                                .Where(e => e.StudentId == deleteStudentEvent.StudentId)
                                .ToListAsync();
                            dbContext.Enrollments.RemoveRange(studentEnrollments);

                            var waitlistedCourses = await dbContext.Courses
                                .Where(c => c.Waitlist.Contains(deleteStudentEvent.StudentId))
                                .ToListAsync();
                            foreach (var course in waitlistedCourses)
                            {
                                course.RemoveFromWaitlist(deleteStudentEvent.StudentId);
                            }

                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Deleted student profile & synced cascade cleanups: {UserId}", deleteStudentEvent.StudentId);
                        }
                    }
                    break;

                case "user.statuschanged":
                    var statusEvent = JsonSerializer.Deserialize<UserStatusChangedEvent>(jsonMessage);
                    if (statusEvent != null && statusEvent.Role == "Student")
                    {
                        var student = await dbContext.Students.FindAsync(statusEvent.UserId);
                        if (student != null)
                        {
                            student.IsActive = statusEvent.IsActive;
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Synced student {UserId} active state to {IsActive}", statusEvent.UserId, statusEvent.IsActive);
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
                                PrerequisiteCourseIds = courseEvent.PrerequisiteCourseIds ?? new List<string>()
                            });
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
                            course.PrerequisiteCourseIds = updateCourseEvent.PrerequisiteCourseIds ?? new List<string>();
                            
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
                            dbContext.Courses.Remove(course);
                            await dbContext.SaveChangesAsync();
                            _logger.LogInformation("Deleted course replica: {CourseId}", deleteCourseEvent.CourseId);
                        }
                    }
                    break;

                case "grades.submitted":
                    var gradesSubmittedEvent = JsonSerializer.Deserialize<GradesSubmittedEvent>(jsonMessage);
                    if (gradesSubmittedEvent != null && gradesSubmittedEvent.Grades != null)
                    {
                        foreach (var gradeEntry in gradesSubmittedEvent.Grades)
                        {
                            var studentId = gradeEntry.Key;
                            var rawGrade = gradeEntry.Value;

                            if (Enum.TryParse<Grade>(rawGrade, true, out var parsedGrade))
                            {
                                var enrollment = await dbContext.Enrollments
                                    .FirstOrDefaultAsync(e => e.StudentId == studentId && 
                                                              e.CourseId == gradesSubmittedEvent.CourseId && 
                                                              e.Status == EnrollmentStatus.Enrolled);
                                if (enrollment != null)
                                {
                                    enrollment.SubmitGradePending(parsedGrade);
                                    _logger.LogInformation("Synced pending grade for {StudentId} in course {CourseId}", studentId, gradesSubmittedEvent.CourseId);
                                }
                            }
                        }
                        await dbContext.SaveChangesAsync();
                    }
                    break;

                case "grades.approved":
                    var gradesApprovedEvent = JsonSerializer.Deserialize<GradesApprovedEvent>(jsonMessage);
                    if (gradesApprovedEvent != null)
                    {
                        var pendingEnrollments = await dbContext.Enrollments
                            .Where(e => e.CourseId == gradesApprovedEvent.CourseId && 
                                        e.SubmissionStatus == GradeSubmissionStatus.Pending)
                            .ToListAsync();

                        var course = await dbContext.Courses.FindAsync(gradesApprovedEvent.CourseId);

                        foreach (var enrollment in pendingEnrollments)
                        {
                            enrollment.FinaliseGrade();

                            var student = await dbContext.Students
                                .Include(s => s.AcademicHistory)
                                .FirstOrDefaultAsync(s => s.UserId == enrollment.StudentId);
                            
                            if (student != null && course != null && enrollment.Grade.HasValue)
                            {
                                student.AcademicHistory.Add(new CourseRecord
                                {
                                    StudentId = student.UserId,
                                    CourseId = course.CourseId,
                                    CourseCode = course.CourseCode,
                                    CourseName = course.CourseName,
                                    Semester = course.Semester,
                                    Grade = enrollment.Grade.Value,
                                    Credits = course.Credits,
                                    CompletedAt = DateTime.UtcNow
                                });
                                _logger.LogInformation("Appended completed course record to student academic history: {StudentId} -> {CourseId}", student.UserId, course.CourseId);
                            }
                        }
                        await dbContext.SaveChangesAsync();
                        _logger.LogInformation("Finalized and synced approved grades for course: {CourseId}", gradesApprovedEvent.CourseId);
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
