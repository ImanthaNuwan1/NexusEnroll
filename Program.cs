using System;
using System.Collections.Generic;
using System.Linq;
using NexusEnroll.Models;
using NexusEnroll.Patterns;
using NexusEnroll.Services;

namespace NexusEnroll
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var app = new System.Windows.Application();
            app.Run(new MainWindow());
        }

        // =====================================================================
        // MAIN MENU
        // =====================================================================

        private static void PrintMainMenu()
        {
            Console.WriteLine();
            Console.WriteLine("=================== MAIN MENU ===================");
            Console.WriteLine(" Welcome to NexusEnroll. Select a portal to begin.");
            Console.WriteLine();
            Console.WriteLine("  [1] Run Full Automated Demo  (for screencast)");
            Console.WriteLine("  [2] Student Portal            (enroll, drop, browse)");
            Console.WriteLine("  [3] Faculty Portal            (rosters, grading)");
            Console.WriteLine("  [4] Administrator Portal       (reports, approvals)");
            Console.WriteLine("  [5] View Notification History  (email/SMS log)");
            Console.WriteLine("  [0] Exit");
            Console.WriteLine("=================================================");
            Console.Write("\nSelect an option [0-5]: ");
        }

        // =====================================================================
        // SEED DATA
        // =====================================================================

        private static void SeedUniversityData(UniversityFacade facade)
        {
            Console.WriteLine("[SETUP] Loading initial university data...");

            // 1. Degree Program
            var csProgram = new DegreeProgram("BS-CS", "BSc in Computer Science", "Computer Science");
            csProgram.AddRequiredCourse("CS101");
            csProgram.AddRequiredCourse("CS201");
            csProgram.AddRequiredCourse("CS301");
            csProgram.AddElectiveCourse("SE101");
            facade.AddDegreeProgram(csProgram);

            // 2. Faculty
            facade.CreateAndRegisterFaculty("FAC001", "Dr. Alan Turing",   "turing@nexus.edu",  "555-0101", "EMP-001", "Computer Science",     "Professor");
            facade.CreateAndRegisterFaculty("FAC002", "Prof. Grace Hopper", "hopper@nexus.edu",  "555-0102", "EMP-002", "Computer Science",     "Associate Professor");

            // 3. Administrators
            facade.CreateAndRegisterAdmin("ADM001", "Alice Administrator", "alice.admin@nexus.edu", "555-0201", "ADM-001", "Dean's Office 201",    AdminScope.Full);
            facade.CreateAndRegisterAdmin("ADM002", "Bob Registrar",       "bob.clerk@nexus.edu",   "555-0202", "ADM-002", "Registrar 105",        AdminScope.CourseManagement);

            // 4. Courses
            var cs101 = new Course("CS101", "CS 101", "Intro to Programming", "Computer Science", 3, 5,
                "FAC001", "Dr. Alan Turing",
                new CourseSchedule("MWF", TimeSpan.FromHours(9),  TimeSpan.FromHours(10),    "Hall A"), "Fall 2026");
            cs101.Description = "Fundamental principles of programming and algorithms.";
            facade.AddCourse(cs101);

            var cs201 = new Course("CS201", "CS 201", "Data Structures & Algorithms", "Computer Science", 4, 2,
                "FAC001", "Dr. Alan Turing",
                new CourseSchedule("MWF", TimeSpan.FromHours(10), TimeSpan.FromHours(11),    "Hall B"), "Fall 2026");
            cs201.Description = "Abstract data types, trees, graphs, and algorithmic complexity.";
            cs201.AddPrerequisite("CS101");
            facade.AddCourse(cs201);

            var cs301 = new Course("CS301", "CS 301", "Advanced Algorithms", "Computer Science", 4, 2,
                "FAC002", "Prof. Grace Hopper",
                new CourseSchedule("TTh", TimeSpan.FromHours(10),  TimeSpan.FromHours(11.5),  "Lab 3"),  "Fall 2026");
            cs301.Description = "Greedy algorithms, dynamic programming, and NP-completeness.";
            cs301.AddPrerequisite("CS201");
            facade.AddCourse(cs301);

            var cs302 = new Course("CS302", "CS 302", "Operating Systems", "Computer Science", 4, 2,
                "FAC002", "Prof. Grace Hopper",
                new CourseSchedule("MWF", TimeSpan.FromHours(10.5), TimeSpan.FromHours(11.5), "Hall C"), "Fall 2026");
            cs302.Description = "Concurrency, processes, memory management, and file systems.";
            cs302.AddPrerequisite("CS201");
            facade.AddCourse(cs302);

            var se101 = new Course("SE101", "SE 101", "Software Architecture", "Software Engineering", 3, 2,
                "FAC001", "Dr. Alan Turing",
                new CourseSchedule("TTh", TimeSpan.FromHours(14),   TimeSpan.FromHours(15.5),  "Hall D"), "Fall 2026");
            se101.Description = "Design patterns, architectural styles, and microservices design.";
            se101.AddPrerequisite("CS101");
            facade.AddCourse(se101);

            // 5. Students (with some pre-completed courses)
            var s1 = facade.CreateAndRegisterStudent("STU001", "John Doe",      "john.doe@nexus.edu",      "555-0301", "S1001", "BS-CS", 2023);
            s1.RecordCompletedCourse(new CourseRecord("CS101", "CS 101", "Intro to Programming", "Spring 2024", Grade.A, 3));

            facade.CreateAndRegisterStudent("STU002", "Jane Smith",    "jane.smith@nexus.edu",    "555-0302", "S1002", "BS-CS", 2026);

            var s3 = facade.CreateAndRegisterStudent("STU003", "Bob Johnson",   "bob.johnson@nexus.edu",   "555-0303", "S1003", "BS-CS", 2024);
            s3.RecordCompletedCourse(new CourseRecord("CS101", "CS 101", "Intro to Programming",          "Spring 2024", Grade.B, 3));
            s3.RecordCompletedCourse(new CourseRecord("CS201", "CS 201", "Data Structures & Algorithms",  "Fall 2024",   Grade.A, 4));

            var s4 = facade.CreateAndRegisterStudent("STU004", "Charlie Brown", "charlie.b@nexus.edu",    "555-0304", "S1004", "BS-CS", 2025);
            s4.RecordCompletedCourse(new CourseRecord("CS101", "CS 101", "Intro to Programming",          "Spring 2025", Grade.C, 3));

            Console.WriteLine("Seed data loaded: 4 students, 2 faculty, 2 admins, 5 courses, 1 degree program.\n");
        }

        // =====================================================================
        // STUDENT PORTAL
        // =====================================================================

        private static void StudentPortal(UniversityFacade facade)
        {
            bool back = false;
            while (!back)
            {
                Console.WriteLine();
                Console.WriteLine("-------------------- STUDENT PORTAL --------------------");
                Console.WriteLine(" Available students: STU001 (John Doe), STU002 (Jane Smith),");
                Console.WriteLine("                     STU003 (Bob Johnson), STU004 (Charlie Brown)");
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine("  [1] Browse Course Catalog");
                Console.WriteLine("  [2] View Course Details");
                Console.WriteLine("  [3] Enroll in a Course");
                Console.WriteLine("  [4] Drop a Course");
                Console.WriteLine("  [5] Join Waitlist (for full courses)");
                Console.WriteLine("  [6] View My Enrolled Schedule");
                Console.WriteLine("  [7] View My Academic History");
                Console.WriteLine("  [8] Degree Audit (remaining required courses)");
                Console.WriteLine("  [0] Back to Main Menu");
                Console.Write("\nSelect an option [0-8]: ");

                string choice = Console.ReadLine()?.Trim();
                Console.WriteLine();

                switch (choice)
                {
                    case "1": StudentBrowseCatalog(facade);   break;
                    case "2": StudentViewCourseDetails(facade); break;
                    case "3": StudentEnroll(facade);          break;
                    case "4": StudentDropCourse(facade);      break;
                    case "5": StudentJoinWaitlist(facade);    break;
                    case "6": StudentViewSchedule(facade);    break;
                    case "7": StudentViewHistory(facade);     break;
                    case "8": StudentDegreeAudit(facade);     break;
                    case "0": back = true;                    break;
                    default:  Console.WriteLine("Invalid option. Try again."); break;
                }
            }
        }

        private static void StudentBrowseCatalog(UniversityFacade facade)
        {
            Console.WriteLine("--- Browse Course Catalog ---");
            Console.WriteLine("(Leave any filter blank to skip it)");
            Console.Write  ("  Department (e.g., Computer Science): ");
            string dept = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(dept)) dept = null;

            Console.Write  ("  Keyword (e.g., Algorithms): ");
            string keyword = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(keyword)) keyword = null;

            Console.Write  ("  Instructor name: ");
            string instructor = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(instructor)) instructor = null;

            var courses = facade.BrowseCatalog(department: dept, keyword: keyword, instructorName: instructor);

            Console.WriteLine($"\n  Found {courses.Count()} course(s):\n");
            foreach (var c in courses)
            {
                Console.WriteLine($"  [{c.CourseCode}] {c.CourseName}");
                Console.WriteLine($"     Department : {c.Department}");
                Console.WriteLine($"     Credits    : {c.Credits}    Semester: {c.Semester}");
                Console.WriteLine($"     Instructor : {c.InstructorName}");
                Console.WriteLine($"     Schedule   : {c.Schedule}");
                Console.WriteLine($"     Enrollment : {c.EnrolledCount}/{c.Capacity} ({c.Status})    Available: {c.AvailableSeats}");
                if (c.PrerequisiteCourseIds.Count > 0)
                    Console.WriteLine($"     Prereqs    : {string.Join(", ", c.PrerequisiteCourseIds)}");
                if (c.Waitlist.Count > 0)
                    Console.WriteLine($"     Waitlist   : {c.Waitlist.Count} student(s)");
                Console.WriteLine();
            }
        }

        private static void StudentViewCourseDetails(UniversityFacade facade)
        {
            Console.Write("Enter Course ID (e.g., CS101): ");
            string courseId = Console.ReadLine()?.Trim();

            var course = facade.GetCourseDetails(courseId);
            if (course == null)
            {
                Console.WriteLine($"Course '{courseId}' not found.");
                return;
            }

            Console.WriteLine("\n--- Course Details ---");
            Console.WriteLine($"  Course Code   : {course.CourseCode}");
            Console.WriteLine($"  Course Name   : {course.CourseName}");
            Console.WriteLine($"  Department    : {course.Department}");
            Console.WriteLine($"  Description   : {course.Description}");
            Console.WriteLine($"  Credits       : {course.Credits}");
            Console.WriteLine($"  Semester      : {course.Semester}");
            Console.WriteLine($"  Instructor    : {course.InstructorName} ({course.InstructorId})");
            Console.WriteLine($"  Schedule      : {course.Schedule}");
            Console.WriteLine($"  Enrollment    : {course.EnrolledCount}/{course.Capacity} ({course.Status})");
            Console.WriteLine($"  Available     : {course.AvailableSeats} seat(s)");
            if (course.PrerequisiteCourseIds.Count > 0)
                Console.WriteLine($"  Prerequisites : {string.Join(", ", course.PrerequisiteCourseIds)}");
            if (course.Waitlist.Count > 0)
                Console.WriteLine($"  Waitlist      : {course.Waitlist.Count} student(s)");
        }

        private static void StudentEnroll(UniversityFacade facade)
        {
            Console.Write("Enter your Student ID (e.g., STU001): ");
            string studentId = Console.ReadLine()?.Trim();
            Console.Write("Enter Course ID to enroll (e.g., CS201): ");
            string courseId = Console.ReadLine()?.Trim();

            bool success = facade.EnrollStudentInCourse(studentId, courseId, out string message);
            Console.WriteLine();
            Console.WriteLine($"  Result : {(success ? "ENROLLED" : "FAILED")}");
            Console.WriteLine($"  Detail : {message}");
        }

        private static void StudentDropCourse(UniversityFacade facade)
        {
            Console.Write("Enter your Student ID: ");
            string studentId = Console.ReadLine()?.Trim();
            Console.Write("Enter Course ID to drop: ");
            string courseId = Console.ReadLine()?.Trim();

            bool success = facade.DropCourseAndPromoteWaitlist(studentId, courseId, out string message);
            Console.WriteLine();
            Console.WriteLine($"  Result : {(success ? "DROPPED" : "FAILED")}");
            Console.WriteLine($"  Detail : {message}");
        }

        private static void StudentJoinWaitlist(UniversityFacade facade)
        {
            Console.Write("Enter your Student ID: ");
            string studentId = Console.ReadLine()?.Trim();
            Console.Write("Enter Course ID (must be full): ");
            string courseId = Console.ReadLine()?.Trim();

            bool success = facade.JoinWaitlist(studentId, courseId, out string message);
            Console.WriteLine();
            Console.WriteLine($"  Result : {(success ? "WAITLISTED" : "FAILED")}");
            Console.WriteLine($"  Detail : {message}");
        }

        private static void StudentViewSchedule(UniversityFacade facade)
        {
            Console.Write("Enter your Student ID: ");
            string studentId = Console.ReadLine()?.Trim();

            var schedule = facade.GetStudentSchedule(studentId);
            Console.WriteLine($"\n  Your Enrolled Courses ({schedule.Count()}):\n");
            foreach (var c in schedule)
            {
                Console.WriteLine($"  [{c.CourseCode}] {c.CourseName}");
                Console.WriteLine($"     Schedule   : {c.Schedule}");
                Console.WriteLine($"     Instructor : {c.InstructorName}");
                Console.WriteLine($"     Enrollment : {c.EnrolledCount}/{c.Capacity}");
                Console.WriteLine();
            }
        }

        private static void StudentViewHistory(UniversityFacade facade)
        {
            Console.Write("Enter your Student ID: ");
            string studentId = Console.ReadLine()?.Trim();

            var history = facade.GetAcademicHistory(studentId);
            Console.WriteLine($"\n  Academic History ({history.Count()} record(s)):\n");
            foreach (var r in history)
            {
                Console.WriteLine($"  [{r.CourseCode}] {r.CourseName}");
                Console.WriteLine($"     Semester : {r.Semester}    Grade: {r.Grade} ({(r.IsPassing ? "Pass" : "Fail")})    Credits: {r.Credits}");
            }
        }

        private static void StudentDegreeAudit(UniversityFacade facade)
        {
            Console.Write("Enter your Student ID: ");
            string studentId = Console.ReadLine()?.Trim();
            Console.Write("Enter Program ID (e.g., BS-CS): ");
            string programId = Console.ReadLine()?.Trim();

            var remaining = facade.GetDegreeAudit(studentId, programId).ToList();
            Console.WriteLine($"\n  Degree Audit for program '{programId}':");
            Console.WriteLine($"  Remaining required courses: {remaining.Count}");
            if (remaining.Count == 0)
            {
                Console.WriteLine("  >>> All required courses completed! You are eligible to graduate.");
            }
            else
            {
                foreach (var id in remaining)
                    Console.WriteLine($"    - {id}");
            }
        }

        // =====================================================================
        // FACULTY PORTAL
        // =====================================================================

        private static void FacultyPortal(UniversityFacade facade)
        {
            bool back = false;
            while (!back)
            {
                Console.WriteLine();
                Console.WriteLine("-------------------- FACULTY PORTAL --------------------");
                Console.WriteLine(" Available faculty: FAC001 (Dr. Alan Turing),");
                Console.WriteLine("                    FAC002 (Prof. Grace Hopper)");
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine("  [1] View My Teaching Schedule");
                Console.WriteLine("  [2] View Class Roster for a Course");
                Console.WriteLine("  [3] Submit Grades (batch entry)");
                Console.WriteLine("  [4] Check Grade Submission Status");
                Console.WriteLine("  [5] Request Course Update (capacity/description)");
                Console.WriteLine("  [6] View My Submitted Change Requests");
                Console.WriteLine("  [0] Back to Main Menu");
                Console.Write("\nSelect an option [0-6]: ");

                string choice = Console.ReadLine()?.Trim();
                Console.WriteLine();

                switch (choice)
                {
                    case "1": FacultyViewSchedule(facade);     break;
                    case "2": FacultyViewRoster(facade);       break;
                    case "3": FacultySubmitGrades(facade);     break;
                    case "4": FacultyGradeStatus(facade);      break;
                    case "5": FacultyRequestUpdate(facade);    break;
                    case "6": FacultyViewRequests(facade);     break;
                    case "0": back = true;                     break;
                    default:  Console.WriteLine("Invalid option. Try again."); break;
                }
            }
        }

        private static void FacultyViewSchedule(UniversityFacade facade)
        {
            Console.Write("Enter your Faculty ID (e.g., FAC001): ");
            string facultyId = Console.ReadLine()?.Trim();

            try
            {
                var schedule = facade.GetFacultyTeachingSchedule(facultyId);
                Console.WriteLine($"\n  Your Teaching Schedule ({schedule.Count} course(s)):\n");
                foreach (var c in schedule)
                {
                    Console.WriteLine($"  [{c.CourseCode}] {c.CourseName}");
                    Console.WriteLine($"     Schedule   : {c.Schedule}");
                    Console.WriteLine($"     Enrollment : {c.EnrolledCount}/{c.Capacity} ({c.Status})");
                    Console.WriteLine($"     Credits    : {c.Credits}    Semester: {c.Semester}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
        }

        private static void FacultyViewRoster(UniversityFacade facade)
        {
            Console.Write("Enter your Faculty ID: ");
            string facultyId = Console.ReadLine()?.Trim();
            Console.Write("Enter Course ID: ");
            string courseId = Console.ReadLine()?.Trim();

            try
            {
                var roster = facade.GetClassRoster(facultyId, courseId);
                Console.WriteLine($"\n  Class Roster for {courseId} ({roster.Count} student(s)):\n");
                if (roster.Count == 0)
                {
                    Console.WriteLine("  (No students currently enrolled)");
                }
                else
                {
                    int i = 1;
                    foreach (var s in roster)
                    {
                        Console.WriteLine($"  {i}. {s.FullName}  ({s.StudentNumber})");
                        Console.WriteLine($"     Email   : {s.Email}");
                        Console.WriteLine($"     Program : {s.ProgramId}    Year: {s.EnrolledYear}");
                        Console.WriteLine();
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
        }

        private static void FacultySubmitGrades(UniversityFacade facade)
        {
            Console.Write("Enter your Faculty ID: ");
            string facultyId = Console.ReadLine()?.Trim();
            Console.Write("Enter Course ID: ");
            string courseId = Console.ReadLine()?.Trim();

            // Show current roster so faculty knows which student IDs to grade
            try
            {
                var roster = facade.GetClassRoster(facultyId, courseId);
                Console.WriteLine($"\n  Enrolled students in {courseId} ({roster.Count}):");
                foreach (var s in roster)
                    Console.WriteLine($"    {s.UserId}  ->  {s.FullName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Cannot load roster: {ex.Message}");
                return;
            }

            Console.WriteLine("\n  Enter grades one per line. Format: StudentID=Grade");
            Console.WriteLine("  Valid grades: A, B, C, D, F, W, I");
            Console.WriteLine("  Type 'done' to submit, or 'cancel' to abort.");

            var rawGrades = new Dictionary<string, string>();
            while (true)
            {
                Console.Write("  > ");
                string input = Console.ReadLine()?.Trim();
                if (input == null) continue;
                if (input.Equals("done",    StringComparison.OrdinalIgnoreCase)) break;
                if (input.Equals("cancel",  StringComparison.OrdinalIgnoreCase)) return;

                var parts = input.Split('=', 2);
                if (parts.Length != 2 || string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[1]))
                {
                    Console.WriteLine("    Invalid format. Use: StudentID=Grade  (e.g., STU001=A)");
                    continue;
                }
                rawGrades[parts[0].Trim()] = parts[1].Trim().ToUpper();
            }

            try
            {
                var result = facade.SubmitFacultyGrades(facultyId, courseId, rawGrades);
                Console.WriteLine();
                Console.WriteLine($"  Submission Summary: {result}");
                Console.WriteLine($"    Total submitted : {result.TotalSubmitted}");
                Console.WriteLine($"    Successful      : {result.SuccessCount}");
                Console.WriteLine($"    Rejected        : {result.Errors.Count}");
                foreach (var err in result.Errors)
                    Console.WriteLine($"      - {err}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
        }

        private static void FacultyGradeStatus(UniversityFacade facade)
        {
            Console.Write("Enter Course ID: ");
            string courseId = Console.ReadLine()?.Trim();

            try
            {
                var status = facade.GetCourseGradeStatus(courseId);
                Console.WriteLine($"\n  Grade Submission Status for {courseId}: {status}");
                switch (status)
                {
                    case GradeSubmissionStatus.NotSubmitted:
                        Console.WriteLine("  (No grades have been submitted yet.)");
                        break;
                    case GradeSubmissionStatus.Pending:
                        Console.WriteLine("  (Grades submitted, awaiting admin approval.)");
                        break;
                    case GradeSubmissionStatus.Submitted:
                        Console.WriteLine("  (Grades approved and finalised.)");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
        }

        private static void FacultyRequestUpdate(UniversityFacade facade)
        {
            Console.Write("Enter your Faculty ID: ");
            string facultyId = Console.ReadLine()?.Trim();
            Console.Write("Enter Course ID: ");
            string courseId = Console.ReadLine()?.Trim();
            Console.Write("Field to change (Capacity / Description / CourseName): ");
            string field = Console.ReadLine()?.Trim();
            Console.Write($"New value for {field}: ");
            string newValue = Console.ReadLine()?.Trim();

            try
            {
                var request = facade.RequestCourseUpdate(facultyId, courseId, field, newValue);
                Console.WriteLine();
                Console.WriteLine("  Change Request Submitted:");
                Console.WriteLine($"    Request ID   : {request.RequestId}");
                Console.WriteLine($"    Course       : {request.CourseId}");
                Console.WriteLine($"    Field        : {request.FieldChanged}");
                Console.WriteLine($"    Old Value    : {request.OldValue}");
                Console.WriteLine($"    New Value    : {request.NewValue}");
                Console.WriteLine($"    Status       : {request.Status}");
                Console.WriteLine("    (Awaiting administrator approval.)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
        }

        private static void FacultyViewRequests(UniversityFacade facade)
        {
            Console.Write("Enter your Faculty ID: ");
            string facultyId = Console.ReadLine()?.Trim();

            var requests = facade.GetFacultyChangeRequests(facultyId);
            Console.WriteLine($"\n  Your Change Requests ({requests.Count}):\n");
            if (requests.Count == 0)
            {
                Console.WriteLine("  (No change requests submitted yet.)");
            }
            else
            {
                foreach (var r in requests)
                {
                    Console.WriteLine($"  [{r.RequestId}] Course: {r.CourseId}");
                    Console.WriteLine($"     Field    : {r.FieldChanged}");
                    Console.WriteLine($"     Change   : '{r.OldValue}' -> '{r.NewValue}'");
                    Console.WriteLine($"     Status   : {r.Status}");
                    Console.WriteLine();
                }
            }
        }

        // =====================================================================
        // ADMINISTRATOR PORTAL
        // =====================================================================

        private static void AdminPortal(UniversityFacade facade)
        {
            bool back = false;
            while (!back)
            {
                Console.WriteLine();
                Console.WriteLine("------------------ ADMINISTRATOR PORTAL -----------------");
                Console.WriteLine(" Available admins: ADM001 (Alice Administrator),");
                Console.WriteLine("                   ADM002 (Bob Registrar)");
                Console.WriteLine("--------------------------------------------------------");
                Console.WriteLine("  [1] Generate Department Enrollment Report");
                Console.WriteLine("  [2] View Pending Course Change Requests");
                Console.WriteLine("  [3] Approve Course Change Request");
                Console.WriteLine("  [4] Reject Course Change Request");
                Console.WriteLine("  [5] Activate User Account");
                Console.WriteLine("  [6] Deactivate User Account");
                Console.WriteLine("  [7] Force Enroll Student (override capacity)");
                Console.WriteLine("  [0] Back to Main Menu");
                Console.Write("\nSelect an option [0-7]: ");

                string choice = Console.ReadLine()?.Trim();
                Console.WriteLine();

                switch (choice)
                {
                    case "1": AdminGenerateReport(facade);   break;
                    case "2": AdminViewPending(facade);       break;
                    case "3": AdminApproveChange(facade);    break;
                    case "4": AdminRejectChange(facade);      break;
                    case "5": AdminActivateUser(facade);     break;
                    case "6": AdminDeactivateUser(facade);   break;
                    case "7": AdminForceEnroll(facade);      break;
                    case "0": back = true;                    break;
                    default:  Console.WriteLine("Invalid option. Try again."); break;
                }
            }
        }

        private static void AdminGenerateReport(UniversityFacade facade)
        {
            Console.Write("Department (e.g., Computer Science): ");
            string dept = Console.ReadLine()?.Trim();
            Console.Write("Utilization threshold % (default 90, press Enter to use default): ");
            string thresholdInput = Console.ReadLine()?.Trim();

            double threshold = 90.0;
            if (double.TryParse(thresholdInput, out var t)) threshold = t;

            var report = facade.GenerateDepartmentReport(dept, threshold);
            Console.WriteLine();
            Console.WriteLine(report.ToString());
        }

        private static void AdminViewPending(UniversityFacade facade)
        {
            var pending = facade.GetPendingChangeRequests().ToList();
            Console.WriteLine($"\n  Pending Course Change Requests ({pending.Count}):\n");
            if (pending.Count == 0)
            {
                Console.WriteLine("  (No pending change requests.)");
            }
            else
            {
                foreach (var r in pending)
                {
                    Console.WriteLine($"  [{r.RequestId}] Course: {r.CourseId}");
                    Console.WriteLine($"     Requested by : {r.RequestedByFacultyId}");
                    Console.WriteLine($"     Field        : {r.FieldChanged}");
                    Console.WriteLine($"     Change       : '{r.OldValue}' -> '{r.NewValue}'");
                    Console.WriteLine($"     Requested at : {r.RequestedAt:u}");
                    Console.WriteLine();
                }
            }
        }

        private static void AdminApproveChange(UniversityFacade facade)
        {
            Console.Write("Enter Request ID (e.g., CCR-0001): ");
            string requestId = Console.ReadLine()?.Trim();
            Console.Write("Enter your Admin ID: ");
            string adminId = Console.ReadLine()?.Trim();

            bool success = facade.ApproveCourseChange(requestId, adminId);
            Console.WriteLine();
            Console.WriteLine($"  Result: {(success ? "APPROVED - course updated." : "FAILED - request not found or already processed.")}");
        }

        private static void AdminRejectChange(UniversityFacade facade)
        {
            Console.Write("Enter Request ID: ");
            string requestId = Console.ReadLine()?.Trim();
            Console.Write("Enter your Admin ID: ");
            string adminId = Console.ReadLine()?.Trim();
            Console.Write("Reason for rejection (optional): ");
            string reason = Console.ReadLine()?.Trim();

            bool success = facade.RejectCourseChange(requestId, adminId, reason);
            Console.WriteLine();
            Console.WriteLine($"  Result: {(success ? "REJECTED - faculty will be notified." : "FAILED - request not found or already processed.")}");
        }

        private static void AdminActivateUser(UniversityFacade facade)
        {
            Console.Write("Enter User ID to activate: ");
            string userId = Console.ReadLine()?.Trim();

            bool success = facade.SetUserActiveStatus(userId, true);
            Console.WriteLine($"  Result: {(success ? "User account activated." : "User not found.")}");
        }

        private static void AdminDeactivateUser(UniversityFacade facade)
        {
            Console.Write("Enter User ID to deactivate: ");
            string userId = Console.ReadLine()?.Trim();

            bool success = facade.SetUserActiveStatus(userId, false);
            Console.WriteLine($"  Result: {(success ? "User account deactivated." : "User not found.")}");
        }

        private static void AdminForceEnroll(UniversityFacade facade)
        {
            Console.Write("Enter Student ID: ");
            string studentId = Console.ReadLine()?.Trim();
            Console.Write("Enter Course ID: ");
            string courseId = Console.ReadLine()?.Trim();

            bool success = facade.ForceEnrollStudent(studentId, courseId);
            Console.WriteLine();
            Console.WriteLine($"  Result: {(success ? "FORCE ENROLLED - capacity bypassed." : "FAILED - student or course not found.")}");
        }

        // =====================================================================
        // NOTIFICATION HISTORY
        // =====================================================================

        private static void ViewNotificationHistory(UniversityFacade facade)
        {
            PrintSection("NOTIFICATION EVENT HISTORY LOG");

            var history = facade.NotificationService.GetHistory();
            Console.WriteLine($"  Total Dispatched Events: {history.Count}\n");

            if (history.Count == 0)
            {
                Console.WriteLine("  (No notifications sent yet.)");
                return;
            }

            int index = 1;
            int showCount = Math.Min(20, history.Count);
            var recent = history.Skip(Math.Max(0, history.Count - 20)).ToList();
            Console.WriteLine($"  Showing last {recent.Count} event(s):\n");

            foreach (var ev in recent)
            {
                Console.WriteLine($"  [{index++}] {ev.Timestamp:HH:mm:ss}  {ev.EventType}");
                foreach (var d in ev.Data)
                    Console.WriteLine($"        {d.Key} : {d.Value}");
                Console.WriteLine();
            }
        }

        // =====================================================================
        // FULL AUTOMATED DEMO (for screencast / integration test)
        // =====================================================================

        private static void RunFullAutomatedDemo(UniversityFacade facade)
        {
            PrintHeader("FULL AUTOMATED DEMONSTRATION (SCREENCAST MODE)");
            Console.WriteLine("This will run through all use cases automatically using");
            Console.WriteLine("scripted data. Useful for integration testing and screencasts.");
            Console.WriteLine("WARNING: This modifies state (enrollments, grades, etc.).");
            Console.Write("\nContinue? (y/n): ");
            if (!Console.ReadLine()?.Trim().Equals("y", StringComparison.OrdinalIgnoreCase) == true) return;

            // Pattern: Factory Method
            PrintSection("1. FACTORY METHOD - Creating Users");
            var demoStudent = facade.CreateAndRegisterStudent("STU999", "Demo Student", "demo.student@nexus.edu", "555-0999", "S9999", "BS-CS", 2026);
            Console.WriteLine($"  Created: {demoStudent.GetProfile()}");
            var demoFaculty = facade.CreateAndRegisterFaculty("FAC999", "Dr. Demo Faculty", "demo.faculty@nexus.edu", "555-0888", "EMP-999", "Computer Science", "Assistant Professor");
            Console.WriteLine($"  Created: {demoFaculty.GetProfile()}");
            var demoAdmin = facade.CreateAndRegisterAdmin("ADM999", "Demo Admin", "demo.admin@nexus.edu", "555-0777", "ADM-999", "Office 303", AdminScope.Full);
            Console.WriteLine($"  Created: {demoAdmin.GetProfile()}");

            // UC1: Browse Catalog
            PrintSection("2. BROWSE COURSE CATALOG");
            var csCourses = facade.BrowseCatalog(department: "Computer Science");
            Console.WriteLine($"  Computer Science courses ({csCourses.Count()}):");
            foreach (var c in csCourses)
                Console.WriteLine($"    - [{c.CourseCode}] {c.CourseName}  ({c.EnrolledCount}/{c.Capacity})");

            var algoCourses = facade.BrowseCatalog(keyword: "Algorithms");
            Console.WriteLine($"\n  Courses matching 'Algorithms' ({algoCourses.Count()}):");
            foreach (var c in algoCourses)
                Console.WriteLine($"    - [{c.CourseCode}] {c.CourseName} ({c.Department})");

            // UC2: Enrollment with SAGA
            PrintSection("3. ENROLLMENT WITH VALIDATION & SAGA ROLLBACK");

            Console.WriteLine("\n  Scenario A: Prerequisite failure");
            Console.WriteLine("    Jane Smith (STU002) tries CS201 without having completed CS101:");
            facade.EnrollStudentInCourse("STU002", "CS201", out string msgA);
            Console.WriteLine($"    -> {msgA}");

            Console.WriteLine("\n  Scenario B: Happy path");
            Console.WriteLine("    John Doe (STU001, has CS101) enrolls in CS201:");
            facade.EnrollStudentInCourse("STU001", "CS201", out string msgB);
            Console.WriteLine($"    -> {msgB}");
            var cs201 = facade.GetCourseDetails("CS201");
            Console.WriteLine($"    CS201 capacity now: {cs201.EnrolledCount}/{cs201.Capacity}");

            Console.WriteLine("\n  Scenario C: Schedule conflict");
            Console.WriteLine("    John Doe tries CS302 (overlaps with CS201 schedule):");
            facade.EnrollStudentInCourse("STU001", "CS302", out string msgC);
            Console.WriteLine($"    -> {msgC}");

            Console.WriteLine("\n  Scenario D: SAGA compensating rollback");
            Console.WriteLine("    Bob Johnson (STU003) enrolls, but Student Service 'fails':");
            facade.EnrollStudentInCourse("STU003", "CS201", out string msgD, simulateStudentFailure: true);
            Console.WriteLine($"    -> {msgD}");
            Console.WriteLine($"    CS201 capacity after rollback: {cs201.EnrolledCount}/{cs201.Capacity}");

            // UC3: Grading workflow
            PrintSection("4. FACULTY BATCH GRADING & ADMIN APPROVAL");
            facade.EnrollStudentInCourse("STU004", "CS101", out _);
            Console.WriteLine("  Roster for CS101:");
            foreach (var s in facade.GetClassRoster("FAC001", "CS101"))
                Console.WriteLine($"    - {s.FullName} ({s.UserId})");

            var grades = new Dictionary<string, string>
            {
                { "STU004", "A" },
                { "STU002", "INVALID_GRADE" },
                { "STU999", "B" }
            };
            var subResult = facade.SubmitFacultyGrades("FAC001", "CS101", grades);
            Console.WriteLine($"\n  Batch result: {subResult}");
            foreach (var err in subResult.Errors)
                Console.WriteLine($"    Rejected: {err}");

            var approval = facade.ApproveCourseGrades("CS101");
            Console.WriteLine($"\n  Approval: {approval.Success} ({approval.ApprovedCount} grade(s) finalised)");

            Console.WriteLine("  STU004 academic history after approval:");
            foreach (var r in facade.GetAcademicHistory("STU004"))
                Console.WriteLine($"    - {r.CourseCode}: {r.Grade} ({(r.IsPassing ? "Pass" : "Fail")})");

            // UC4: Admin report
            PrintSection("5. ADMIN ENROLLMENT REPORT");
            facade.EnrollStudentInCourse("STU003", "CS201", out _);
            var report = facade.GenerateDepartmentReport("Computer Science", 90.0);
            Console.WriteLine(report.ToString());

            // UC5: Drop & waitlist auto-promotion
            PrintSection("6. DROP COURSE & WAITLIST AUTO-PROMOTION");
            Console.WriteLine("  CS201 status before: " + $"{cs201.EnrolledCount}/{cs201.Capacity}");
            facade.EnrollStudentInCourse("STU004", "CS201", out _);  // fails, full
            facade.JoinWaitlist("STU004", "CS201", out _);
            Console.WriteLine("  STU004 joined waitlist for CS201.");
            Console.WriteLine("  STU001 drops CS201 -> auto-promote STU004 from waitlist:");
            facade.DropCourseAndPromoteWaitlist("STU001", "CS201", out string dropMsg);
            Console.WriteLine($"    -> {dropMsg}");
            Console.WriteLine($"  CS201 final: {cs201.EnrolledCount}/{cs201.Capacity}, waitlist: {cs201.Waitlist.Count}");

            PrintHeader("AUTOMATED DEMO COMPLETED");
        }

        // =====================================================================
        // CONSOLE FORMATTING HELPERS
        // =====================================================================

        private static void PrintHeader(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("  " + new string('=', title.Length + 4));
            Console.WriteLine("  |  " + title + "  |");
            Console.WriteLine("  " + new string('=', title.Length + 4));
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void PrintSection(string title)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("  " + new string('-', 70));
            Console.WriteLine("  >> " + title);
            Console.WriteLine("  " + new string('-', 70));
            Console.ResetColor();
        }
    }
}
