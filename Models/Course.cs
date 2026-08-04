using System;
using System.Collections.Generic;
using System.Linq;

namespace NexusEnroll.Models
{
<<<<<<< Updated upstream

    public enum CourseStatus
    {
        Open,

        Closed,

        Cancelled
    }

    public enum Grade
    {
        A,
        B,
        C,
        D,
        F,
        W,
        I
    }

    public enum EnrollmentStatus
    {
        Enrolled,
        Waitlisted,
        Completed,
        Dropped
    }

    public enum GradeSubmissionStatus
    {
        NotSubmitted,
        Pending,
        Submitted
    }

    public static class GradeExtensions
    {
=======
    // ====================================================================================
    //  NexusEnroll — Domain Model: Course
    //
    //  Owner : Member A — Foundation & Architecture
    //  Layer : Business / Domain layer (shared model)
    //
    //  Consumed by:
    //    - Services/StudentService.cs   — browse catalogue, enrol / drop, waitlist
    //    - Services/FacultyService.cs   — roster viewing, batch grade submission
    //    - Services/AdminService.cs     — create / edit / delete courses, reports
    //    - Services/NotificationService.cs — triggers waitlist notifications
    //    - Patterns/Facade.cs           — EnrollStudentInCourse() orchestration
    //
    //  Design principles demonstrated here:
    //    * Composition over Inheritance  — Course HAS-A Prerequisites list (not IS-A)
    //    * Single Responsibility         — Course only models a course offering
    //    * Open/Closed Principle         — new statuses / grades are enum values, not edits
    //    * Encapsulate What Varies       — waitlist & prerequisite logic live inside Course
    // ====================================================================================

    /// <summary>
    /// Lifecycle status of a <see cref="Course"/> offering, controlled by the
    /// Admin Service and the enrolment engine.
    /// </summary>
    public enum CourseStatus
    {
        /// <summary>Open for enrolment — seats still available.</summary>
        Open,

        /// <summary>Enrolment closed (capacity reached or deadline passed).</summary>
        Closed,

        /// <summary>Course has been cancelled by an administrator.</summary>
        Cancelled
    }

    /// <summary>
    /// Letter grades awarded on course completion. <see cref="W"/> (Withdrawn)
    /// and <see cref="I"/> (Incomplete) are non-graded outcomes that do not
    /// satisfy prerequisite checks.
    /// </summary>
    public enum Grade
    {
        /// <summary>Excellent.</summary>
        A,
        /// <summary>Good.</summary>
        B,
        /// <summary>Satisfactory.</summary>
        C,
        /// <summary>Minimum pass.</summary>
        D,
        /// <summary>Fail — does not satisfy prerequisites.</summary>
        F,
        /// <summary>Withdrawn — does not satisfy prerequisites.</summary>
        W,
        /// <summary>Incomplete — does not satisfy prerequisites.</summary>
        I
    }

    /// <summary>
    /// State of a live <see cref="Enrollment"/> relationship between a Student
    /// and a Course for the current term.
    /// </summary>
    public enum EnrollmentStatus
    {
        /// <summary>Student holds a confirmed seat in the course.</summary>
        Enrolled,
        /// <summary>Student is on the waitlist; no seat held yet (UC5).</summary>
        Waitlisted,
        /// <summary>Course finished and a final grade has been recorded.</summary>
        Completed,
        /// <summary>Student dropped the course before completion.</summary>
        Dropped
    }

    /// <summary>
    /// Grade-submission lifecycle driven by the Faculty Service batch-grade
    /// workflow (UC3). The assignment requires a "Pending" state before a
    /// final "Submitted" state so that a validation error on one enrollment
    /// does not corrupt the rest of the batch.
    /// </summary>
    public enum GradeSubmissionStatus
    {
        /// <summary>No grade has been entered yet.</summary>
        NotSubmitted,
        /// <summary>Grade entered by faculty; awaiting validation / approval.</summary>
        Pending,
        /// <summary>Grade finalised and written to the academic record.</summary>
        Submitted
    }

    /// <summary>
    /// Extension methods for <see cref="Grade"/>. Kept here next to the enum
    /// so the prerequisite-validation rule has a single, authoritative
    /// definition of "what counts as passing".
    /// </summary>
    public static class GradeExtensions
    {
        /// <summary>
        /// Returns <c>true</c> if the grade satisfies a prerequisite
        /// (i.e. the student has genuinely passed the course).
        /// </summary>
>>>>>>> Stashed changes
        public static bool IsPassing(this Grade grade)
            => grade == Grade.A
            || grade == Grade.B
            || grade == Grade.C
            || grade == Grade.D;
    }

<<<<<<< Updated upstream
    public class CourseSchedule
    {
        public string Days { get; set; }

        public TimeSpan StartTime { get; set; }

        public TimeSpan EndTime { get; set; }

=======
    /// <summary>
    /// Weekly meeting schedule for a course offering. Modelled as a value
    /// object: two schedules with the same values are interchangeable, and the
    /// only behaviour it carries is conflict detection (used by the enrolment
    /// time-conflict rule).
    /// </summary>
    public class CourseSchedule
    {
        /// <summary>Meeting days, e.g. "MWF" (Mon/Wed/Fri) or "TR" (Tue/Thu).</summary>
        public string Days { get; set; }

        /// <summary>Start time of day.</summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>End time of day.</summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>Room / building, e.g. "NH-204".</summary>
>>>>>>> Stashed changes
        public string Location { get; set; }

        public CourseSchedule() { }

        public CourseSchedule(string days, TimeSpan start, TimeSpan end, string location)
        {
            Days      = days;
            StartTime = start;
            EndTime   = end;
            Location  = location;
        }

<<<<<<< Updated upstream
=======
        /// <summary>
        /// Returns <c>true</c> if this schedule overlaps in time <em>and</em>
        /// shares at least one meeting day with <paramref name="other"/>. This
        /// is the time-conflict validation rule consumed by UC2 (Enrol).
        /// </summary>
>>>>>>> Stashed changes
        public bool ConflictsWith(CourseSchedule other)
        {
            if (other == null) return false;
            if (string.IsNullOrEmpty(Days) || string.IsNullOrEmpty(other.Days)) return false;

<<<<<<< Updated upstream
            bool shareDay = Days.Any(d => other.Days.IndexOf(d) >= 0);
            if (!shareDay) return false;

=======
            // Shared day? (any character in common)
            bool shareDay = Days.Any(d => other.Days.IndexOf(d) >= 0);
            if (!shareDay) return false;

            // Overlapping time interval?
>>>>>>> Stashed changes
            return StartTime < other.EndTime && other.StartTime < EndTime;
        }

        public override string ToString()
            => Days + " " + StartTime + "-" + EndTime + " @ " + Location;
    }

<<<<<<< Updated upstream
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

        private readonly List<string> _prerequisiteCourseIds;

        public IReadOnlyList<string> PrerequisiteCourseIds => _prerequisiteCourseIds;

        private readonly List<string> _waitlist;

        public IReadOnlyList<string> Waitlist => _waitlist;

=======
    /// <summary>
    /// A course offering in the NexusEnroll catalogue.
    /// </summary>
    /// <remarks>
    /// <b>Composition over Inheritance:</b> a <c>Course</c> is <em>composed</em>
    /// of its <see cref="PrerequisiteCourseIds"/> (a list) rather than
    /// inheriting prerequisite behaviour from a parent class. Prerequisites
    /// are <em>data plugged into</em> the Course, not a type-of relationship —
    /// so a Course can have zero, one or many prerequisites without requiring
    /// a deep class hierarchy. This is the exact example called out in the
    /// Software Design Principles document (Composition over Inheritance).
    /// <para/>
    /// <b>Microservice boundary:</b> the instructor is referenced by ID
    /// (<see cref="InstructorId"/>) plus a denormalised display name
    /// (<see cref="InstructorName"/>) rather than holding a <c>Faculty</c>
    /// object. The Course / Student service is not authoritative for Faculty
    /// data, so it stores only what it needs to render the catalogue.
    /// </remarks>
    public class Course
    {
        /// <summary>System-wide unique identifier for this offering.</summary>
        public string CourseId { get; set; }

        /// <summary>Catalogue code, e.g. "SCS2303".</summary>
        public string CourseCode { get; set; }

        /// <summary>Human-readable name, e.g. "Software Architecture".</summary>
        public string CourseName { get; set; }

        /// <summary>Free-text description shown in the catalogue.</summary>
        public string Description { get; set; }

        /// <summary>Owning department, e.g. "Computer Science".</summary>
        public string Department { get; set; }

        /// <summary>Identifier of the assigned <c>Faculty</c> instructor.</summary>
        public string InstructorId { get; set; }

        /// <summary>Denormalised instructor name for fast catalogue display.</summary>
        public string InstructorName { get; set; }

        /// <summary>Credit hours awarded on completion.</summary>
        public int Credits { get; set; }

        /// <summary>Semester / term identifier, e.g. "2026/2".</summary>
        public string Semester { get; set; }

        /// <summary>Maximum number of students that can enrol.</summary>
        public int Capacity { get; set; }

        /// <summary>Current number of enrolled students.</summary>
        public int EnrolledCount { get; set; }

        /// <summary>Weekly meeting schedule (days, times, location).</summary>
        public CourseSchedule Schedule { get; set; }

        /// <summary>Lifecycle status of the offering.</summary>
        public CourseStatus Status { get; set; }

        /// <summary>
        /// Grade-submission state for this offering as a whole (UC3 batch
        /// workflow). Individual enrollment grades are tracked on
        /// <see cref="Enrollment.SubmissionStatus"/>.
        /// </summary>
        public GradeSubmissionStatus GradeStatus { get; set; }

        // ---- Prerequisites (composition) --------------------------------
        private readonly List<string> _prerequisiteCourseIds;

        /// <summary>
        /// Read-only view of the prerequisite course IDs. Prerequisites are
        /// composed into the Course as data — adding a prerequisite does not
        /// require a new subclass, so the design stays open for extension
        /// without modification (Open/Closed Principle).
        /// </summary>
        public IReadOnlyList<string> PrerequisiteCourseIds => _prerequisiteCourseIds;

        // ---- Waitlist (UC5) ---------------------------------------------
        private readonly List<string> _waitlist;

        /// <summary>
        /// Ordered waitlist of student IDs. The first student in this list is
        /// the next to be notified (via the Observer pattern) when a seat
        /// opens up through a drop.
        /// </summary>
        public IReadOnlyList<string> Waitlist => _waitlist;

        /// <summary>Default constructor (for serialisation / ORMs).</summary>
>>>>>>> Stashed changes
        public Course()
        {
            _prerequisiteCourseIds = new List<string>();
            _waitlist              = new List<string>();
            Status                 = CourseStatus.Open;
            GradeStatus            = GradeSubmissionStatus.NotSubmitted;
        }

<<<<<<< Updated upstream
=======
        /// <summary>Creates a fully-specified course offering.</summary>
>>>>>>> Stashed changes
        public Course(string courseId, string courseCode, string courseName,
                      string department, int credits, int capacity,
                      string instructorId, string instructorName,
                      CourseSchedule schedule, string semester)
            : this()
        {
            CourseId         = courseId;
            CourseCode       = courseCode;
            CourseName       = courseName;
            Department       = department;
            Credits          = credits;
            Capacity         = capacity;
            InstructorId     = instructorId;
            InstructorName   = instructorName;
            Schedule         = schedule;
            Semester         = semester;
            Description      = string.Empty;
        }

<<<<<<< Updated upstream

        public int AvailableSeats => Math.Max(0, Capacity - EnrolledCount);

        public bool HasAvailableSeats()
            => Status == CourseStatus.Open && EnrolledCount < Capacity;

=======
        // ---- Capacity / enrolment ---------------------------------------

        /// <summary>Number of seats still available (never negative).</summary>
        public int AvailableSeats => Math.Max(0, Capacity - EnrolledCount);

        /// <summary>True if the course still has seats and is open for enrolment.</summary>
        public bool HasAvailableSeats()
            => Status == CourseStatus.Open && EnrolledCount < Capacity;

        /// <summary>
        /// Attempts to enrol one more student. Returns <c>false</c> (without
        /// mutating state) if the course is full or not open — the caller can
        /// then fall back to waitlisting. Encapsulates the capacity rule so
        /// the enrolment engine never has to inspect <see cref="Capacity"/>
        /// and <see cref="EnrolledCount"/> directly.
        /// </summary>
>>>>>>> Stashed changes
        public bool Enroll()
        {
            if (!HasAvailableSeats()) return false;
            EnrolledCount++;
            if (EnrolledCount >= Capacity)
                Status = CourseStatus.Closed;
            return true;
        }

<<<<<<< Updated upstream
=======
        /// <summary>
        /// Releases one seat (a student dropped). Re-opens the course if it
        /// was closed purely due to capacity. Returns <c>false</c> if there
        /// was nobody enrolled to drop.
        /// </summary>
>>>>>>> Stashed changes
        public bool Drop()
        {
            if (EnrolledCount <= 0) return false;
            EnrolledCount--;
            if (Status == CourseStatus.Closed && EnrolledCount < Capacity)
                Status = CourseStatus.Open;
            return true;
        }

<<<<<<< Updated upstream

=======
        // ---- Prerequisites (composition) --------------------------------

        /// <summary>Registers another course as a prerequisite.</summary>
>>>>>>> Stashed changes
        public void AddPrerequisite(string prerequisiteCourseId)
        {
            if (prerequisiteCourseId == null) return;
            if (!_prerequisiteCourseIds.Contains(prerequisiteCourseId))
                _prerequisiteCourseIds.Add(prerequisiteCourseId);
        }

<<<<<<< Updated upstream
=======
        /// <summary>Removes a prerequisite.</summary>
>>>>>>> Stashed changes
        public void RemovePrerequisite(string prerequisiteCourseId)
        {
            if (prerequisiteCourseId == null) return;
            _prerequisiteCourseIds.Remove(prerequisiteCourseId);
        }

<<<<<<< Updated upstream
=======
        /// <summary>True if this course requires the given course as a prerequisite.</summary>
>>>>>>> Stashed changes
        public bool HasPrerequisite(string prerequisiteCourseId)
            => prerequisiteCourseId != null
               && _prerequisiteCourseIds.Contains(prerequisiteCourseId);

<<<<<<< Updated upstream
=======
        /// <summary>
        /// Returns <c>true</c> if a student who has completed the courses in
        /// <paramref name="completedCourseIds"/> satisfies every prerequisite
        /// of this offering. This is the prerequisite validation rule for UC2.
        /// </summary>
>>>>>>> Stashed changes
        public bool MeetsPrerequisites(IEnumerable<string> completedCourseIds)
        {
            if (_prerequisiteCourseIds.Count == 0) return true;
            if (completedCourseIds == null) return false;

            var completed = new HashSet<string>(completedCourseIds);
            return _prerequisiteCourseIds.All(id => completed.Contains(id));
        }

<<<<<<< Updated upstream

=======
        // ---- Waitlist (UC5) ---------------------------------------------

        /// <summary>Adds a student to the back of the waitlist.</summary>
>>>>>>> Stashed changes
        public void AddToWaitlist(string studentId)
        {
            if (studentId == null) return;
            if (!_waitlist.Contains(studentId))
                _waitlist.Add(studentId);
        }

<<<<<<< Updated upstream
=======
        /// <summary>Removes a student from the waitlist.</summary>
>>>>>>> Stashed changes
        public void RemoveFromWaitlist(string studentId)
        {
            if (studentId == null) return;
            _waitlist.Remove(studentId);
        }

<<<<<<< Updated upstream
=======
        /// <summary>
        /// Pops the next student from the front of the waitlist. The
        /// NotificationService (Observer pattern) is expected to notify this
        /// student that a seat has opened — the Course itself never sends the
        /// notification, keeping enrolment logic decoupled from delivery.
        /// </summary>
        /// <returns>The next student ID, or <c>null</c> if the waitlist is empty.</returns>
>>>>>>> Stashed changes
        public string PopNextWaitlistedStudent()
        {
            if (_waitlist.Count == 0) return null;
            string next = _waitlist[0];
            _waitlist.RemoveAt(0);
            return next;
        }

<<<<<<< Updated upstream
        public bool IsWaitlisted(string studentId)
            => studentId != null && _waitlist.Contains(studentId);

=======
        /// <summary>True if the given student is currently waitlisted.</summary>
        public bool IsWaitlisted(string studentId)
            => studentId != null && _waitlist.Contains(studentId);

        /// <summary>
        /// True if this course's schedule overlaps with
        /// <paramref name="other"/>'s schedule. Used by the enrolment
        /// time-conflict rule.
        /// </summary>
>>>>>>> Stashed changes
        public bool HasTimeConflictWith(Course other)
        {
            if (other == null || Schedule == null || other.Schedule == null) return false;
            return Schedule.ConflictsWith(other.Schedule);
        }

<<<<<<< Updated upstream
=======
        /// <inheritdoc/>
>>>>>>> Stashed changes
        public override string ToString()
            => CourseCode + " - " + CourseName
               + " (" + EnrolledCount + "/" + Capacity
               + ", " + Status + ")";
    }


<<<<<<< Updated upstream
    public class CourseRecord
    {
        public string CourseId { get; set; }

        public string CourseCode { get; set; }

        public string CourseName { get; set; }

        public string Semester { get; set; }

        public Grade Grade { get; set; }

        public int Credits { get; set; }

=======
    /// <summary>
    /// Immutable snapshot of a completed course in a student's academic
    /// history. Stored by <see cref="Student.AcademicHistory"/>.
    /// </summary>
    /// <remarks>
    /// The course name and code are <em>snapshotted</em> at completion time
    /// so that a later rename of the live <see cref="Course"/> never rewrites
    /// historical academic records — an audit / integrity requirement.
    /// </remarks>
    public class CourseRecord
    {
        /// <summary>Identifier of the completed course.</summary>
        public string CourseId { get; set; }

        /// <summary>Snapshot of the catalogue code at completion time.</summary>
        public string CourseCode { get; set; }

        /// <summary>Snapshot of the course name at completion time.</summary>
        public string CourseName { get; set; }

        /// <summary>Semester in which the course was completed.</summary>
        public string Semester { get; set; }

        /// <summary>Final grade awarded.</summary>
        public Grade Grade { get; set; }

        /// <summary>Credit hours earned (snapshot).</summary>
        public int Credits { get; set; }

        /// <summary>When the grade was finalised (UTC).</summary>
>>>>>>> Stashed changes
        public DateTime CompletedAt { get; set; }

        public CourseRecord() { }

        public CourseRecord(string courseId, string courseCode, string courseName,
                            string semester, Grade grade, int credits)
        {
            CourseId     = courseId;
            CourseCode   = courseCode;
            CourseName   = courseName;
            Semester     = semester;
            Grade        = grade;
            Credits      = credits;
            CompletedAt  = DateTime.UtcNow;
        }

<<<<<<< Updated upstream
=======
        /// <summary>Convenience accessor mirroring <see cref="GradeExtensions.IsPassing"/>.</summary>
>>>>>>> Stashed changes
        public bool IsPassing => Grade.IsPassing();
    }


<<<<<<< Updated upstream
    public class Enrollment
    {
        public string EnrollmentId { get; set; }

        public string StudentId { get; set; }

        public string CourseId { get; set; }

        public EnrollmentStatus Status { get; set; }

        public DateTime EnrolledAt { get; set; }

        public Grade? Grade { get; set; }

        public DateTime? GradedAt { get; set; }

=======
    /// <summary>
    /// Live relationship between a Student and a Course for the current term.
    /// Used by the enrolment engine (UC2 / UC5) and by the Faculty Service
    /// batch-grade workflow (UC3).
    /// </summary>
    /// <remarks>
    /// Storing the enrollment as a first-class object (rather than just a row
    /// in <c>Student.EnrolledCourseIds</c>) lets the Faculty Service track the
    /// per-student grade-submission state independently of the Course-level
    /// batch state — which is exactly what UC3 needs to recover gracefully
    /// from a single invalid grade without losing the rest of the batch.
    /// </remarks>
    public class Enrollment
    {
        /// <summary>Unique identifier for this enrollment record.</summary>
        public string EnrollmentId { get; set; }

        /// <summary>The enrolled student's ID.</summary>
        public string StudentId { get; set; }

        /// <summary>The course's ID.</summary>
        public string CourseId { get; set; }

        /// <summary>Current status of the enrollment.</summary>
        public EnrollmentStatus Status { get; set; }

        /// <summary>When the enrollment was first created (UTC).</summary>
        public DateTime EnrolledAt { get; set; }

        /// <summary>
        /// Final grade, or <c>null</c> until the Faculty Service finalises it.
        /// </summary>
        public Grade? Grade { get; set; }

        /// <summary>When the grade was finalised (UTC).</summary>
        public DateTime? GradedAt { get; set; }

        /// <summary>
        /// Per-enrollment grade-submission state (UC3 batch workflow:
        /// NotSubmitted &#8594; Pending &#8594; Submitted).
        /// </summary>
>>>>>>> Stashed changes
        public GradeSubmissionStatus SubmissionStatus { get; set; }

        public Enrollment() { }

        public Enrollment(string enrollmentId, string studentId, string courseId,
                          EnrollmentStatus status = EnrollmentStatus.Enrolled)
        {
            EnrollmentId     = enrollmentId;
            StudentId        = studentId;
            CourseId         = courseId;
            Status           = status;
            EnrolledAt       = DateTime.UtcNow;
            SubmissionStatus = GradeSubmissionStatus.NotSubmitted;
        }

<<<<<<< Updated upstream
=======
        /// <summary>
        /// Marks a grade as entered by faculty and pending validation / approval
        /// (UC3). Does not yet write to the academic record.
        /// </summary>
>>>>>>> Stashed changes
        public void SubmitGradePending(Grade grade)
        {
            Grade            = grade;
            SubmissionStatus = GradeSubmissionStatus.Pending;
        }

<<<<<<< Updated upstream
=======
        /// <summary>
        /// Finalises the pending grade: marks the enrollment Completed, stamps
        /// the grade time, and flips the submission state to Submitted. The
        /// Student Service is then responsible for appending a
        /// <see cref="CourseRecord"/> to the student's academic history.
        /// </summary>
>>>>>>> Stashed changes
        public void FinaliseGrade()
        {
            if (SubmissionStatus != GradeSubmissionStatus.Pending)
                throw new InvalidOperationException(
                    "Cannot finalise a grade that has not been submitted as Pending.");
            if (Grade == null)
                throw new InvalidOperationException("Cannot finalise a null grade.");

            Status           = EnrollmentStatus.Completed;
            GradedAt         = DateTime.UtcNow;
            SubmissionStatus = GradeSubmissionStatus.Submitted;
        }

<<<<<<< Updated upstream
=======
        /// <summary>
        /// Drops the enrollment (student-initiated drop, or admin override).
        /// </summary>
>>>>>>> Stashed changes
        public void Drop()
        {
            Status = EnrollmentStatus.Dropped;
        }

<<<<<<< Updated upstream
=======
        /// <inheritdoc/>
>>>>>>> Stashed changes
        public override string ToString()
            => "Enrollment[" + StudentId + " -> " + CourseId
               + ", " + Status + ", " + SubmissionStatus + "]";
    }
}
