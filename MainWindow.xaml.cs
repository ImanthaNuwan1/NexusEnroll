using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NexusEnroll.Models;
using NexusEnroll.Patterns;
using NexusEnroll.Services;

namespace NexusEnroll
{
    public partial class MainWindow : Window
    {
        private readonly UniversityFacade _facade;
        private User _activeUser;
        private ObservableCollection<NotificationViewModel> _notifications = new ObservableCollection<NotificationViewModel>();
        private ObservableCollection<GradeInputViewModel> _currentRoster = new ObservableCollection<GradeInputViewModel>();

        public MainWindow()
        {
            InitializeComponent();

            _facade = new UniversityFacade();

            // Attach notification observers
            _facade.AttachObserver(new ConsoleNotificationObserver());
            _facade.AttachObserver(new EmailNotificationObserver());
            _facade.AttachObserver(new SmsNotificationObserver());
            _facade.AttachObserver(new GuiNotificationObserver(this));

            // Seed data
            SeedData();

            // Set notifications items source
            GridNotifications.ItemsSource = _notifications;

            // Load user dropdown
            LoadUsers();
            RefreshAllViews();
        }

        private void SeedData()
        {
            // Degree Program
            var csProgram = new DegreeProgram("BS-CS", "BSc in Computer Science", "Computer Science");
            csProgram.AddRequiredCourse("CS101");
            csProgram.AddRequiredCourse("CS201");
            csProgram.AddRequiredCourse("CS301");
            csProgram.AddElectiveCourse("SE101");
            _facade.AddDegreeProgram(csProgram);

            // Faculty
            _facade.CreateAndRegisterFaculty("FAC001", "Dr. Alan Turing", "turing@nexus.edu", "555-0101", "EMP-001", "Computer Science", "Professor");
            _facade.CreateAndRegisterFaculty("FAC002", "Prof. Grace Hopper", "hopper@nexus.edu", "555-0102", "EMP-002", "Computer Science", "Associate Professor");

            // Administrators
            _facade.CreateAndRegisterAdmin("ADM001", "Alice Administrator", "alice.admin@nexus.edu", "555-0201", "ADM-001", "Dean's Office 201", AdminScope.Full);
            _facade.CreateAndRegisterAdmin("ADM002", "Bob Registrar", "bob.clerk@nexus.edu", "555-0202", "ADM-002", "Registrar 105", AdminScope.CourseManagement);

            // Courses
            var cs101 = new Course("CS101", "CS 101", "Intro to Programming", "Computer Science", 3, 5,
                "FAC001", "Dr. Alan Turing",
                new CourseSchedule("MWF", TimeSpan.FromHours(9), TimeSpan.FromHours(10), "Hall A"), "Fall 2026");
            cs101.Description = "Fundamental principles of programming and algorithms.";
            _facade.AddCourse(cs101);

            var cs201 = new Course("CS201", "CS 201", "Data Structures & Algorithms", "Computer Science", 4, 2,
                "FAC001", "Dr. Alan Turing",
                new CourseSchedule("MWF", TimeSpan.FromHours(10), TimeSpan.FromHours(11), "Hall B"), "Fall 2026");
            cs201.Description = "Abstract data types, trees, graphs, and algorithmic complexity.";
            cs201.AddPrerequisite("CS101");
            _facade.AddCourse(cs201);

            var cs301 = new Course("CS301", "CS 301", "Advanced Algorithms", "Computer Science", 4, 2,
                "FAC002", "Prof. Grace Hopper",
                new CourseSchedule("TTh", TimeSpan.FromHours(10), TimeSpan.FromHours(11.5), "Lab 3"), "Fall 2026");
            cs301.Description = "Greedy algorithms, dynamic programming, and NP-completeness.";
            cs301.AddPrerequisite("CS201");
            _facade.AddCourse(cs301);

            var cs302 = new Course("CS302", "CS 302", "Operating Systems", "Computer Science", 4, 2,
                "FAC002", "Prof. Grace Hopper",
                new CourseSchedule("MWF", TimeSpan.FromHours(10.5), TimeSpan.FromHours(11.5), "Hall C"), "Fall 2026");
            cs302.Description = "Concurrency, processes, memory management, and file systems.";
            cs302.AddPrerequisite("CS201");
            _facade.AddCourse(cs302);

            var se101 = new Course("SE101", "SE 101", "Software Architecture", "Software Engineering", 3, 2,
                "FAC001", "Dr. Alan Turing",
                new CourseSchedule("TTh", TimeSpan.FromHours(14), TimeSpan.FromHours(15.5), "Hall D"), "Fall 2026");
            se101.Description = "Design patterns, architectural styles, and microservices design.";
            se101.AddPrerequisite("CS101");
            _facade.AddCourse(se101);

            // Students
            var s1 = _facade.CreateAndRegisterStudent("STU001", "John Doe", "john.doe@nexus.edu", "555-0301", "S1001", "BS-CS", 2023);
            s1.RecordCompletedCourse(new CourseRecord("CS101", "CS 101", "Intro to Programming", "Spring 2024", Grade.A, 3));

            _facade.CreateAndRegisterStudent("STU002", "Jane Smith", "jane.smith@nexus.edu", "555-0302", "S1002", "BS-CS", 2026);

            var s3 = _facade.CreateAndRegisterStudent("STU003", "Bob Johnson", "bob.johnson@nexus.edu", "555-0303", "S1003", "BS-CS", 2024);
            s3.RecordCompletedCourse(new CourseRecord("CS101", "CS 101", "Intro to Programming", "Spring 2024", Grade.B, 3));
            s3.RecordCompletedCourse(new CourseRecord("CS201", "CS 201", "Data Structures & Algorithms", "Fall 2024", Grade.A, 4));

            var s4 = _facade.CreateAndRegisterStudent("STU004", "Charlie Brown", "charlie.b@nexus.edu", "555-0304", "S1004", "BS-CS", 2025);
            s4.RecordCompletedCourse(new CourseRecord("CS101", "CS 101", "Intro to Programming", "Spring 2025", Grade.C, 3));

            // Seed initial department combobox items
            CmbReportDept.Items.Add("Computer Science");
            CmbReportDept.Items.Add("Software Engineering");
            CmbReportDept.SelectedIndex = 0;
        }

        private void LoadUsers()
        {
            var userList = _facade.Users.Values
                .Select(u => new UserItem { User = u, DisplayName = $"[{u.Role}] {u.FullName} ({u.UserId})" })
                .OrderBy(u => u.User.Role)
                .ThenBy(u => u.User.UserId)
                .ToList();

            CmbUsers.ItemsSource = userList;
            if (userList.Count > 0)
                CmbUsers.SelectedIndex = 0;
        }

        private void CmbUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbUsers.SelectedItem is UserItem item)
            {
                _activeUser = item.User;
                TxtRoleBadge.Text = _activeUser.Role.ToString().ToUpper();

                switch (_activeUser.Role)
                {
                    case UserRole.Student:
                        BadgeRole.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
                        break;
                    case UserRole.Faculty:
                        BadgeRole.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));
                        break;
                    case UserRole.Admin:
                        BadgeRole.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
                        break;
                }

                RefreshAllViews();
            }
        }

        private void RefreshAllViews()
        {
            RefreshCatalog();
            RefreshStudentPortal();
            RefreshFacultyPortal();
            RefreshAdminPortal();
        }

        // =====================================================================
        // STUDENT PORTAL LOGIC
        // =====================================================================

        private void RefreshCatalog()
        {
            string dept = string.IsNullOrWhiteSpace(TxtFilterDept?.Text) ? null : TxtFilterDept.Text.Trim();
            string keyword = string.IsNullOrWhiteSpace(TxtFilterKeyword?.Text) ? null : TxtFilterKeyword.Text.Trim();
            string instructor = string.IsNullOrWhiteSpace(TxtFilterInstructor?.Text) ? null : TxtFilterInstructor.Text.Trim();

            var courses = _facade.BrowseCatalog(department: dept, keyword: keyword, instructorName: instructor)
                .Select(c => new CourseViewModel(c))
                .ToList();

            GridCatalog.ItemsSource = courses;
        }

        private void BtnSearchCatalog_Click(object sender, RoutedEventArgs e)
        {
            RefreshCatalog();
            ShowStatus("Catalog filtered.", isError: false);
        }

        private void RefreshStudentPortal()
        {
            if (_activeUser is Student student)
            {
                var schedule = _facade.GetStudentSchedule(student.UserId)
                    .Select(c => new CourseViewModel(c))
                    .ToList();
                GridStudentSchedule.ItemsSource = schedule;

                var history = _facade.GetAcademicHistory(student.UserId)
                    .Select(r => new CourseRecordViewModel(r))
                    .ToList();
                GridAcademicHistory.ItemsSource = history;

                var remaining = _facade.GetDegreeAudit(student.UserId, student.ProgramId).ToList();
                TxtDegreeAuditTitle.Text = $"Degree Audit Program: {student.ProgramId}";
                LstDegreeAudit.ItemsSource = remaining;
                TxtDegreeAuditSummary.Text = remaining.Count == 0
                    ? "🎉 All required courses completed! Eligible for graduation."
                    : $"Remaining required courses ({remaining.Count}):";
            }
            else
            {
                GridStudentSchedule.ItemsSource = null;
                GridAcademicHistory.ItemsSource = null;
                LstDegreeAudit.ItemsSource = null;
                TxtDegreeAuditSummary.Text = "(Active user is not a Student)";
            }
        }

        private void BtnEnroll_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateActiveStudent(out var student)) return;
            if (GridCatalog.SelectedItem is CourseViewModel courseVM)
            {
                bool ok = _facade.EnrollStudentInCourse(student.UserId, courseVM.CourseId, out string msg);
                ShowStatus(msg, !ok);
                RefreshAllViews();
            }
            else
            {
                ShowStatus("Please select a course from the catalog table to enroll.", isError: true);
            }
        }

        private void BtnDrop_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateActiveStudent(out var student)) return;
            if (GridCatalog.SelectedItem is CourseViewModel courseVM)
            {
                bool ok = _facade.DropCourseAndPromoteWaitlist(student.UserId, courseVM.CourseId, out string msg);
                ShowStatus(msg, !ok);
                RefreshAllViews();
            }
            else
            {
                ShowStatus("Please select a course from the catalog table to drop.", isError: true);
            }
        }

        private void BtnJoinWaitlist_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateActiveStudent(out var student)) return;
            if (GridCatalog.SelectedItem is CourseViewModel courseVM)
            {
                bool ok = _facade.JoinWaitlist(student.UserId, courseVM.CourseId, out string msg);
                ShowStatus(msg, !ok);
                RefreshAllViews();
            }
            else
            {
                ShowStatus("Please select a full course from the catalog table to join waitlist.", isError: true);
            }
        }

        private bool ValidateActiveStudent(out Student student)
        {
            if (_activeUser is Student s)
            {
                student = s;
                return true;
            }
            student = null;
            ShowStatus("Action rejected: Active user is not a Student. Switch user in top header.", isError: true);
            return false;
        }

        // =====================================================================
        // FACULTY PORTAL LOGIC
        // =====================================================================

        private void RefreshFacultyPortal()
        {
            if (_activeUser is Faculty faculty)
            {
                try
                {
                    var courses = _facade.GetFacultyTeachingSchedule(faculty.UserId);
                    LstFacultyCourses.ItemsSource = courses.Select(c => new CourseViewModel(c)).ToList();
                    if (courses.Count > 0 && LstFacultyCourses.SelectedIndex < 0)
                        LstFacultyCourses.SelectedIndex = 0;

                    var requests = _facade.GetFacultyChangeRequests(faculty.UserId);
                    GridFacultyRequests.ItemsSource = requests;
                }
                catch
                {
                    LstFacultyCourses.ItemsSource = null;
                    GridFacultyRequests.ItemsSource = null;
                }
            }
            else
            {
                LstFacultyCourses.ItemsSource = null;
                GridFacultyRequests.ItemsSource = null;
                GridClassRoster.ItemsSource = null;
                TxtSelectedCourseHeader.Text = "Class Roster - Select a Course (Switch to a Faculty user)";
            }
        }

        private void LstFacultyCourses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstFacultyCourses.SelectedItem is CourseViewModel courseVM && _activeUser is Faculty faculty)
            {
                TxtSelectedCourseHeader.Text = $"Class Roster - {courseVM.CourseCode} ({courseVM.CourseName})";

                try
                {
                    var roster = _facade.GetClassRoster(faculty.UserId, courseVM.CourseId);
                    _currentRoster = new ObservableCollection<GradeInputViewModel>(
                        roster.Select(s => new GradeInputViewModel { StudentId = s.UserId, FullName = s.FullName, Email = s.Email, ProgramId = s.ProgramId }));
                    GridClassRoster.ItemsSource = _currentRoster;

                    var gradeStatus = _facade.GetCourseGradeStatus(courseVM.CourseId);
                    TxtGradeStatus.Text = $"Grade Submission Status: {gradeStatus}";
                }
                catch (Exception ex)
                {
                    GridClassRoster.ItemsSource = null;
                    TxtGradeStatus.Text = $"Error: {ex.Message}";
                }
            }
        }

        private void BtnSubmitBatchGrades_Click(object sender, RoutedEventArgs e)
        {
            if (!(_activeUser is Faculty faculty))
            {
                ShowStatus("Action rejected: Active user is not Faculty.", isError: true);
                return;
            }

            if (!(LstFacultyCourses.SelectedItem is CourseViewModel courseVM))
            {
                ShowStatus("Select a course from your teaching schedule first.", isError: true);
                return;
            }

            var rawGrades = new Dictionary<string, string>();
            foreach (var item in _currentRoster)
            {
                if (!string.IsNullOrWhiteSpace(item.GradeInput))
                {
                    rawGrades[item.StudentId] = item.GradeInput.Trim().ToUpper();
                }
            }

            var result = _facade.SubmitFacultyGrades(faculty.UserId, courseVM.CourseId, rawGrades);
            ShowStatus(result.ToString(), !result.AllSucceeded);
            RefreshFacultyPortal();
        }

        private void BtnSubmitChangeRequest_Click(object sender, RoutedEventArgs e)
        {
            if (!(_activeUser is Faculty faculty))
            {
                ShowStatus("Action rejected: Active user is not Faculty.", isError: true);
                return;
            }

            if (!(LstFacultyCourses.SelectedItem is CourseViewModel courseVM))
            {
                ShowStatus("Select a course from your teaching schedule first.", isError: true);
                return;
            }

            string field = (CmbChangeField.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Capacity";
            string newValue = TxtNewFieldValue.Text?.Trim();

            if (string.IsNullOrWhiteSpace(newValue))
            {
                ShowStatus("Specify a new value for the field.", isError: true);
                return;
            }

            try
            {
                var request = _facade.RequestCourseUpdate(faculty.UserId, courseVM.CourseId, field, newValue);
                ShowStatus($"Submitted request {request.RequestId} for {courseVM.CourseCode} ({field} -> '{newValue}').", isError: false);
                TxtNewFieldValue.Text = "";
                RefreshFacultyPortal();
                RefreshAdminPortal();
            }
            catch (Exception ex)
            {
                ShowStatus($"Request failed: {ex.Message}", isError: true);
            }
        }

        // =====================================================================
        // ADMINISTRATOR PORTAL LOGIC
        // =====================================================================

        private void RefreshAdminPortal()
        {
            var pending = _facade.GetPendingChangeRequests().ToList();
            GridPendingRequests.ItemsSource = pending;

            var users = _facade.Users.Values
                .Select(u => new UserViewModel(u))
                .OrderBy(u => u.Role)
                .ThenBy(u => u.UserId)
                .ToList();
            GridUsers.ItemsSource = users;
        }

        private void BtnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            string dept = CmbReportDept.Text?.Trim() ?? "Computer Science";
            double threshold = 90.0;
            if (double.TryParse(TxtReportThreshold.Text?.Trim(), out var t)) threshold = t;

            var report = _facade.GenerateDepartmentReport(dept, threshold);
            TxtReportOutput.Text = report.ToString();
            ShowStatus($"Generated report for {dept} at {threshold}% threshold.", isError: false);
        }

        private void BtnApproveRequest_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateActiveAdmin(out var admin)) return;
            if (GridPendingRequests.SelectedItem is CourseChangeRequest request)
            {
                bool ok = _facade.ApproveCourseChange(request.RequestId, admin.UserId);
                ShowStatus(ok ? $"Approved request {request.RequestId}." : "Failed to approve request.", !ok);
                RefreshAllViews();
            }
            else
            {
                ShowStatus("Select a pending request from the table to approve.", isError: true);
            }
        }

        private void BtnRejectRequest_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateActiveAdmin(out var admin)) return;
            if (GridPendingRequests.SelectedItem is CourseChangeRequest request)
            {
                bool ok = _facade.RejectCourseChange(request.RequestId, admin.UserId, "Rejected by Admin via GUI");
                ShowStatus(ok ? $"Rejected request {request.RequestId}." : "Failed to reject request.", !ok);
                RefreshAllViews();
            }
            else
            {
                ShowStatus("Select a pending request from the table to reject.", isError: true);
            }
        }

        private void BtnActivateUser_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateActiveAdmin(out _)) return;
            if (GridUsers.SelectedItem is UserViewModel uVM)
            {
                bool ok = _facade.SetUserActiveStatus(uVM.UserId, true);
                ShowStatus(ok ? $"Activated account for {uVM.FullName}." : "Failed.", !ok);
                LoadUsers();
                RefreshAllViews();
            }
            else
            {
                ShowStatus("Select a user from the account table to activate.", isError: true);
            }
        }

        private void BtnDeactivateUser_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateActiveAdmin(out _)) return;
            if (GridUsers.SelectedItem is UserViewModel uVM)
            {
                bool ok = _facade.SetUserActiveStatus(uVM.UserId, false);
                ShowStatus(ok ? $"Deactivated account for {uVM.FullName}." : "Failed.", !ok);
                LoadUsers();
                RefreshAllViews();
            }
            else
            {
                ShowStatus("Select a user from the account table to deactivate.", isError: true);
            }
        }

        private void BtnCreateStudent_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateActiveAdmin(out _)) return;
            var dialog = new CreateStudentDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var s = _facade.CreateStudentAccount(
                        dialog.UserId, dialog.FullName, dialog.Email, dialog.Phone,
                        dialog.StudentNumber, dialog.ProgramId, dialog.EnrolledYear);
                    ShowStatus($"Created Student account: {s.FullName} ({s.UserId}).", isError: false);
                    LoadUsers();
                    RefreshAllViews();
                }
                catch (Exception ex)
                {
                    ShowStatus($"Failed to create student: {ex.Message}", isError: true);
                }
            }
        }

        private void BtnCreateFaculty_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateActiveAdmin(out _)) return;
            var dialog = new CreateFacultyDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var f = _facade.CreateFacultyAccount(
                        dialog.UserId, dialog.FullName, dialog.Email, dialog.Phone,
                        dialog.EmployeeNumber, dialog.Department, dialog.Rank);
                    ShowStatus($"Created Faculty account: {f.FullName} ({f.UserId}).", isError: false);
                    LoadUsers();
                    RefreshAllViews();
                }
                catch (Exception ex)
                {
                    ShowStatus($"Failed to create faculty: {ex.Message}", isError: true);
                }
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateActiveAdmin(out _)) return;
            if (GridUsers.SelectedItem is UserViewModel uVM)
            {
                if (uVM.Role == "Admin")
                {
                    ShowStatus("Administrator accounts cannot be deleted.", isError: true);
                    return;
                }

                var res = MessageBox.Show($"Are you sure you want to delete {uVM.Role} account '{uVM.FullName}' ({uVM.UserId})?", "Confirm Delete Account", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    bool ok = false;
                    if (uVM.Role == "Student")
                        ok = _facade.DeleteStudentAccount(uVM.UserId);
                    else if (uVM.Role == "Faculty")
                        ok = _facade.DeleteFacultyAccount(uVM.UserId);

                    ShowStatus(ok ? $"Deleted {uVM.Role} account for {uVM.FullName}." : "Delete failed.", !ok);
                    LoadUsers();
                    RefreshAllViews();
                }
            }
            else
            {
                ShowStatus("Select a user from the account table to delete.", isError: true);
            }
        }

        private void BtnForceEnroll_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateActiveAdmin(out _)) return;

            // Prompt for Student and Course IDs
            var dialog = new ForceEnrollDialog(_facade.Users.Values.OfType<Student>(), _facade.Courses.Values);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                bool ok = _facade.ForceEnrollStudent(dialog.SelectedStudentId, dialog.SelectedCourseId);
                ShowStatus(ok ? $"Force enrolled {dialog.SelectedStudentId} in {dialog.SelectedCourseId}." : "Force enrollment failed.", !ok);
                RefreshAllViews();
            }
        }

        private bool ValidateActiveAdmin(out Admin admin)
        {
            if (_activeUser is Admin a)
            {
                admin = a;
                return true;
            }
            admin = null;
            ShowStatus("Action rejected: Active user is not an Administrator. Switch user in header.", isError: true);
            return false;
        }

        // =====================================================================
        // NOTIFICATIONS & OBSERVER LOGIC
        // =====================================================================

        public void AddNotification(NotificationEvent ev)
        {
            Dispatcher.Invoke(() =>
            {
                _notifications.Insert(0, new NotificationViewModel(ev));
            });
        }

        private void BtnRefreshNotifications_Click(object sender, RoutedEventArgs e)
        {
            _notifications.Clear();
            foreach (var ev in _facade.NotificationService.GetHistory().Reverse())
            {
                _notifications.Add(new NotificationViewModel(ev));
            }
            ShowStatus("Notification history refreshed.", isError: false);
        }

        private void ShowStatus(string message, bool isError)
        {
            TxtStatusMessage.Text = message;
            TxtStatusIcon.Text = isError ? "⚠️" : "✅";
            BannerStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isError ? "#FEF2F2" : "#EFF6FF"));
            BannerStatus.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isError ? "#FCA5A5" : "#BFDBFE"));
            TxtStatusMessage.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isError ? "#991B1B" : "#1E40AF"));
            BannerStatus.Visibility = Visibility.Visible;
        }
    }

    // =========================================================================
    // GUI NOTIFICATION OBSERVER IMPLEMENTATION
    // =========================================================================

    public class GuiNotificationObserver : INotificationObserver
    {
        private readonly MainWindow _mainWindow;
        public string ObserverName => "GuiNotificationObserver";

        public GuiNotificationObserver(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void Update(NotificationEvent notificationEvent)
        {
            _mainWindow.AddNotification(notificationEvent);
        }
    }

    // =========================================================================
    // VIEW MODELS & HELPER CLASSES
    // =========================================================================

    public class UserItem
    {
        public User User { get; set; }
        public string DisplayName { get; set; }
    }

    public class UserViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string StatusDisplay { get; set; }

        public UserViewModel(User u)
        {
            UserId = u.UserId;
            FullName = u.FullName;
            Email = u.Email;
            Role = u.Role.ToString();
            StatusDisplay = u.IsActive ? "Active" : "Inactive";
        }
    }

    public class CourseViewModel
    {
        public string CourseId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string CourseCodeAndName => $"[{CourseCode}] {CourseName}";
        public string Department { get; set; }
        public int Credits { get; set; }
        public string Schedule { get; set; }
        public string InstructorName { get; set; }
        public string EnrolledDisplay => $"{EnrolledCount}/{Capacity}";
        public int EnrolledCount { get; set; }
        public int Capacity { get; set; }
        public string Status { get; set; }
        public string WaitlistCountDisplay => WaitlistCount.ToString();
        public int WaitlistCount { get; set; }

        public CourseViewModel(Course c)
        {
            CourseId = c.CourseId;
            CourseCode = c.CourseCode;
            CourseName = c.CourseName;
            Department = c.Department;
            Credits = c.Credits;
            Schedule = c.Schedule?.ToString() ?? "TBA";
            InstructorName = c.InstructorName;
            EnrolledCount = c.EnrolledCount;
            Capacity = c.Capacity;
            Status = c.Status.ToString();
            WaitlistCount = c.Waitlist.Count;
        }
    }

    public class CourseRecordViewModel
    {
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Semester { get; set; }
        public string Grade { get; set; }
        public string ResultDisplay { get; set; }
        public int Credits { get; set; }

        public CourseRecordViewModel(CourseRecord r)
        {
            CourseCode = r.CourseCode;
            CourseName = r.CourseName;
            Semester = r.Semester;
            Grade = r.Grade.ToString();
            ResultDisplay = r.IsPassing ? "Pass" : "Fail";
            Credits = r.Credits;
        }
    }

    public class GradeInputViewModel
    {
        public string StudentId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ProgramId { get; set; }
        public string GradeInput { get; set; }
    }

    public class NotificationViewModel
    {
        public string TimestampDisplay { get; set; }
        public string EventType { get; set; }
        public string RecipientDisplay { get; set; }
        public string DetailsDisplay { get; set; }

        public NotificationViewModel(NotificationEvent ev)
        {
            TimestampDisplay = ev.Timestamp.ToString("HH:mm:ss");
            EventType = ev.EventType;

            string email = ev.Get("RecipientEmail")?.ToString();
            string phone = ev.Get("RecipientPhone")?.ToString();
            RecipientDisplay = !string.IsNullOrEmpty(email) ? email : (!string.IsNullOrEmpty(phone) ? phone : "(System Broadcaster)");

            string msg = ev.Get("Message")?.ToString() ?? ev.Get("Reason")?.ToString() ?? "";
            DetailsDisplay = string.IsNullOrEmpty(msg) ? $"{ev.Data.Count} parameter(s)" : msg;
        }
    }

    // Dialog for Force Enrollment
    public class ForceEnrollDialog : Window
    {
        public string SelectedStudentId => (CmbStudents.SelectedItem as Student)?.UserId;
        public string SelectedCourseId => (CmbCourses.SelectedItem as Course)?.CourseId;

        private ComboBox CmbStudents;
        private ComboBox CmbCourses;

        public ForceEnrollDialog(IEnumerable<Student> students, IEnumerable<Course> courses)
        {
            Title = "Force Enrollment Override (Admin)";
            Width = 420; Height = 220;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));

            var mainStack = new StackPanel { Margin = new Thickness(16) };

            mainStack.Children.Add(new TextBlock { Text = "Select Student:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            CmbStudents = new ComboBox { DisplayMemberPath = "FullName", Margin = new Thickness(0, 0, 0, 12), Height = 28, ItemsSource = students.ToList() };
            if (CmbStudents.Items.Count > 0) CmbStudents.SelectedIndex = 0;
            mainStack.Children.Add(CmbStudents);

            mainStack.Children.Add(new TextBlock { Text = "Select Course:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 4) });
            CmbCourses = new ComboBox { DisplayMemberPath = "CourseName", Margin = new Thickness(0, 0, 0, 16), Height = 28, ItemsSource = courses.ToList() };
            if (CmbCourses.Items.Count > 0) CmbCourses.SelectedIndex = 0;
            mainStack.Children.Add(CmbCourses);

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnOk = new Button { Content = "⚡ Force Enroll", Width = 110, Height = 30, Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A")), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            btnOk.Click += (s, e) => { DialogResult = true; Close(); };
            var btnCancel = new Button { Content = "Cancel", Width = 80, Height = 30, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")), Foreground = Brushes.White };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

            btnStack.Children.Add(btnOk);
            btnStack.Children.Add(btnCancel);
            mainStack.Children.Add(btnStack);

            Content = mainStack;
        }
    }

    // Dialog for Creating Student Account
    public class CreateStudentDialog : Window
    {
        public string UserId => TxtUserId.Text.Trim();
        public string FullName => TxtFullName.Text.Trim();
        public string Email => TxtEmail.Text.Trim();
        public string Phone => TxtPhone.Text.Trim();
        public string StudentNumber => TxtStudentNum.Text.Trim();
        public string ProgramId => TxtProgramId.Text.Trim();
        public int EnrolledYear => int.TryParse(TxtEnrolledYear.Text.Trim(), out var y) ? y : DateTime.UtcNow.Year;

        private TextBox TxtUserId, TxtFullName, TxtEmail, TxtPhone, TxtStudentNum, TxtProgramId, TxtEnrolledYear;

        public CreateStudentDialog()
        {
            Title = "Create New Student Account (Admin)";
            Width = 440; Height = 440;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));

            var mainStack = new StackPanel { Margin = new Thickness(16) };

            TxtUserId = AddField(mainStack, "User ID (e.g. STU005):");
            TxtFullName = AddField(mainStack, "Full Name:");
            TxtEmail = AddField(mainStack, "Email:");
            TxtPhone = AddField(mainStack, "Phone:");
            TxtStudentNum = AddField(mainStack, "Student Number:");
            TxtProgramId = AddField(mainStack, "Program ID (e.g. BS-CS):", "BS-CS");
            TxtEnrolledYear = AddField(mainStack, "Enrolled Year:", DateTime.UtcNow.Year.ToString());

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            var btnOk = new Button { Content = "➕ Create Student", Width = 130, Height = 30, Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB")), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(FullName))
                {
                    MessageBox.Show("User ID, Full Name, and Email are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                DialogResult = true; Close();
            };
            var btnCancel = new Button { Content = "Cancel", Width = 80, Height = 30, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")), Foreground = Brushes.White };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

            btnStack.Children.Add(btnOk);
            btnStack.Children.Add(btnCancel);
            mainStack.Children.Add(btnStack);

            Content = new ScrollViewer { Content = mainStack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private TextBox AddField(StackPanel parent, string label, string defaultValue = "")
        {
            parent.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 2, 0, 2), FontSize = 12 });
            var tb = new TextBox { Text = defaultValue, Height = 26, Margin = new Thickness(0, 0, 0, 6) };
            parent.Children.Add(tb);
            return tb;
        }
    }

    // Dialog for Creating Faculty Account
    public class CreateFacultyDialog : Window
    {
        public string UserId => TxtUserId.Text.Trim();
        public string FullName => TxtFullName.Text.Trim();
        public string Email => TxtEmail.Text.Trim();
        public string Phone => TxtPhone.Text.Trim();
        public string EmployeeNumber => TxtEmployeeNum.Text.Trim();
        public string Department => TxtDepartment.Text.Trim();
        public string Rank => TxtRank.Text.Trim();

        private TextBox TxtUserId, TxtFullName, TxtEmail, TxtPhone, TxtEmployeeNum, TxtDepartment, TxtRank;

        public CreateFacultyDialog()
        {
            Title = "Create New Faculty Account (Admin)";
            Width = 440; Height = 440;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));

            var mainStack = new StackPanel { Margin = new Thickness(16) };

            TxtUserId = AddField(mainStack, "User ID (e.g. FAC003):");
            TxtFullName = AddField(mainStack, "Full Name:");
            TxtEmail = AddField(mainStack, "Email:");
            TxtPhone = AddField(mainStack, "Phone:");
            TxtEmployeeNum = AddField(mainStack, "Employee Number:");
            TxtDepartment = AddField(mainStack, "Department:", "Computer Science");
            TxtRank = AddField(mainStack, "Rank (e.g. Senior Lecturer):", "Lecturer");

            var btnStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
            var btnOk = new Button { Content = "➕ Create Faculty", Width = 130, Height = 30, Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706")), Foreground = Brushes.White, FontWeight = FontWeights.Bold };
            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(FullName))
                {
                    MessageBox.Show("User ID, Full Name, and Email are required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                DialogResult = true; Close();
            };
            var btnCancel = new Button { Content = "Cancel", Width = 80, Height = 30, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")), Foreground = Brushes.White };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

            btnStack.Children.Add(btnOk);
            btnStack.Children.Add(btnCancel);
            mainStack.Children.Add(btnStack);

            Content = new ScrollViewer { Content = mainStack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        }

        private TextBox AddField(StackPanel parent, string label, string defaultValue = "")
        {
            parent.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 2, 0, 2), FontSize = 12 });
            var tb = new TextBox { Text = defaultValue, Height = 26, Margin = new Thickness(0, 0, 0, 6) };
            parent.Children.Add(tb);
            return tb;
        }
    }
}
