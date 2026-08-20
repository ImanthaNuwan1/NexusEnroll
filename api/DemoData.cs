using NexusEnroll.Models;
using NexusEnroll.Patterns;

namespace NexusEnroll.Api;

// Mirrors backend/Program.cs's console-demo seed data (same IDs/emails) so the
// web frontend and the console demo show the same university out of the box.
public static class DemoData
{
    public static void Seed(UniversityFacade facade)
    {
        var csProgram = new DegreeProgram("BS-CS", "BSc in Computer Science", "Computer Science");
        csProgram.AddRequiredCourse("CS101");
        csProgram.AddRequiredCourse("CS201");
        csProgram.AddRequiredCourse("CS301");
        csProgram.AddElectiveCourse("SE101");
        facade.AddDegreeProgram(csProgram);

        facade.CreateAndRegisterFaculty("FAC001", "Dr. Alan Turing", "turing@nexus.edu", "555-0101", "EMP-001", "Computer Science", "Professor");
        facade.CreateAndRegisterFaculty("FAC002", "Prof. Grace Hopper", "hopper@nexus.edu", "555-0102", "EMP-002", "Computer Science", "Associate Professor");

        facade.CreateAndRegisterAdmin("ADM001", "Alice Administrator", "alice.admin@nexus.edu", "555-0201", "ADM-001", "Dean's Office 201", AdminScope.Full);
        facade.CreateAndRegisterAdmin("ADM002", "Bob Registrar", "bob.clerk@nexus.edu", "555-0202", "ADM-002", "Registrar 105", AdminScope.CourseManagement);

        var cs101 = new Course("CS101", "CS 101", "Intro to Programming", "Computer Science", 3, 5,
            "FAC001", "Dr. Alan Turing",
            new CourseSchedule("MWF", TimeSpan.FromHours(9), TimeSpan.FromHours(10), "Hall A"), "Fall 2026");
        cs101.Description = "Fundamental principles of programming and algorithms.";
        facade.AddCourse(cs101);

        var cs201 = new Course("CS201", "CS 201", "Data Structures & Algorithms", "Computer Science", 4, 2,
            "FAC001", "Dr. Alan Turing",
            new CourseSchedule("MWF", TimeSpan.FromHours(10), TimeSpan.FromHours(11), "Hall B"), "Fall 2026");
        cs201.Description = "Abstract data types, trees, graphs, and algorithmic complexity.";
        cs201.AddPrerequisite("CS101");
        facade.AddCourse(cs201);

        var cs301 = new Course("CS301", "CS 301", "Advanced Algorithms", "Computer Science", 4, 2,
            "FAC002", "Prof. Grace Hopper",
            new CourseSchedule("TTh", TimeSpan.FromHours(10), TimeSpan.FromHours(11.5), "Lab 3"), "Fall 2026");
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
            new CourseSchedule("TTh", TimeSpan.FromHours(14), TimeSpan.FromHours(15.5), "Hall D"), "Fall 2026");
        se101.Description = "Design patterns, architectural styles, and microservices design.";
        se101.AddPrerequisite("CS101");
        facade.AddCourse(se101);

        var s1 = facade.CreateAndRegisterStudent("STU001", "John Doe", "john.doe@nexus.edu", "555-0301", "S1001", "BS-CS", 2023);
        s1.RecordCompletedCourse(new CourseRecord("CS101", "CS 101", "Intro to Programming", "Spring 2024", Grade.A, 3));

        facade.CreateAndRegisterStudent("STU002", "Jane Smith", "jane.smith@nexus.edu", "555-0302", "S1002", "BS-CS", 2026);

        var s3 = facade.CreateAndRegisterStudent("STU003", "Bob Johnson", "bob.johnson@nexus.edu", "555-0303", "S1003", "BS-CS", 2024);
        s3.RecordCompletedCourse(new CourseRecord("CS101", "CS 101", "Intro to Programming", "Spring 2024", Grade.B, 3));
        s3.RecordCompletedCourse(new CourseRecord("CS201", "CS 201", "Data Structures & Algorithms", "Fall 2024", Grade.A, 4));

        var s4 = facade.CreateAndRegisterStudent("STU004", "Charlie Brown", "charlie.b@nexus.edu", "555-0304", "S1004", "BS-CS", 2025);
        s4.RecordCompletedCourse(new CourseRecord("CS101", "CS 101", "Intro to Programming", "Spring 2025", Grade.C, 3));
    }
}
