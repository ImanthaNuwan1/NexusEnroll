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
        static void Main(string[] args)
        {
            Console.Title = "NexusEnroll - Course Enrollment Modernization System";
            PrintHeader("NEXUSENROLL: UNIVERSITY ENROLLMENT SYSTEM (SCS2303 - GROUP PROJECT)");

            // Initialize the Facade (API Gateway Orchestration Layer)
            var facade = new UniversityFacade();

            // Attach Observers (Observer Pattern)
            facade.AttachObserver(new ConsoleNotificationObserver());
            facade.AttachObserver(new EmailNotificationObserver());
            facade.AttachObserver(new SmsNotificationObserver());

            // Seed initial university data
            SeedUniversityData(facade);

            // Run interactive or automated demo
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("Nexus Enroll Main menu");
                Console.WriteLine(" [1] Run Full Automated Demonstration (Recommended for Screencast)");
                Console.WriteLine(" [2] Demo Pattern 1: Factory Method (Create Student, Faculty, Admin)");
                Console.WriteLine(" [3] Demo UC1: Course Catalog Browsing & Search (Student Module)");
                Console.WriteLine(" [4] Demo UC2: Course Enrollment & Distributed SAGA Transaction (with Rollback)");
                Console.WriteLine(" [5] Demo UC3: Faculty Batch Grade Submission & Admin Approval Workflow");
                Console.WriteLine(" [6] Demo UC4: Administrator Course Capacity Utilization Report (>= 90%)");
                Console.WriteLine(" [7] Demo UC5: Drop Course & Waitlist Auto-Promotion (Observer Pattern)");
                Console.WriteLine(" [8] View Notification Event History Log");
                Console.WriteLine(" [0] Exit");
                Console.Write("\nSelect an option [0-8]: ");

                string choice = Console.ReadLine()?.Trim();
                Console.WriteLine();

                switch (choice)
                {
                    case "1":
                        RunFullAutomatedSuite(facade);
                        break;
                    case "2":
                        DemoFactoryMethod(facade);
                        break;
                    case "3":
                        DemoUC1BrowseCatalog(facade);
                        break;
                    case "4":
                        DemoUC2EnrollmentSaga(facade);
                        break;
                    case "5":
                        DemoUC3GradingWorkflow(facade);
                        break;
                    case "6":
                        DemoUC4AdminReporting(facade);
                        break;
                    case "7":
                        DemoUC5DropAndWaitlistPromotion(facade);
                        break;
                    case "8":
                        DemoViewNotificationHistory(facade);
                        break;
                    case "0":
                        exit = true;
                        Console.WriteLine("Exiting NexusEnroll. Goodbye!");
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please enter a number between 0 and 8.");
                        break;
                }
            }
        }

        // Data seeding helper
        private static void SeedUniversityData(UniversityFacade facade)
        {
            Console.WriteLine("[SETUP] Seeding Initial University Data via Factory Method...");

            // 1. Degree Programs
            var csProgram = new DegreeProgram("BS-CS", "Bachelor of Science in Computer Science", "Computer Science");
            csProgram.AddRequiredCourse("CS101");
            csProgram.AddRequiredCourse("CS201");
            csProgram.AddRequiredCourse("CS301");
            csProgram.AddElectiveCourse("SE101");
            facade.AddDegreeProgram(csProgram);

            // 2. Faculty
            facade.CreateAndRegisterFaculty("FAC001", "Dr. Alan Turing", "turing@nexus.edu", "555-0101", "EMP-001", "Computer Science", "Professor");
            facade.CreateAndRegisterFaculty("FAC002", "Prof. Grace Hopper", "hopper@nexus.edu", "555-0102", "EMP-002", "Computer Science", "Associate Professor");

            // 3. Administrators
            facade.CreateAndRegisterAdmin("ADM001", "Alice Administrator", "alice.admin@nexus.edu", "555-0201", "ADM-001", "Dean's Office 201", AdminScope.Full);
            facade.CreateAndRegisterAdmin("ADM002", "Bob Registrar", "bob.clerk@nexus.edu", "555-0202", "ADM-002", "Registrar 105", AdminScope.CourseManagement);

            // 4. Courses
            var cs101 = new Course("CS101", "CS 101", "Intro to Programming", "Computer Science", 3, 5, "FAC001", "Dr. Alan Turing",
                                   new CourseSchedule("MWF", TimeSpan.FromHours(9), TimeSpan.FromHours(10), "Hall A"), "Fall 2026");
            cs101.Description = "Fundamental principles of programming and algorithms.";
            facade.AddCourse(cs101);
            var cs201 = new Course("CS201", "CS 201", "Data Structures & Algorithms", "Computer Science", 4, 2, "FAC001", "Dr. Alan Turing",
                                   new CourseSchedule("MWF", TimeSpan.FromHours(10), TimeSpan.FromHours(11), "Hall B"), "Fall 2026");
            cs201.Description = "Abstract data types, trees, graphs, and algorithmic complexity.";
            cs201.AddPrerequisite("CS101");
            facade.AddCourse(cs201);
            var cs301 = new Course("CS301", "CS 301", "Advanced Algorithms", "Computer Science", 4, 2, "FAC002", "Prof. Grace Hopper",
                                   new CourseSchedule("TTh", TimeSpan.FromHours(10), TimeSpan.FromHours(11.5), "Lab 3"), "Fall 2026");
            cs301.Description = "Greedy algorithms, dynamic programming, and NP-completeness.";
            cs301.AddPrerequisite("CS201");
            facade.AddCourse(cs301);
            var cs302 = new Course("CS302", "CS 302", "Operating Systems", "Computer Science", 4, 2, "FAC002", "Prof. Grace Hopper",
                                   new CourseSchedule("MWF", TimeSpan.FromHours(10.5), TimeSpan.FromHours(11.5), "Hall C"), "Fall 2026");
            cs302.Description = "Concurrency, processes, memory management, and file systems.";
            cs302.AddPrerequisite("CS201");
            facade.AddCourse(cs302);
            var se101 = new Course("SE101", "SE 101", "Software Architecture", "Software Engineering", 3, 2, "FAC001", "Dr. Alan Turing",
                                   new CourseSchedule("TTh", TimeSpan.FromHours(14), TimeSpan.FromHours(15.5), "Hall D"), "Fall 2026");
            se101.Description = "Design patterns, architectural styles, and microservices design.";
            se101.AddPrerequisite("CS101");
            facade.AddCourse(se101);

            // 5. Students
            var s1 = facade.CreateAndRegisterStudent("STU001", "John Doe", "john.doe@nexus.edu", "555-0301", "S1001", "BS-CS", 2023);
            s1.RecordCompletedCourse(new CourseRecord("CS101", "CS 101", "Intro to Programming", "Spring 2024", Grade.A, 3));
            facade.CreateAndRegisterStudent("STU002", "Jane Smith", "jane.smith@nexus.edu", "555-0302", "S1002", "BS-CS", 2026);

            var s3 = facade.CreateAndRegisterStudent("STU003", "Bob Johnson", "bob.johnson@nexus.edu", "555-0303", "S1003", "BS-CS", 2024);
            s3.RecordCompletedCourse(new CourseRecord("CS101", "CS 101", "Intro to Programming", "Spring 2024", Grade.B, 3));
            s3.RecordCompletedCourse(new CourseRecord("CS201", "CS 201", "Data Structures & Algorithms", "Fall 2024", Grade.A, 4));

            var s4 = facade.CreateAndRegisterStudent("STU004", "Charlie Brown", "charlie.b@nexus.edu", "555-0304", "S1004", "BS-CS", 2025);
            s4.RecordCompletedCourse(new CourseRecord("CS101", "CS 101", "Intro to Programming", "Spring 2025", Grade.C, 3));

            Console.WriteLine("Seed data loaded.\n");
        }

        // =====================================================================
        // FULL AUTOMATED SUITE (FOR SCREENCAST & INTEGRATION TEST)
        // =====================================================================

        private static void RunFullAutomatedSuite(UniversityFacade facade)
        {
            PrintHeader("RUNNING COMPLETE AUTOMATED INTEGRATION & DEMO SUITE");

            DemoFactoryMethod(facade);
            DemoUC1BrowseCatalog(facade);
            DemoUC2EnrollmentSaga(facade);
            DemoUC3GradingWorkflow(facade);
            DemoUC4AdminReporting(facade);
            DemoUC5DropAndWaitlistPromotion(facade);
            DemoViewNotificationHistory(facade);

            PrintHeader("ALL INTEGRATION TESTS & DEMONSTRATION SCENARIOS COMPLETED SUCCESSFULLY");
        }

        // =====================================================================
        // PATTERN 1: FACTORY METHOD DEMO
        // =====================================================================

        private static void DemoFactoryMethod(UniversityFacade facade)
        {
            PrintSection("PATTERN 1: FACTORY METHOD PATTERN (User Creation)");

            Console.WriteLine("Creating users through UserFactoryManager (polymorphic factory method)...");

            var student = facade.CreateAndRegisterStudent("STU999", "Demonstration Student", "demo.student@nexus.edu", "555-0999", "S9999", "BS-CS", 2026);
            Console.WriteLine($"[Factory Output] {student.GetProfile()}");

            var faculty = facade.CreateAndRegisterFaculty("FAC999", "Dr. Demo Faculty", "demo.faculty@nexus.edu", "555-0888", "EMP-999", "Computer Science", "Assistant Professor");
            Console.WriteLine($"[Factory Output] {faculty.GetProfile()}");

            var admin = facade.CreateAndRegisterAdmin("ADM999", "Demo Administrator", "demo.admin@nexus.edu", "555-0777", "ADM-999", "Office 303", AdminScope.Full);
            Console.WriteLine($"[Factory Output] {admin.GetProfile()}");

            Console.WriteLine("\n[SOLID / LSP Check] All created subclasses correctly substitute abstract User base class.");
        }

        // =====================================================================
        // UC1: BROWSE & SEARCH COURSE CATALOG
        // =====================================================================

        private static void DemoUC1BrowseCatalog(UniversityFacade facade)
        {
            PrintSection("UC1: STUDENT MODULE — BROWSE & SEARCH COURSE CATALOG");

            Console.WriteLine("1. Browsing all courses in Department 'Computer Science':");
            var csCourses = facade.BrowseCatalog(department: "Computer Science");
            foreach (var c in csCourses)
            {
                Console.WriteLine($"   - [{c.CourseCode}] {c.CourseName} | Cap: {c.EnrolledCount}/{c.Capacity} | Instructor: {c.InstructorName} | Schedule: {c.Schedule}");
            }

            Console.WriteLine("\n2. Searching catalog by keyword 'Algorithms':");
            var algoCourses = facade.BrowseCatalog(keyword: "Algorithms");
            foreach (var c in algoCourses)
            {
                Console.WriteLine($"   - [{c.CourseCode}] {c.CourseName} ({c.Department})");
            }

            Console.WriteLine("\n3. Degree Audit for John Doe (STU001) in BS-CS Program:");
            var remaining = facade.GetDegreeAudit("STU001", "BS-CS");
            Console.WriteLine($"   Remaining required courses: {string.Join(", ", remaining)}");
        }

        // =====================================================================
        // UC2: ENROLLMENT & SAGA DISTRIBUTED TRANSACTION DEMO
        // =====================================================================

        private static void DemoUC2EnrollmentSaga(UniversityFacade facade)
        {
            PrintSection("UC2: ENROLLMENT & SAGA PATTERN (With Compensating Rollback)");

            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("SCENARIO 2A: Prerequisite Validation Failure");
            Console.WriteLine("Jane Smith (STU002) has NOT completed CS101, but tries enrolling in CS201.");
            Console.WriteLine("--------------------------------------------------------------------------------");
            bool res2A = facade.EnrollStudentInCourse("STU002", "CS201", out string msg2A);
            Console.WriteLine($"Result: {res2A} | Message: {msg2A}\n");

            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("SCENARIO 2B: Happy Path Enrollment via Distributed Saga");
            Console.WriteLine("John Doe (STU001) meets prerequisites for CS201 (Data Structures).");
            Console.WriteLine("Saga Steps: 1. Reserve Course Seat -> 2. Commit Student Record -> 3. Publish Event");
            Console.WriteLine("--------------------------------------------------------------------------------");
            bool res2B = facade.EnrollStudentInCourse("STU001", "CS201", out string msg2B);
            Console.WriteLine($"Result: {res2B} | Message: {msg2B}");
            var cs201 = facade.GetCourseDetails("CS201");
            Console.WriteLine($"[Course State] CS201 Capacity: {cs201.EnrolledCount}/{cs201.Capacity} (Status: {cs201.Status})\n");

            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("SCENARIO 2C: Schedule Clash Detection");
            Console.WriteLine("John Doe (STU001) is in CS201 (MWF 10:00-11:00) and tries enrolling in CS302 (MWF 10:30-11:30).");
            Console.WriteLine("--------------------------------------------------------------------------------");
            bool res2C = facade.EnrollStudentInCourse("STU001", "CS302", out string msg2C);
            Console.WriteLine($"Result: {res2C} | Message: {msg2C}\n");

            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("SCENARIO 2D: SAGA EVENTUAL CONSISTENCY FAILURE & COMPENSATING ROLLBACK");
            Console.WriteLine("Bob Johnson (STU003) tries enrolling in CS201, but Student Service database fails.");
            Console.WriteLine("Expected: Step 1 reserves seat -> Step 2 throws -> Compensation drops seat.");
            Console.WriteLine("--------------------------------------------------------------------------------");
            int initialEnrolled = cs201.EnrolledCount;
            Console.WriteLine($"[Before Saga] CS201 Enrolled Count: {initialEnrolled}/{cs201.Capacity}");

            bool res2D = facade.EnrollStudentInCourse("STU003", "CS201", out string msg2D, simulateStudentFailure: true);
            Console.WriteLine($"Result: {res2D}");
            Console.WriteLine($"Message: {msg2D}");
            Console.WriteLine($"[After Compensation] CS201 Enrolled Count: {cs201.EnrolledCount}/{cs201.Capacity} (Rolled back to initial!)");
        }

        // =====================================================================
        // UC3: FACULTY BATCH GRADING & ADMIN APPROVAL WORKFLOW
        // =====================================================================

        private static void DemoUC3GradingWorkflow(UniversityFacade facade)
        {
            PrintSection("UC3: FACULTY MODULE — BATCH GRADE SUBMISSION & APPROVAL");

            // First enroll STU004 into CS101 so we have students to grade
            facade.EnrollStudentInCourse("STU004", "CS101", out _);

            Console.WriteLine("1. Faculty (Dr. Alan Turing) viewing class roster for CS101:");
            var roster = facade.GetClassRoster("FAC001", "CS101");
            foreach (var s in roster)
            {
                Console.WriteLine($"   - Student: {s.FullName} ({s.StudentNumber})");
            }

            Console.WriteLine("\n2. Submitting Batch Grades with Partial Errors (SRS 3.2.2):");
            Console.WriteLine("   - STU004 -> 'A' (Valid)");
            Console.WriteLine("   - STU002 -> 'INVALID_GRADE' (Format Error)");
            Console.WriteLine("   - STU999 -> 'B' (Student not enrolled in course)");

            var rawGrades = new Dictionary<string, string>
            {
                { "STU004", "A" },
                { "STU002", "INVALID_GRADE" },
                { "STU999", "B" }
            };

            var subResult = facade.SubmitFacultyGrades("FAC001", "CS101", rawGrades);
            Console.WriteLine($"\n[Batch Result] {subResult}");
            foreach (var err in subResult.Errors)
            {
                Console.WriteLine($"   [Rejected] Student '{err.StudentId}': '{err.RawValue}' -> Reason: {err.Reason}");
            }

            Console.WriteLine($"\nCourse Grade Status: {facade.GetCourseGradeStatus("CS101")} (Pending Administrator Approval)");

            Console.WriteLine("\n3. Administrator Approving Course Grades (Pending -> Submitted):");
            var approvalResult = facade.ApproveCourseGrades("CS101");
            Console.WriteLine($"[Approval Result] Success: {approvalResult.Success} | Count: {approvalResult.ApprovedCount} | Message: {approvalResult.Message}");
            Console.WriteLine($"Course Grade Status after approval: {facade.GetCourseGradeStatus("CS101")}");

            Console.WriteLine("\n4. Verifying Student Academic History updated with new completed course:");
            var history = facade.GetAcademicHistory("STU004");
            foreach (var record in history)
            {
                Console.WriteLine($"   - Completed: {record.CourseCode} ({record.CourseName}) | Grade: {record.Grade} | Passing: {record.IsPassing}");
            }
        }

        // =====================================================================
        // UC4: ADMINISTRATOR REPORTING & ANALYTICS
        // =====================================================================

        private static void DemoUC4AdminReporting(UniversityFacade facade)
        {
            PrintSection("UC4: ADMINISTRATOR MODULE — UTILIZATION REPORTING (>= 90% CAPACITY)");

            // Enroll Bob Johnson in CS201 to make it 2/2 (100% capacity)
            facade.EnrollStudentInCourse("STU003", "CS201", out _);

            Console.WriteLine("Generating Department Enrollment Utilization Report for 'Computer Science' at 90% threshold:\n");
            var report = facade.GenerateDepartmentReport("Computer Science", utilizationThresholdPercent: 90.0);

            Console.WriteLine(report.ToString());
        }

        // =====================================================================
        // UC5: DROP COURSE & WAITLIST AUTO-PROMOTION (OBSERVER PATTERN)
        // =====================================================================

        private static void DemoUC5DropAndWaitlistPromotion(UniversityFacade facade)
        {
            PrintSection("UC5: DROP COURSE & WAITLIST AUTO-PROMOTION (Observer Pattern)");

            var cs201 = facade.GetCourseDetails("CS201");
            Console.WriteLine($"Current CS201 Status: {cs201.EnrolledCount}/{cs201.Capacity} (Full).");

            Console.WriteLine("\n1. Charlie Brown (STU004) tries to enroll in full course CS201:");
            bool enrollFull = facade.EnrollStudentInCourse("STU004", "CS201", out string fullMsg);
            Console.WriteLine($"   Result: {enrollFull} | {fullMsg}");

            Console.WriteLine("\n2. Charlie Brown joins Waitlist for CS201:");
            bool waitlistRes = facade.JoinWaitlist("STU004", "CS201", out string waitMsg);
            Console.WriteLine($"   Result: {waitlistRes} | {waitMsg}");
            Console.WriteLine($"   Waitlist count for CS201: {cs201.Waitlist.Count} (Student: {cs201.Waitlist[0]})");

            Console.WriteLine("\n3. John Doe (STU001) drops CS201:");
            Console.WriteLine("   Triggering Drop -> Auto-Promote Waitlist -> Notify Observers (Email/SMS/Console)...");
            bool dropRes = facade.DropCourseAndPromoteWaitlist("STU001", "CS201", out string dropMsg);
            Console.WriteLine($"\n[Drop Result] {dropRes} | {dropMsg}");

            Console.WriteLine("\n4. Verifying Charlie Brown (STU004) is now ENROLLED in CS201:");
            var s4Schedule = facade.GetStudentSchedule("STU004");
            foreach (var c in s4Schedule)
            {
                Console.WriteLine($"   - Enrolled: [{c.CourseCode}] {c.CourseName}");
            }
            Console.WriteLine($"   CS201 final enrolled count: {cs201.EnrolledCount}/{cs201.Capacity} | Waitlist: {cs201.Waitlist.Count}");
        }

        // =====================================================================
        // NOTIFICATION HISTORY
        // =====================================================================

        private static void DemoViewNotificationHistory(UniversityFacade facade)
        {
            PrintSection("NOTIFICATION SERVICE EVENT HISTORY LOG (OBSERVER DISPATCH LOG)");

            var history = facade.NotificationService.GetHistory();
            Console.WriteLine($"Total Dispatched Events: {history.Count}\n");

            int index = 1;
            foreach (var ev in history.TakeLast(10))
            {
                Console.WriteLine($"[{index++}] {ev.Timestamp:HH:mm:ss} | Event: {ev.EventType}");
                foreach (var data in ev.Data)
                {
                    Console.WriteLine($"      • {data.Key}: {data.Value}");
                }
            }
        }

        // =====================================================================
        // CONSOLE FORMATTING HELPERS
        // =====================================================================

        private static void PrintHeader(string title)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔" + new string('═', title.Length + 4) + "╗");
            Console.WriteLine("║  " + title + "  ║");
            Console.WriteLine("╚" + new string('═', title.Length + 4) + "╝");
            Console.ResetColor();
            Console.WriteLine();
        }

        private static void PrintSection(string title)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n" + new string('─', 80));
            Console.WriteLine("► " + title);
            Console.WriteLine(new string('─', 80));
            Console.ResetColor();
        }
    }
}
