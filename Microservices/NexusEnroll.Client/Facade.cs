using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using NexusEnroll.Models;
using NexusEnroll.Services;

namespace NexusEnroll.Patterns
{
    // Facade to handle all API communications with Gateway on port 5000.
    public class UniversityFacade
    {
        private readonly HttpClient _client;
        private readonly INotificationService _notificationService;
        private readonly UserFactoryManager _factoryManager;

        private readonly Dictionary<string, Course> _courses = new();
        private readonly Dictionary<string, User> _users = new();
        private readonly Dictionary<string, DegreeProgram> _programs = new();

        public INotificationService NotificationService => _notificationService;
        public IDictionary<string, Course> Courses => _courses;
        public IDictionary<string, User> Users => _users;
        public IDictionary<string, DegreeProgram> Programs => _programs;

        public UniversityFacade()
        {
            _notificationService = new NotificationService();
            _factoryManager = new UserFactoryManager();
            _client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
            try { RefreshCache(); } catch { }
        }

        public void RefreshCache()
        {
            try
            {
                var usersTask = _client.GetAsync("/api/admin/users");
                var coursesTask = _client.GetAsync("/api/admin/courses");
                var progTask = _client.GetAsync("/api/admin/programs");

                System.Threading.Tasks.Task.WaitAll(usersTask, coursesTask, progTask);

                var userRes = usersTask.Result;
                if (userRes.IsSuccessStatusCode)
                {
                    var res = userRes.Content.ReadFromJsonAsync<UsersResponseDto>().GetAwaiter().GetResult();
                    _users.Clear();
                    foreach (var s in res?.Students ?? new())
                    {
                        var student = _factoryManager.CreateStudent(s.UserId, s.FullName, s.Email, s.Phone, s.StudentNumber, s.ProgramId, s.EnrolledYear);
                        student.IsActive = s.IsActive;
                        foreach (var c in s.EnrolledCourseIds ?? new()) student.EnrollInCourse(c);
                        foreach (var c in s.WaitlistedCourseIds ?? new()) student.WaitlistedCourseIds.Add(c);
                        _users[s.UserId] = student;
                    }
                    foreach (var f in res?.Faculty ?? new())
                    {
                        var faculty = _factoryManager.CreateFaculty(f.UserId, f.FullName, f.Email, f.Phone, f.EmployeeNumber, f.Department, f.Rank);
                        faculty.IsActive = f.IsActive;
                        var courses = f.TeachingCourseIds ?? f.AssignedCourseIds ?? new();
                        foreach (var c in courses) faculty.AssignCourse(c);
                        _users[f.UserId] = faculty;
                    }
                    foreach (var a in res?.Admins ?? new())
                    {
                        var admin = _factoryManager.CreateAdmin(a.UserId, a.FullName, a.Email, a.Phone, a.StaffNumber, a.Office, (AdminScope)a.Scope);
                        admin.IsActive = a.IsActive;
                        _users[a.UserId] = admin;
                    }
                }

                var courseRes = coursesTask.Result;
                if (courseRes.IsSuccessStatusCode)
                {
                    var list = courseRes.Content.ReadFromJsonAsync<List<CourseDto>>().GetAwaiter().GetResult();
                    _courses.Clear();
                    foreach (var c in list ?? new())
                    {
                        _courses[c.CourseId] = MapToCourse(c);
                    }
                }

                var progRes = progTask.Result;
                if (progRes.IsSuccessStatusCode)
                {
                    var list = progRes.Content.ReadFromJsonAsync<List<ProgramDto>>().GetAwaiter().GetResult();
                    _programs.Clear();
                    foreach (var p in list ?? new())
                    {
                        var program = new DegreeProgram(p.ProgramId, p.ProgramName, p.Department);
                        foreach (var req in p.RequiredCourseIds ?? new()) program.AddRequiredCourse(req);
                        foreach (var el in p.ElectiveCourseIds ?? new()) program.AddElectiveCourse(el);
                        _programs[p.ProgramId] = program;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cache warning: {ex.Message}");
            }
        }

        public bool EnrollStudentInCourse(string studentId, string courseId, out string message, bool simulateStudentFailure = false)
        {
            var res = _client.PostAsJsonAsync("/api/students/enroll", new { studentId, courseId }).GetAwaiter().GetResult();
            var content = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            RefreshCache();
            using var doc = JsonDocument.Parse(content);
            if (res.IsSuccessStatusCode)
            {
                message = doc.RootElement.GetProperty("message").GetString();
                _notificationService.NotifyEvent("StudentEnrolledEvent", new Dictionary<string, object> { { "StudentId", studentId }, { "CourseId", courseId }, { "Message", message } });
                return true;
            }
            message = doc.RootElement.TryGetProperty("error", out var err) ? err.GetString() : "Enrollment failed.";
            return false;
        }

        public bool DropCourseAndPromoteWaitlist(string studentId, string courseId, out string message)
        {
            var res = _client.PostAsJsonAsync("/api/students/drop", new { studentId, courseId }).GetAwaiter().GetResult();
            var content = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            RefreshCache();
            using var doc = JsonDocument.Parse(content);
            if (res.IsSuccessStatusCode)
            {
                message = doc.RootElement.GetProperty("message").GetString();
                _notificationService.NotifyEvent("CourseDroppedEvent", new Dictionary<string, object> { { "StudentId", studentId }, { "CourseId", courseId }, { "Message", message } });
                return true;
            }
            message = doc.RootElement.TryGetProperty("error", out var err) ? err.GetString() : "Drop failed.";
            return false;
        }

        public bool JoinWaitlist(string studentId, string courseId, out string message)
        {
            var res = _client.PostAsJsonAsync("/api/students/waitlist", new { studentId, courseId }).GetAwaiter().GetResult();
            var content = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            RefreshCache();
            using var doc = JsonDocument.Parse(content);
            if (res.IsSuccessStatusCode)
            {
                message = doc.RootElement.GetProperty("message").GetString();
                _notificationService.NotifyEvent("WaitlistJoinedEvent", new Dictionary<string, object> { { "StudentId", studentId }, { "CourseId", courseId }, { "Message", message } });
                return true;
            }
            message = doc.RootElement.TryGetProperty("error", out var err) ? err.GetString() : "Waitlist failed.";
            return false;
        }

        public IEnumerable<Course> BrowseCatalog(string department = null, string keyword = null, string instructorName = null)
        {
            var url = $"/api/students/catalog?department={Uri.EscapeDataString(department ?? "")}&keyword={Uri.EscapeDataString(keyword ?? "")}&instructor={Uri.EscapeDataString(instructorName ?? "")}";
            var res = _client.GetAsync(url).GetAwaiter().GetResult();
            if (res.IsSuccessStatusCode)
            {
                var list = res.Content.ReadFromJsonAsync<List<CourseDto>>().GetAwaiter().GetResult();
                return list?.Select(MapToCourse) ?? Enumerable.Empty<Course>();
            }
            return Enumerable.Empty<Course>();
        }

        public Course GetCourseDetails(string courseId)
        {
            var res = _client.GetAsync($"/api/students/courses/{courseId}").GetAwaiter().GetResult();
            if (res.IsSuccessStatusCode)
            {
                var d = res.Content.ReadFromJsonAsync<CourseDto>().GetAwaiter().GetResult();
                return d != null ? MapToCourse(d) : null;
            }
            return null;
        }

        public IEnumerable<Course> GetStudentSchedule(string studentId)
        {
            var res = _client.GetAsync($"/api/students/{studentId}/schedule").GetAwaiter().GetResult();
            if (res.IsSuccessStatusCode)
            {
                var list = res.Content.ReadFromJsonAsync<List<CourseDto>>().GetAwaiter().GetResult();
                return list?.Select(MapToCourse) ?? Enumerable.Empty<Course>();
            }
            return Enumerable.Empty<Course>();
        }

        public IEnumerable<CourseRecord> GetAcademicHistory(string studentId)
        {
            var res = _client.GetAsync($"/api/students/{studentId}/history").GetAwaiter().GetResult();
            if (res.IsSuccessStatusCode)
            {
                var list = res.Content.ReadFromJsonAsync<List<CourseRecordDto>>().GetAwaiter().GetResult();
                return list?.Select(d => new CourseRecord(d.CourseId, d.CourseCode, d.CourseName, d.Semester, (Grade)Enum.Parse(typeof(Grade), d.Grade), d.Credits)) ?? Enumerable.Empty<CourseRecord>();
            }
            return Enumerable.Empty<CourseRecord>();
        }

        public IEnumerable<string> GetDegreeAudit(string studentId, string programId)
        {
            var res = _client.GetAsync($"/api/students/{studentId}/degree-audit?programId={programId}").GetAwaiter().GetResult();
            return res.IsSuccessStatusCode ? (res.Content.ReadFromJsonAsync<List<string>>().GetAwaiter().GetResult() ?? Enumerable.Empty<string>()) : Enumerable.Empty<string>();
        }

        public List<Course> GetFacultyTeachingSchedule(string facultyId)
        {
            var res = _client.GetAsync($"/api/faculty/{facultyId}/schedule").GetAwaiter().GetResult();
            if (res.IsSuccessStatusCode)
            {
                var list = res.Content.ReadFromJsonAsync<List<CourseDto>>().GetAwaiter().GetResult();
                return list?.Select(MapToCourse).ToList() ?? new();
            }
            return new();
        }

        public List<Student> GetClassRoster(string facultyId, string courseId)
        {
            var res = _client.GetAsync($"/api/faculty/{facultyId}/courses/{courseId}/roster").GetAwaiter().GetResult();
            if (res.IsSuccessStatusCode)
            {
                var roster = res.Content.ReadFromJsonAsync<CourseRosterDto>().GetAwaiter().GetResult();
                if (roster?.Students != null)
                {
                    var students = new List<Student>();
                    foreach (var s in roster.Students)
                    {
                        var student = _factoryManager.CreateStudent(s.StudentId, s.StudentName, "", "", s.StudentNumber, "", 2026);
                        if (s.Grade != "Not Graded" && s.Grade != null)
                            student.RecordCompletedCourse(new CourseRecord(courseId, "", "", "", (Grade)Enum.Parse(typeof(Grade), s.Grade), 0));
                        students.Add(student);
                    }
                    return students;
                }
            }
            return new();
        }

        public GradeSubmissionResult SubmitFacultyGrades(string facultyId, string courseId, Dictionary<string, string> rawGrades)
        {
            var res = _client.PostAsJsonAsync("/api/faculty/grades/submit", new { facultyId, courseId, grades = rawGrades }).GetAwaiter().GetResult();
            RefreshCache();
            if (res.IsSuccessStatusCode) return new GradeSubmissionResult(rawGrades.Count, rawGrades.Count, new());
            return new GradeSubmissionResult(rawGrades.Count, 0, new() { new GradeError("Batch", "Error", res.Content.ReadAsStringAsync().GetAwaiter().GetResult()) });
        }

        public GradeApprovalResult ApproveCourseGrades(string courseId)
        {
            var res = _client.PostAsync($"/api/admin/courses/{courseId}/approve-grades", null).GetAwaiter().GetResult();
            RefreshCache();
            return new GradeApprovalResult(res.IsSuccessStatusCode, res.IsSuccessStatusCode ? "Approved." : "Failed.", res.IsSuccessStatusCode ? 1 : 0);
        }

        public CourseChangeRequest RequestCourseUpdate(string facultyId, string courseId, string fieldChanged, string newValue)
        {
            var res = _client.PostAsJsonAsync("/api/faculty/course-change-request", new { CourseId = courseId, FacultyId = facultyId, FieldChanged = fieldChanged, NewValue = newValue }).GetAwaiter().GetResult();
            var content = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            RefreshCache();
            if (res.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(content);
                var reqId = doc.RootElement.GetProperty("requestId").GetString();
                return new CourseChangeRequest(reqId, courseId, facultyId, fieldChanged, "", newValue);
            }
            string errMsg = "Request failed.";
            try
            {
                using var errDoc = JsonDocument.Parse(content);
                if (errDoc.RootElement.TryGetProperty("error", out var err)) errMsg = err.GetString();
                else if (errDoc.RootElement.TryGetProperty("errors", out var errs)) errMsg = string.Join("; ", errs.EnumerateArray().Select(e => e.GetString()));
            }
            catch { }
            throw new InvalidOperationException(errMsg);
        }

        public List<CourseChangeRequest> GetFacultyChangeRequests(string facultyId)
        {
            var res = _client.GetAsync($"/api/faculty/{facultyId}/change-requests").GetAwaiter().GetResult();
            if (res.IsSuccessStatusCode)
            {
                var list = res.Content.ReadFromJsonAsync<List<ChangeRequestDto>>().GetAwaiter().GetResult();
                return list?.Select(d => new CourseChangeRequest(d.RequestId, d.CourseId, d.RequestedByFacultyId, d.FieldChanged, d.OldValue, d.NewValue)
                {
                    Status = ParseChangeRequestStatus(d.Status),
                    ReviewedByAdminId = d.ReviewedByAdminId,
                    RequestedAt = d.RequestedAt
                }).ToList() ?? new();
            }
            return new();
        }

        public GradeSubmissionStatus GetCourseGradeStatus(string courseId)
            => _courses.TryGetValue(courseId, out var c) ? c.GradeStatus : GradeSubmissionStatus.NotSubmitted;

        public EnrollmentReport GenerateDepartmentReport(string department, double utilizationThresholdPercent = 90.0)
        {
            var res = _client.GetAsync($"/api/admin/reports/utilization?department={Uri.EscapeDataString(department)}&threshold={utilizationThresholdPercent}").GetAwaiter().GetResult();
            if (res.IsSuccessStatusCode)
            {
                var d = res.Content.ReadFromJsonAsync<ReportResponseDto>().GetAwaiter().GetResult();
                if (d != null)
                {
                    var report = new EnrollmentReport { GeneratedAt = d.GeneratedAt, Department = d.Department, UtilizationThresholdPercent = d.UtilizationThresholdPercent, TotalCourses = d.TotalCourses, TotalEnrollments = d.TotalEnrollments };
                    foreach (var c in d.CoursesOverThreshold ?? new())
                        report.CoursesOverThreshold.Add(new CourseUtilization { CourseId = c.CourseId, CourseName = c.CourseName, Enrolled = c.Enrolled, Capacity = c.Capacity, UtilizationPercent = c.UtilizationPercent });
                    return report;
                }
            }
            return new EnrollmentReport { Department = department, UtilizationThresholdPercent = utilizationThresholdPercent };
        }

        public IEnumerable<CourseChangeRequest> GetPendingChangeRequests()
        {
            var res = _client.GetAsync("/api/admin/change-requests/pending").GetAwaiter().GetResult();
            if (res.IsSuccessStatusCode)
            {
                var list = res.Content.ReadFromJsonAsync<List<ChangeRequestDto>>().GetAwaiter().GetResult();
                return list?.Select(d => new CourseChangeRequest(d.RequestId, d.CourseId, d.RequestedByFacultyId, d.FieldChanged, d.OldValue, d.NewValue)
                {
                    Status = ParseChangeRequestStatus(d.Status),
                    ReviewedByAdminId = d.ReviewedByAdminId,
                    RequestedAt = d.RequestedAt
                }) ?? Enumerable.Empty<CourseChangeRequest>();
            }
            return Enumerable.Empty<CourseChangeRequest>();
        }

        public bool ApproveCourseChange(string requestId, string adminId)
        {
            var res = _client.PostAsJsonAsync($"/api/admin/change-requests/{requestId}/approve?adminId={adminId}", new { }).GetAwaiter().GetResult();
            RefreshCache();
            return res.IsSuccessStatusCode;
        }

        public bool RejectCourseChange(string requestId, string adminId, string reason = null)
        {
            var res = _client.PostAsJsonAsync($"/api/admin/change-requests/{requestId}/reject?adminId={adminId}&reason={Uri.EscapeDataString(reason ?? "")}", new { }).GetAwaiter().GetResult();
            RefreshCache();
            return res.IsSuccessStatusCode;
        }

        public bool SetUserActiveStatus(string userId, bool active)
        {
            var res = _client.PostAsJsonAsync($"/api/admin/users/{userId}/{(active ? "activate" : "deactivate")}", new { }).GetAwaiter().GetResult();
            RefreshCache();
            return res.IsSuccessStatusCode;
        }

        public bool ForceEnrollStudent(string studentId, string courseId)
        {
            var res = _client.PostAsJsonAsync("/api/admin/enroll-override", new { studentId, courseId }).GetAwaiter().GetResult();
            RefreshCache();
            return res.IsSuccessStatusCode;
        }

        public Student CreateAndRegisterStudent(string id, string name, string email, string phone, string num, string prog, int year)
        {
            var res = _client.PostAsJsonAsync("/api/admin/students", new { userId = id, fullName = name, email, phone, studentNumber = num, programId = prog, enrolledYear = year }).GetAwaiter().GetResult();
            RefreshCache();
            return _users.TryGetValue(id, out var u) && u is Student s ? s : null;
        }

        public Faculty CreateAndRegisterFaculty(string id, string name, string email, string phone, string num, string dept, string rank)
        {
            var res = _client.PostAsJsonAsync("/api/admin/faculty", new { userId = id, fullName = name, email, phone, employeeNumber = num, department = dept, rank }).GetAwaiter().GetResult();
            RefreshCache();
            return _users.TryGetValue(id, out var u) && u is Faculty f ? f : null;
        }

        public Admin CreateAndRegisterAdmin(string id, string name, string email, string phone, string num, string office, AdminScope scope = AdminScope.Full)
        {
            var res = _client.PostAsJsonAsync("/api/admin/admins", new { userId = id, fullName = name, email, phone, staffNumber = num, office, scope }).GetAwaiter().GetResult();
            RefreshCache();
            return _users.TryGetValue(id, out var u) && u is Admin a ? a : null;
        }

        public Student CreateStudentAccount(string id, string name, string email, string phone, string num, string prog, int year)
            => CreateAndRegisterStudent(id, name, email, phone, num, prog, year);

        public Faculty CreateFacultyAccount(string id, string name, string email, string phone, string num, string dept, string rank, List<string> courses)
        {
            var res = _client.PostAsJsonAsync("/api/admin/faculty", new { userId = id, fullName = name, email, phone, employeeNumber = num, department = dept, rank, assignedCourseIds = courses }).GetAwaiter().GetResult();
            RefreshCache();
            return _users.TryGetValue(id, out var u) && u is Faculty f ? f : null;
        }

        public void AttachObserver(INotificationObserver observer) => _notificationService.AddObserver(observer);
        public bool DeleteStudentAccount(string studentId) { var res = _client.DeleteAsync($"/api/admin/students/{studentId}").GetAwaiter().GetResult(); RefreshCache(); return res.IsSuccessStatusCode; }
        public bool DeleteFacultyAccount(string facultyId) { var res = _client.DeleteAsync($"/api/admin/faculty/{facultyId}").GetAwaiter().GetResult(); RefreshCache(); return res.IsSuccessStatusCode; }

        public Course AddCourse(Course course)
        {
            var pre = course.PrerequisiteCourseIds.ToList();
            _client.PostAsJsonAsync("/api/admin/courses", new { courseId = course.CourseId, courseCode = course.CourseCode, courseName = course.CourseName, description = course.Description, department = course.Department, instructorId = course.InstructorId, instructorName = course.InstructorName, credits = course.Credits, semester = course.Semester, capacity = course.Capacity, days = course.Schedule?.Days ?? "", startTime = course.Schedule?.StartTime.ToString(@"hh\:mm") ?? "00:00", endTime = course.Schedule?.EndTime.ToString(@"hh\:mm") ?? "00:00", location = course.Schedule?.Location ?? "", prerequisiteCourseIds = pre }).GetAwaiter().GetResult();
            RefreshCache();
            return course;
        }

        public DegreeProgram AddDegreeProgram(DegreeProgram program)
        {
            var req = program.RequiredCourseIds.ToList();
            var el = program.ElectiveCourseIds.ToList();
            _client.PostAsJsonAsync("/api/admin/programs", new { programId = program.ProgramId, programName = program.ProgramName, department = program.Department, requiredCourseIds = req, electiveCourseIds = el }).GetAwaiter().GetResult();
            RefreshCache();
            return program;
        }

        // --- Helper Enum Parsers ---
        private static CourseStatus ParseCourseStatus(object obj)
        {
            if (obj is JsonElement elem)
            {
                if (elem.ValueKind == JsonValueKind.Number && elem.TryGetInt32(out int val))
                    return (CourseStatus)val;
                if (elem.ValueKind == JsonValueKind.String && Enum.TryParse<CourseStatus>(elem.GetString(), true, out var parsed))
                    return parsed;
            }
            else if (obj is int i) return (CourseStatus)i;
            else if (obj is string s && Enum.TryParse<CourseStatus>(s, true, out var parsed)) return parsed;
            return CourseStatus.Open;
        }

        private static GradeSubmissionStatus ParseGradeStatus(object obj)
        {
            if (obj is JsonElement elem)
            {
                if (elem.ValueKind == JsonValueKind.Number && elem.TryGetInt32(out int val))
                    return (GradeSubmissionStatus)val;
                if (elem.ValueKind == JsonValueKind.String && Enum.TryParse<GradeSubmissionStatus>(elem.GetString(), true, out var parsed))
                    return parsed;
            }
            else if (obj is int i) return (GradeSubmissionStatus)i;
            else if (obj is string s && Enum.TryParse<GradeSubmissionStatus>(s, true, out var parsed)) return parsed;
            return GradeSubmissionStatus.NotSubmitted;
        }

        private static ChangeRequestStatus ParseChangeRequestStatus(object obj)
        {
            if (obj is JsonElement elem)
            {
                if (elem.ValueKind == JsonValueKind.Number && elem.TryGetInt32(out int val))
                    return (ChangeRequestStatus)val;
                if (elem.ValueKind == JsonValueKind.String && Enum.TryParse<ChangeRequestStatus>(elem.GetString(), true, out var parsed))
                    return parsed;
            }
            else if (obj is int i) return (ChangeRequestStatus)i;
            else if (obj is string s && Enum.TryParse<ChangeRequestStatus>(s, true, out var parsed)) return parsed;
            return ChangeRequestStatus.Pending;
        }

        private static Course MapToCourse(CourseDto c)
        {
            var schedule = new CourseSchedule(c.Days, TimeSpan.Parse(c.StartTime), TimeSpan.Parse(c.EndTime), c.Location);
            var course = new Course(c.CourseId, c.CourseCode, c.CourseName, c.Department, c.Credits, c.Capacity, c.InstructorId, c.InstructorName, schedule, c.Semester)
            {
                Description = c.Description ?? "",
                EnrolledCount = c.EnrolledCount,
                Status = ParseCourseStatus(c.Status),
                GradeStatus = ParseGradeStatus(c.GradeStatus)
            };
            foreach (var pre in c.PrerequisiteCourseIds ?? new()) course.AddPrerequisite(pre);
            foreach (var w in c.Waitlist ?? new()) course.AddToWaitlist(w);
            return course;
        }

        // --- HTTP DTOs ---
        private record StudentDto(string UserId, string FullName, string Email, string Phone, string StudentNumber, string ProgramId, int EnrolledYear, bool IsActive, List<string> EnrolledCourseIds, List<string> WaitlistedCourseIds);
        private record FacultyDto(string UserId, string FullName, string Email, string Phone, string EmployeeNumber, string Department, string Rank, bool IsActive, List<string> TeachingCourseIds, List<string> AssignedCourseIds);
        private record AdminDto(string UserId, string FullName, string Email, string Phone, string StaffNumber, string Office, int Scope, bool IsActive);
        private record UsersResponseDto(List<StudentDto> Students, List<FacultyDto> Faculty, List<AdminDto> Admins);
        private record CourseDto(string CourseId, string CourseCode, string CourseName, string Description, string Department, string InstructorId, string InstructorName, int Credits, string Semester, int Capacity, int EnrolledCount, string Days, string StartTime, string EndTime, string Location, JsonElement Status, JsonElement GradeStatus, List<string> PrerequisiteCourseIds, List<string> Waitlist);
        private record ProgramDto(string ProgramId, string ProgramName, string Department, List<string> RequiredCourseIds, List<string> ElectiveCourseIds);
        private record CourseRecordDto(string CourseId, string CourseCode, string CourseName, string Semester, string Grade, int Credits);
        private record CourseRosterDto(string CourseId, string CourseCode, string CourseName, JsonElement GradeStatus, List<RosterStudentDto> Students);
        private record RosterStudentDto(string StudentId, string StudentName, string StudentNumber, string Grade);
        private record ChangeRequestDto(string RequestId, string CourseId, string RequestedByFacultyId, string FieldChanged, string OldValue, string NewValue, JsonElement Status, string ReviewedByAdminId, DateTime RequestedAt);
        private record ReportResponseDto(DateTime GeneratedAt, string Department, int TotalCourses, int TotalEnrollments, double UtilizationThresholdPercent, List<ReportCourseDto> CoursesOverThreshold);
        private record ReportCourseDto(string CourseId, string CourseName, int Enrolled, int Capacity, double UtilizationPercent);
    }
}

namespace NexusEnroll.Patterns
{
    // Factory manager to create client user profiles.
    public class UserFactoryManager
    {
        public Student CreateStudent(string id, string name, string email, string phone, string num, string prog, int year)
            => new Student(id, name, email, phone, num, prog, year);

        public Faculty CreateFaculty(string id, string name, string email, string phone, string num, string dept, string rank)
            => new Faculty(id, name, email, phone, num, dept, rank);

        public Admin CreateAdmin(string id, string name, string email, string phone, string num, string office, AdminScope scope = AdminScope.Full)
            => new Admin(id, name, email, phone, num, office, scope);
    }

    // Observer patterns definitions.
    public class NotificationEvent
    {
        public string EventType { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public IReadOnlyDictionary<string, object> Data { get; }
        public NotificationEvent(string type, IDictionary<string, object> d = null) { EventType = type; Data = d != null ? new Dictionary<string, object>(d) : new Dictionary<string, object>(); }
        public object Get(string key) => Data.TryGetValue(key, out var v) ? v : null;
        public override string ToString() => $"[{Timestamp:u}] {EventType} ({Data.Count} fields)";
    }

    public interface INotificationObserver { string ObserverName { get; } void Update(NotificationEvent ev); }
    public interface ISubject { void Attach(INotificationObserver observer); void Detach(INotificationObserver observer); void Notify(NotificationEvent ev); }

    public class ConsoleNotificationObserver : INotificationObserver
    {
        public string ObserverName => "ConsoleObserver";
        public void Update(NotificationEvent ev)
        {
            Console.WriteLine($"[CONSOLE] {ev}");
            foreach (var kv in ev.Data) Console.WriteLine($"    - {kv.Key}: {kv.Value}");
        }
    }

    public class EmailNotificationObserver : INotificationObserver
    {
        public string ObserverName => "EmailObserver";
        public void Update(NotificationEvent ev)
        {
            var recipient = ev.Get("RecipientEmail")?.ToString() ?? "unknown@nexus.edu";
            Console.WriteLine($"[EMAIL -> {recipient}] {ev.EventType}");
        }
    }

    public class SmsNotificationObserver : INotificationObserver
    {
        public string ObserverName => "SmsObserver";
        public void Update(NotificationEvent ev)
        {
            var phone = ev.Get("RecipientPhone")?.ToString() ?? "unknown";
            Console.WriteLine($"[SMS -> {phone}] {ev.EventType}");
        }
    }
}

namespace NexusEnroll.Services
{
    using NexusEnroll.Patterns;
    public interface INotificationService
    {
        void AddObserver(INotificationObserver observer);
        void RemoveObserver(INotificationObserver observer);
        void NotifyEvent(string eventType, IDictionary<string, object> data = null);
        IReadOnlyList<NotificationEvent> GetHistory();
    }

    public class NotificationService : INotificationService, ISubject
    {
        private readonly List<INotificationObserver> _observers = new();
        private readonly List<NotificationEvent> _history = new();
        public void Attach(INotificationObserver observer) => AddObserver(observer);
        public void Detach(INotificationObserver observer) => RemoveObserver(observer);
        public void AddObserver(INotificationObserver observer) { if (observer != null && !_observers.Contains(observer)) _observers.Add(observer); }
        public void RemoveObserver(INotificationObserver observer) { if (observer != null) _observers.Remove(observer); }
        public void NotifyEvent(string type, IDictionary<string, object> data = null) => Notify(new NotificationEvent(type, data));
        public void Notify(NotificationEvent ev)
        {
            _history.Add(ev);
            foreach (var obs in _observers.ToArray()) { try { obs.Update(ev); } catch { } }
        }
        public IReadOnlyList<NotificationEvent> GetHistory() => _history.AsReadOnly();
    }

    public class GradeError
    {
        public string StudentId { get; set; }
        public string RawValue { get; set; }
        public string Reason { get; set; }
        public GradeError(string id, string val, string r) { StudentId = id; RawValue = val; Reason = r; }
        public override string ToString() => $"{StudentId}: '{RawValue}' rejected -- {Reason}";
    }

    public class GradeSubmissionResult
    {
        public int TotalSubmitted { get; }
        public int SuccessCount { get; }
        public List<GradeError> Errors { get; }
        public bool AllSucceeded => Errors.Count == 0;
        public bool AnySucceeded => SuccessCount > 0;
        public GradeSubmissionResult(int tot, int succ, List<GradeError> errs) { TotalSubmitted = tot; SuccessCount = succ; Errors = errs ?? new(); }
        public override string ToString() => AllSucceeded ? $"All {SuccessCount} grades submitted." : $"{SuccessCount}/{TotalSubmitted} submitted. {Errors.Count} rejected.";
    }

    public class GradeApprovalResult
    {
        public bool Success { get; }
        public string Message { get; }
        public int ApprovedCount { get; }
        public GradeApprovalResult(bool s, string m, int count) { Success = s; Message = m; ApprovedCount = count; }
    }

    public class CourseUtilization
    {
        public string CourseId { get; set; }
        public string CourseName { get; set; }
        public int Enrolled { get; set; }
        public int Capacity { get; set; }
        public double UtilizationPercent { get; set; }
    }

    public class EnrollmentReport
    {
        public DateTime GeneratedAt { get; set; }
        public string Department { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public double UtilizationThresholdPercent { get; set; }
        public List<CourseUtilization> CoursesOverThreshold { get; set; } = new();

        public override string ToString()
        {
            var lines = new List<string>
            {
                $"Enrollment Report - {Department} (generated {GeneratedAt:G})",
                $"  Total courses      : {TotalCourses}",
                $"  Total enrollments  : {TotalEnrollments}",
                $"  Courses >= {UtilizationThresholdPercent}% capacity:"
            };
            if (CoursesOverThreshold != null && CoursesOverThreshold.Count > 0)
            {
                foreach (var c in CoursesOverThreshold)
                    lines.Add($"    - {c.CourseName} ({c.CourseId}): {c.Enrolled}/{c.Capacity} = {c.UtilizationPercent:F1}%");
            }
            else
            {
                lines.Add("    (No courses currently meet or exceed this utilization threshold)");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}

namespace NexusEnroll.Models
{
    public enum UserRole { Student, Faculty, Admin }
    public enum AdminScope { ReadOnly, CourseManagement, Full }
    public enum CourseStatus { Open, Closed, Cancelled }
    public enum Grade { A, B, C, D, F, W, I }
    public enum EnrollmentStatus { Enrolled, Waitlisted, Completed, Dropped }
    public enum GradeSubmissionStatus { NotSubmitted, Pending, Submitted }
    public enum ChangeRequestStatus { Pending, Approved, Rejected }

    public static class GradeExtensions
    {
        public static bool IsPassing(this Grade grade) => grade <= Grade.D;
    }

    public class CourseSchedule
    {
        public string Days { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Location { get; set; }
        public CourseSchedule() { }
        public CourseSchedule(string days, TimeSpan start, TimeSpan end, string loc) { Days = days; StartTime = start; EndTime = end; Location = loc; }
        public bool ConflictsWith(CourseSchedule other)
        {
            if (other == null || string.IsNullOrEmpty(Days) || string.IsNullOrEmpty(other.Days)) return false;
            var myDays = ParseDays(Days);
            var otherDays = ParseDays(other.Days);
            if (!myDays.Overlaps(otherDays)) return false;
            return StartTime < other.EndTime && other.StartTime < EndTime;
        }
        private static HashSet<string> ParseDays(string daysStr)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(daysStr)) return set;
            int i = 0;
            while (i < daysStr.Length)
            {
                if (i + 1 < daysStr.Length && (daysStr.Substring(i, 2).Equals("Th", StringComparison.OrdinalIgnoreCase) || daysStr.Substring(i, 2).Equals("Sa", StringComparison.OrdinalIgnoreCase) || daysStr.Substring(i, 2).Equals("Su", StringComparison.OrdinalIgnoreCase)))
                { set.Add(daysStr.Substring(i, 2)); i += 2; }
                else { set.Add(daysStr[i].ToString()); i++; }
            }
            return set;
        }
        public override string ToString() => $"{Days} {StartTime}-{EndTime} @ {Location}";
    }

    public class Course
    {
        public string CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Description { get; set; }
        public string Department { get; set; }
        public string InstructorId { get; set; }
        public string InstructorName { get; set; }
        public int Credits { get; set; }
        public string Semester { get; set; }
        public int Capacity { get; set; }
        public int EnrolledCount { get; set; }
        public CourseSchedule Schedule { get; set; }
        public CourseStatus Status { get; set; }
        public GradeSubmissionStatus GradeStatus { get; set; }
        private readonly List<string> _prerequisiteCourseIds = new();
        public IReadOnlyList<string> PrerequisiteCourseIds => _prerequisiteCourseIds;
        private readonly List<string> _waitlist = new();
        public IReadOnlyList<string> Waitlist => _waitlist;
        public Course() { Status = CourseStatus.Open; GradeStatus = GradeSubmissionStatus.NotSubmitted; }
        public Course(string id, string code, string name, string dept, int cred, int cap, string instId, string instName, CourseSchedule sched, string sem) : this()
        { CourseId = id; CourseCode = code; CourseName = name; Department = dept; Credits = cred; Capacity = cap; InstructorId = instId; InstructorName = instName; Schedule = sched; Semester = sem; Description = ""; }
        public int AvailableSeats => Math.Max(0, Capacity - EnrolledCount);
        public bool HasAvailableSeats() => Status == CourseStatus.Open && EnrolledCount < Capacity;
        public bool Enroll() { if (!HasAvailableSeats()) return false; EnrolledCount++; if (EnrolledCount >= Capacity) Status = CourseStatus.Closed; return true; }
        public bool Drop() { if (EnrolledCount <= 0) return false; EnrolledCount--; if (Status == CourseStatus.Closed && EnrolledCount < Capacity) Status = CourseStatus.Open; return true; }
        public void AddPrerequisite(string id) { if (id != null && !_prerequisiteCourseIds.Contains(id)) _prerequisiteCourseIds.Add(id); }
        public void RemovePrerequisite(string id) { if (id != null) _prerequisiteCourseIds.Remove(id); }
        public bool HasPrerequisite(string id) => id != null && _prerequisiteCourseIds.Contains(id);
        public bool MeetsPrerequisites(IEnumerable<string> completed) => _prerequisiteCourseIds.Count == 0 || (completed != null && _prerequisiteCourseIds.All(new HashSet<string>(completed).Contains));
        public void AddToWaitlist(string studentId) { if (studentId != null && !_waitlist.Contains(studentId)) _waitlist.Add(studentId); }
        public void RemoveFromWaitlist(string studentId) { if (studentId != null) _waitlist.Remove(studentId); }
        public string PopNextWaitlistedStudent() { if (_waitlist.Count == 0) return null; string next = _waitlist[0]; _waitlist.RemoveAt(0); return next; }
        public bool IsWaitlisted(string studentId) => studentId != null && _waitlist.Contains(studentId);
        public bool HasTimeConflictWith(Course other) => other != null && Schedule != null && other.Schedule != null && Schedule.ConflictsWith(other.Schedule);
        public override string ToString() => $"{CourseCode} - {CourseName} ({EnrolledCount}/{Capacity}, {Status})";
    }

    public class CourseRecord
    {
        public string CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Semester { get; set; }
        public Grade Grade { get; set; }
        public int Credits { get; set; }
        public DateTime CompletedAt { get; set; }
        public CourseRecord() { }
        public CourseRecord(string id, string code, string name, string sem, Grade gr, int cred) { CourseId = id; CourseCode = code; CourseName = name; Semester = sem; Grade = gr; Credits = cred; CompletedAt = DateTime.UtcNow; }
        public bool IsPassing => Grade.IsPassing();
    }

    public class Enrollment
    {
        public string EnrollmentId { get; set; }
        public string StudentId { get; set; }
        public string CourseId { get; set; }
        public EnrollmentStatus Status { get; set; }
        public DateTime EnrolledAt { get; set; }
        public Grade? Grade { get; set; }
        public DateTime? GradedAt { get; set; }
        public GradeSubmissionStatus SubmissionStatus { get; set; }
        public Enrollment() { }
        public Enrollment(string id, string stuId, string cId, EnrollmentStatus stat = EnrollmentStatus.Enrolled) { EnrollmentId = id; StudentId = stuId; CourseId = cId; Status = stat; EnrolledAt = DateTime.UtcNow; SubmissionStatus = GradeSubmissionStatus.NotSubmitted; }
        public void SubmitGradePending(Grade grade) { Grade = grade; SubmissionStatus = GradeSubmissionStatus.Pending; }
        public void FinaliseGrade() { if (SubmissionStatus != GradeSubmissionStatus.Pending || Grade == null) throw new InvalidOperationException("Not pending."); Status = EnrollmentStatus.Completed; GradedAt = DateTime.UtcNow; SubmissionStatus = GradeSubmissionStatus.Submitted; }
        public void Drop() => Status = EnrollmentStatus.Dropped;
        public override string ToString() => $"Enrollment[{StudentId} -> {CourseId}, {Status}]";
    }

    public class DegreeProgram
    {
        public string ProgramId { get; set; }
        public string ProgramName { get; set; }
        public string Department { get; set; }
        private readonly List<string> _requiredCourseIds = new();
        public IReadOnlyList<string> RequiredCourseIds => _requiredCourseIds;
        private readonly List<string> _electiveCourseIds = new();
        public IReadOnlyList<string> ElectiveCourseIds => _electiveCourseIds;
        public DegreeProgram() { }
        public DegreeProgram(string id, string name, string dept) : this() { ProgramId = id; ProgramName = name; Department = dept; }
        public void AddRequiredCourse(string id) { if (id != null && !_requiredCourseIds.Contains(id)) _requiredCourseIds.Add(id); }
        public void AddElectiveCourse(string id) { if (id != null && !_electiveCourseIds.Contains(id)) _electiveCourseIds.Add(id); }
        public bool IsRequiredCourse(string id) => id != null && _requiredCourseIds.Contains(id);
        public bool IsElectiveCourse(string id) => id != null && _electiveCourseIds.Contains(id);
    }

    public class CourseChangeRequest
    {
        public string RequestId { get; set; }
        public string CourseId { get; set; }
        public string RequestedByFacultyId { get; set; }
        public string FieldChanged { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public ChangeRequestStatus Status { get; set; }
        public string ReviewedByAdminId { get; set; }
        public DateTime RequestedAt { get; set; }
        public CourseChangeRequest() { Status = ChangeRequestStatus.Pending; RequestedAt = DateTime.UtcNow; }
        public CourseChangeRequest(string id, string cId, string facId, string field, string oldV, string newV) : this() { RequestId = id; CourseId = cId; RequestedByFacultyId = facId; FieldChanged = field; OldValue = oldV; NewValue = newV; }
    }

    public abstract class User
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public UserRole Role { get; protected set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        protected User(string id, string name, string mail, string ph, UserRole r) { UserId = id; FullName = name; Email = mail; Phone = ph; Role = r; IsActive = true; CreatedAt = DateTime.UtcNow; }
    }

    public class Student : User
    {
        public string StudentNumber { get; set; }
        public string ProgramId { get; set; }
        public int EnrolledYear { get; set; }
        public List<string> EnrolledCourseIds { get; } = new();
        public List<string> WaitlistedCourseIds { get; } = new();
        public List<CourseRecord> AcademicHistory { get; } = new();
        public Student(string id, string name, string mail, string ph, string num, string prog, int year) : base(id, name, mail, ph, UserRole.Student) { StudentNumber = num; ProgramId = prog; EnrolledYear = year; }
        public void EnrollInCourse(string id) { if (id != null && !EnrolledCourseIds.Contains(id)) EnrolledCourseIds.Add(id); }
        public void DropCourse(string id) { if (id != null) EnrolledCourseIds.Remove(id); }
        public void RecordCompletedCourse(CourseRecord record) { if (record != null) AcademicHistory.Add(record); }
        public string GetProfile() => $"Student: {FullName} ({StudentNumber}) - {ProgramId}";
    }

    public class Faculty : User
    {
        public string EmployeeNumber { get; set; }
        public string Department { get; set; }
        public string Rank { get; set; }
        public List<string> TeachingCourseIds { get; } = new();
        public Faculty(string id, string name, string mail, string ph, string num, string dept, string rnk) : base(id, name, mail, ph, UserRole.Faculty) { EmployeeNumber = num; Department = dept; Rank = rnk; }
        public void AssignCourse(string id) { if (id != null && !TeachingCourseIds.Contains(id)) TeachingCourseIds.Add(id); }
        public void RemoveCourse(string id) { if (id != null) TeachingCourseIds.Remove(id); }
        public string GetProfile() => $"Faculty: {FullName} ({EmployeeNumber}) - {Department}";
    }

    public class Admin : User
    {
        public string StaffNumber { get; set; }
        public string Office { get; set; }
        public AdminScope Scope { get; set; }
        public Admin(string id, string name, string mail, string ph, string num, string off, AdminScope sc = AdminScope.Full) : base(id, name, mail, ph, UserRole.Admin) { StaffNumber = num; Office = off; Scope = sc; }
        public string GetProfile() => $"Admin: {FullName} ({StaffNumber}) - Scope: {Scope}";
    }
}
