using System;
using System.Collections.Generic;
using System.Linq;
using NexusEnroll.Models;
using NexusEnroll.Services;

namespace NexusEnroll.Patterns
{
    public class UniversityFacade
    {
        private readonly IStudentService _studentService;
        private readonly IFacultyService _facultyService;
        private readonly IAdminService _adminService;
        private readonly INotificationService _notificationService;
        private readonly UserFactoryManager _factoryManager;

        private readonly IDictionary<string, Course> _courses;
        private readonly IDictionary<string, User> _users;
        private readonly IDictionary<string, DegreeProgram> _programs;
        private readonly IList<CourseChangeRequest> _changeRequests;

        // Constructors ---

        public UniversityFacade(
            IStudentService studentService,
            IFacultyService facultyService,
            IAdminService adminService,
            INotificationService notificationService,
            UserFactoryManager factoryManager,
            IDictionary<string, Course> courses,
            IDictionary<string, User> users,
            IDictionary<string, DegreeProgram> programs,
            IList<CourseChangeRequest> changeRequests)
        {
            _studentService = studentService ?? throw new ArgumentNullException(nameof(studentService));
            _facultyService = facultyService ?? throw new ArgumentNullException(nameof(facultyService));
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _factoryManager = factoryManager ?? throw new ArgumentNullException(nameof(factoryManager));
            _courses = courses ?? new Dictionary<string, Course>();
            _users = users ?? new Dictionary<string, User>();
            _programs = programs ?? new Dictionary<string, DegreeProgram>();
            _changeRequests = changeRequests ?? new List<CourseChangeRequest>();
        }


        public UniversityFacade()
        {
            _courses = new Dictionary<string, Course>();
            _users = new Dictionary<string, User>();
            _programs = new Dictionary<string, DegreeProgram>();
            _changeRequests = new List<CourseChangeRequest>();

            _notificationService = new NotificationService();
            _factoryManager = new UserFactoryManager();
            _studentService = new StudentService();
            _facultyService = new FacultyService(_notificationService, _changeRequests, _courses, _users);
            _adminService = new AdminService(_courses, _users, _changeRequests, _notificationService);
        }

        // Public accessors for testing
        public INotificationService NotificationService => _notificationService;
        public UserFactoryManager FactoryManager => _factoryManager;
        public IDictionary<string, Course> Courses => _courses;
        public IDictionary<string, User> Users => _users;
        public IDictionary<string, DegreeProgram> Programs => _programs;


        public bool EnrollStudentInCourse(string studentId, string courseId, out string message, bool simulateStudentFailure = false)
        {
            // Pre-condition validations
            if (!_users.TryGetValue(studentId, out var user) || !(user is Student student))
            {
                message = $"Enrollment failed: Student '{studentId}' not found.";
                return false;
            }

            if (!_courses.TryGetValue(courseId, out var course))
            {
                message = $"Enrollment failed: Course '{courseId}' not found.";
                return false;
            }

            if (!user.IsActive)
            {
                message = $"Enrollment failed: Student '{student.FullName}' account is inactive.";
                return false;
            }

            if (student.EnrolledCourseIds.Contains(courseId))
            {
                message = $"Student is already enrolled in {course.CourseCode}.";
                return false;
            }

            // Prerequisite check
            if (!student.HasCompletedPrerequisites(course.PrerequisiteCourseIds))
            {
                message = $"Enrollment rejected: Prerequisites not met for {course.CourseCode}.";
                return false;
            }

            // Schedule conflict check against student's current courses
            var currentSchedule = _studentService.GetStudentSchedule(student, _courses.Values);
            foreach (var existing in currentSchedule)
            {
                if (course.HasTimeConflictWith(existing))
                {
                    message = $"Enrollment rejected: Time conflict between {course.CourseCode} and {existing.CourseCode}.";
                    return false;
                }
            }

            // Check seat availability
            if (!course.HasAvailableSeats())
            {
                message = $"Course {course.CourseCode} is full ({course.Capacity}/{course.Capacity}). Join waitlist instead.";
                return false;
            }

            bool seatReserved = course.Enroll();
            if (!seatReserved)
            {
                message = $"Saga Step 1 failed: Could not reserve seat in {course.CourseCode}.";
                return false;
            }

            try
            {
                // Failure hook
                if (simulateStudentFailure)
                {
                    throw new InvalidOperationException("Simulated Student Service network outage / database failure.");
                }

                // Commit student state
                student.EnrollInCourse(course.CourseId);

                // If student was on waitlist, remove them
                if (course.IsWaitlisted(student.UserId))
                {
                    course.RemoveFromWaitlist(student.UserId);
                    student.WaitlistedCourseIds.Remove(course.CourseId);
                }

                // Register with FacultyService so roster stays in sync
                var enrollmentRecord = new Enrollment(Guid.NewGuid().ToString(), student.UserId, course.CourseId);
                _facultyService.AddEnrollment(enrollmentRecord);
                _facultyService.AddStudent(student);
            }
            catch (Exception ex)
            {
                course.Drop();

                _notificationService.NotifyEvent("SagaCompensationExecuted", new Dictionary<string, object>
                {
                    { "StudentId", studentId },
                    { "CourseId", courseId },
                    { "Reason", ex.Message },
                    { "Action", "Course seat reservation rolled back" }
                });

                message = $"[SAGA ROLLBACK] Step 2 failed ({ex.Message}). Compensating action executed: Seat released for {course.CourseCode}.";
                return false;
            }

            _notificationService.NotifyEvent("StudentEnrolledEvent", new Dictionary<string, object>
            {
                { "StudentId", student.UserId },
                { "StudentName", student.FullName },
                { "RecipientEmail", student.Email },
                { "RecipientPhone", student.Phone },
                { "CourseId", course.CourseId },
                { "CourseCode", course.CourseCode },
                { "CourseName", course.CourseName },
                { "Message", $"Successfully enrolled in {course.CourseCode} - {course.CourseName}." }
            });

            message = $"Successfully enrolled {student.FullName} in {course.CourseCode} - {course.CourseName}.";
            return true;
        }

        public IEnumerable<Course> BrowseCatalog(string department = null, string keyword = null, string instructorName = null)
        {
            return _studentService.BrowseCatalogue(_courses.Values, department, keyword, instructorName);
        }

        public Course GetCourseDetails(string courseId)
        {
            return _studentService.GetCourseDetails(_courses.Values, courseId);
        }

        public bool JoinWaitlist(string studentId, string courseId, out string message)
        {
            if (!_users.TryGetValue(studentId, out var user) || !(user is Student student))
            {
                message = $"Student '{studentId}' not found.";
                return false;
            }

            if (!_courses.TryGetValue(courseId, out var course))
            {
                message = $"Course '{courseId}' not found.";
                return false;
            }

            bool joined = _studentService.JoinWaitlist(student, course, out message);
            if (joined)
            {
                _notificationService.NotifyEvent("WaitlistJoinedEvent", new Dictionary<string, object>
                {
                    { "StudentId", student.UserId },
                    { "CourseId", course.CourseId },
                    { "RecipientEmail", student.Email },
                    { "RecipientPhone", student.Phone },
                    { "Position", course.Waitlist.Count }
                });
            }

            return joined;
        }


        public bool DropCourseAndPromoteWaitlist(string studentId, string courseId, out string message)
        {
            if (!_users.TryGetValue(studentId, out var user) || !(user is Student student))
            {
                message = $"Student '{studentId}' not found.";
                return false;
            }

            if (!_courses.TryGetValue(courseId, out var course))
            {
                message = $"Course '{courseId}' not found.";
                return false;
            }

            bool dropped = _studentService.DropCourse(student, course, out message);
            if (!dropped) return false;

            _facultyService.RemoveEnrollment(studentId, courseId);

            // Notify dropping student
            _notificationService.NotifyEvent("CourseDroppedEvent", new Dictionary<string, object>
            {
                { "StudentId", student.UserId },
                { "CourseId", course.CourseId },
                { "RecipientEmail", student.Email },
                { "RecipientPhone", student.Phone },
                { "Message", $"You dropped {course.CourseCode}." }
            });

            // Check if there is someone on the waitlist to promote
            if (course.Waitlist.Count > 0)
            {
                string nextStudentId = course.PopNextWaitlistedStudent();
                if (nextStudentId != null && _users.TryGetValue(nextStudentId, out var nextUser) && nextUser is Student promotedStudent)
                {
                    promotedStudent.WaitlistedCourseIds.Remove(course.CourseId);

                    // Re-enroll promoted student via Saga
                    if (EnrollStudentInCourse(promotedStudent.UserId, course.CourseId, out string promoMessage))
                    {
                        _notificationService.NotifyEvent("WaitlistPromotedEvent", new Dictionary<string, object>
                        {
                            { "StudentId", promotedStudent.UserId },
                            { "CourseId", course.CourseId },
                            { "RecipientEmail", promotedStudent.Email },
                            { "RecipientPhone", promotedStudent.Phone },
                            { "Message", $"A seat opened in {course.CourseCode}! You have been promoted from waitlist to enrolled." }
                        });
                        message += $" Auto-promoted waitlisted student: {promotedStudent.FullName}.";
                    }
                    else
                    {
                        // Restore waitlist status if promotion failed
                        course.AddToWaitlist(nextStudentId);
                        promotedStudent.WaitlistForCourse(course.CourseId);
                    }
                }
            }

            return true;
        }

        public IEnumerable<Course> GetStudentSchedule(string studentId)
        {
            if (_users.TryGetValue(studentId, out var user) && user is Student student)
                return _studentService.GetStudentSchedule(student, _courses.Values);

            return Enumerable.Empty<Course>();
        }

        public IEnumerable<CourseRecord> GetAcademicHistory(string studentId)
        {
            if (_users.TryGetValue(studentId, out var user) && user is Student student)
                return _studentService.GetAcademicHistory(student);

            return Enumerable.Empty<CourseRecord>();
        }

        public IEnumerable<string> GetDegreeAudit(string studentId, string programId)
        {
            if (_users.TryGetValue(studentId, out var user) && user is Student student &&
                _programs.TryGetValue(programId, out var program))
            {
                return _studentService.GetRemainingRequiredCourses(student, program);
            }

            return Enumerable.Empty<string>();
        }


        public GradeSubmissionResult SubmitFacultyGrades(string facultyId, string courseId, Dictionary<string, string> rawGrades)
        {
            return _facultyService.SubmitGrades(facultyId, courseId, rawGrades);
        }

        public GradeApprovalResult ApproveCourseGrades(string courseId)
        {
            return _facultyService.ApproveGrades(courseId);
        }

        public List<Student> GetClassRoster(string facultyId, string courseId)
        {
            return _facultyService.GetClassRoster(facultyId, courseId);
        }

        public CourseChangeRequest RequestCourseUpdate(string facultyId, string courseId, string fieldChanged, string newValue)
        {
            return _facultyService.RequestCourseUpdate(facultyId, courseId, fieldChanged, newValue);
        }

        public List<Course> GetFacultyTeachingSchedule(string facultyId)
        {
            return _facultyService.GetTeachingSchedule(facultyId);
        }

        // Admin use case ---
        public EnrollmentReport GenerateDepartmentReport(string department, double utilizationThresholdPercent = 90.0)
        {
            return _adminService.GenerateEnrollmentReport(department, utilizationThresholdPercent);
        }

        public GradeSubmissionStatus GetCourseGradeStatus(string courseId)
        {
            return _facultyService.GetGradeStatus(courseId);
        }

        public List<CourseChangeRequest> GetFacultyChangeRequests(string facultyId)
        {
            return _facultyService.GetChangeRequestsFor(facultyId);
        }

        public IEnumerable<CourseChangeRequest> GetPendingChangeRequests()
        {
            return _adminService.GetPendingChangeRequests();
        }

        public void AttachObserver(INotificationObserver observer)
        {
            _notificationService.AddObserver(observer);
        }

        public bool ApproveCourseChange(string requestId, string adminId)
        {
            return _adminService.ApproveChangeRequest(requestId, adminId);
        }

        public bool RejectCourseChange(string requestId, string adminId, string reason = null)
        {
            return _adminService.RejectChangeRequest(requestId, adminId, reason);
        }

        public bool SetUserActiveStatus(string userId, bool active)
        {
            return active ? _adminService.ActivateUser(userId) : _adminService.DeactivateUser(userId);
        }

        public bool ForceEnrollStudent(string studentId, string courseId)
        {
            bool success = _adminService.ForceEnroll(studentId, courseId);
            if (success && _users.TryGetValue(studentId, out var u) && u is Student s)
            {
                _facultyService.AddStudent(s);
                _facultyService.AddEnrollment(new Enrollment(Guid.NewGuid().ToString(), studentId, courseId));
            }
            return success;
        }

        public Student CreateStudentAccount(string userId, string fullName, string email, string phone, string studentNumber, string programId, int enrolledYear)
        {
            var student = _adminService.CreateStudentAccount(_factoryManager, userId, fullName, email, phone, studentNumber, programId, enrolledYear);
            _facultyService.AddStudent(student);
            return student;
        }

        public Faculty CreateFacultyAccount(string userId, string fullName, string email, string phone, string employeeNumber, string department, string rank)
        {
            var faculty = _adminService.CreateFacultyAccount(_factoryManager, userId, fullName, email, phone, employeeNumber, department, rank);
            _facultyService.RegisterFaculty(faculty);
            return faculty;
        }

        public bool DeleteStudentAccount(string studentId)
        {
            return _adminService.DeleteStudentAccount(studentId);
        }

        public bool DeleteFacultyAccount(string facultyId)
        {
            return _adminService.DeleteFacultyAccount(facultyId);
        }

        // Factory Method ---
        public Student CreateAndRegisterStudent(string userId, string fullName, string email, string phone,
                                                string studentNumber, string programId, int enrolledYear)
        {
            var student = _factoryManager.CreateStudent(userId, fullName, email, phone, studentNumber, programId, enrolledYear);
            _users[userId] = student;
            _facultyService.AddStudent(student);
            return student;
        }

        public Faculty CreateAndRegisterFaculty(string userId, string fullName, string email, string phone,
                                                string employeeNumber, string department, string rank)
        {
            var faculty = _factoryManager.CreateFaculty(userId, fullName, email, phone, employeeNumber, department, rank);
            _users[userId] = faculty;
            _facultyService.RegisterFaculty(faculty);
            return faculty;
        }

        public Admin CreateAndRegisterAdmin(string userId, string fullName, string email, string phone,
                                            string staffNumber, string office, AdminScope scope = AdminScope.Full)
        {
            var admin = _factoryManager.CreateAdmin(userId, fullName, email, phone, staffNumber, office, scope);
            _users[userId] = admin;
            return admin;
        }

        public Course AddCourse(Course course)
        {
            if (course == null) throw new ArgumentNullException(nameof(course));
            _adminService.CreateCourse(course);
            _facultyService.AddCourse(course);
            return course;
        }

        public DegreeProgram AddDegreeProgram(DegreeProgram program)
        {
            if (program == null) throw new ArgumentNullException(nameof(program));
            _adminService.CreateDegreeProgram(program);
            _programs[program.ProgramId] = program;
            return program;
        }
    }
}
