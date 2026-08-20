# 🎓 NexusEnroll - University Enrollment Management System

NexusEnroll is a comprehensive **University Course Enrollment and Academic Management System** built with **C# and .NET 9**. The application features a modern **WPF Graphical User Interface (GUI)** desktop client and an event-driven domain architecture built on software design patterns (**Facade**, **Factory Method**, **Observer**, and **Saga Transaction Compensation**).

---

## 🌟 Key Features & User Portals

NexusEnroll provides specialized workflows for all core university actors:

### 👨‍🎓 1. Student Portal
- **Browse & Search Catalog**: Real-time filtering by department, course keyword, or instructor name.
- **Enrollment Validation**: Enforces active account checks, prerequisite verification, schedule time conflict detection, and seat capacity validation.
- **Saga Transaction Rollback**: Simulates transaction saga compensation if downstream student record updates fail (releasing reserved seats automatically).
- **Waitlist & Auto-Promotion**: Automatic queue management; when an enrolled student drops a course, the top waitlisted student is automatically promoted and enrolled.
- **Academic Progress Tracking**: View enrolled class schedule, past completed course history, and dynamic **Degree Audits** (remaining required courses for degree programs).

### 👩‍🏫 2. Faculty Portal
- **Teaching Schedule**: View assigned courses and instructor schedule.
- **Class Roster Inspection**: View real-time enrolled student rosters for any assigned course.
- **Batch Grade Entry**: Fault-tolerant batch grade submission (`A`, `B`, `C`, `D`, `F`, `W`, `I`) with itemized validation error reporting and submission status tracking (`Pending`, `Submitted`).
- **Course Change Requests**: Submit official requests to modify course capacity, description, or course title for administrative review.

### 🛠️ 3. Administrator Portal
- **Student Account Creation**: Dynamically manufacture new student user accounts via `UserFactoryManager` (**Factory Method Pattern**).
- **Faculty Account Creation**: Dynamically manufacture new faculty user accounts via `UserFactoryManager` (**Factory Method Pattern**) with multi-course teaching assignments.
- **Student Account Deletion**: Delete student accounts with automatic cascade cleanup (releasing enrolled course seats and removing waitlist entries).
- **Faculty Account Deletion**: Delete faculty accounts with automatic cascade unassignment from teaching schedules.
- **Enrollment & Utilization Analytics**: Generate department-wide enrollment reports highlighting high-capacity utilization courses exceeding threshold percentages (e.g., $\ge 90\%$).
- **Course Change Approval Workflow**: Review, approve, or reject pending course update requests submitted by faculty.
- **User Account Activation & Deactivation**: Enable or disable user accounts dynamically.
- **Force Enrollment Override**: Administrative privilege to bypass course capacity limits and force-enroll students.

### 🔔 4. Real-Time Notification Stream (Observer Pattern)
- Event-driven notifications dispatched to multiple observer channels: **Console Observer**, **Email Observer**, **SMS Observer**, and **WPF GUI Observer Stream**.

---

## 🏗️ Architecture & Design Patterns

```text
               +----------------------------------+
               |        UniversityFacade          |  <-- Unified Entry Point
               +----------------------------------+
                 /        |           |         \
                v         v           v          v
   +------------------+ +-----------+ +--------+ +---------------------+
   | UserFactoryManager| |StudentServ| |AdminServ| | FacultyService      |
   +------------------+ +-----------+ +--------+ +---------------------+
   (Factory Method)                              | NotificationService |
                                                 +---------------------+
                                                   (Observer Pattern)
```

- **Facade Pattern** (`UniversityFacade`): Encapsulates sub-services (`StudentService`, `FacultyService`, `AdminService`, `NotificationService`) behind a simplified API.
- **Factory Method Pattern** (`IUserFactory`, `UserFactoryManager`): Encapsulates object creation logic for `Student`, `Faculty`, and `Admin` users using flexible attribute dictionaries.
- **Observer Pattern** (`ISubject`, `INotificationObserver`, `NotificationService`): Dispatches real-time event notifications (`StudentEnrolledEvent`, `StudentAccountCreated`, `FacultyAccountDeleted`, etc.) to subscribers.
- **Saga Pattern Simulation**: Transaction rollback handling for multi-step course seat reservation and student enrollment.

---

## 💻 Prerequisites & Environment Setup

### Prerequisites
- **Operating System**: Windows 10 / 11 (required for WPF desktop client).
- **SDK**: [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or higher.
- **IDE** (Optional but recommended): Visual Studio 2022 (v17.12+ with .NET Desktop Development workload) or Visual Studio Code with C# Dev Kit.

---

## 🚀 Installation & Running the System

### 1. Clone the Repository
Open PowerShell or Command Prompt:
```bash
git clone https://github.com/ImanthaNuwan1/NexusEnroll.git
cd NexusEnroll
```

### 2. Build the Project
Compile the solution using the .NET CLI:
```bash
dotnet build
```

### 3. Launching the Application

#### Launching the WPF Graphical User Interface (GUI)
Run the application via `dotnet run`:
```bash
dotnet run
```
Or execute the desktop application binary directly:
```powershell
.\bin\Debug\net9.0-windows\NexusEnroll.exe
```

---

## 📖 Admin User Guide: Account Management

### Creating Student & Faculty Accounts
1. Launch the GUI and select an Administrator user from the top header dropdown (e.g. `[Admin] Alice Administrator (ADM001)`).
2. Go to the **🛠️ Administrator Portal** tab.
3. Under **User Account Lifecycle Management**, click:
   - `➕ Create Student`: Fill in User ID (`STU005`), Full Name, Email, Phone, Student #, Program ID (`BS-CS`), and Enrolled Year.
   - `➕ Create Faculty`: Fill in User ID (`FAC003`), Full Name, Email, Phone, Employee #, Department, and Rank.
4. Click **Create**. The user will immediately be added to the system, registered with domain services, and made available in the Active User dropdown selector.

### Deleting Accounts & Cascade Cleanup
1. Select a Student or Faculty user in the **User Account Lifecycle Management** grid.
2. Click `🗑️ Delete Account`.
3. Confirm deletion in the pop-up window:
   - Deleting a **Student** releases their reserved seats in enrolled courses (calling `course.Drop()`) and removes them from waitlists.
   - Deleting a **Faculty** member unassigns them from teaching schedules (`course.InstructorId = null`).
4. Event notifications (`StudentAccountDeleted` / `FacultyAccountDeleted`) will immediately stream to the **🔔 Notification History** tab.

---

## 💻 API Code Example (Programmatic Usage)

```csharp
using NexusEnroll.Patterns;
using NexusEnroll.Models;

// Initialize facade
var facade = new UniversityFacade();
facade.AttachObserver(new GuiNotificationObserver(mainWindow));

// Create Student Account via Admin Facade API (Factory Method)
Student student = facade.CreateStudentAccount(
    userId: "STU005",
    fullName: "David Miller",
    email: "david.m@nexus.edu",
    phone: "555-0305",
    studentNumber: "S1005",
    programId: "BS-CS",
    enrolledYear: 2026
);

// Create Faculty Account via Admin Facade API
Faculty faculty = facade.CreateFacultyAccount(
    userId: "FAC003",
    fullName: "Dr. Ada Lovelace",
    email: "lovelace@nexus.edu",
    phone: "555-0800",
    employeeNumber: "EMP-003",
    department: "Computer Science",
    rank: "Professor"
);

// Enroll Student in Course (Validation & Saga Rollback)
bool enrolled = facade.EnrollStudentInCourse("STU005", "CS101", out string message);

// Delete Account (Cascade Cleanups & Observer Events)
bool deleted = facade.DeleteStudentAccount("STU005");
```

---

## 📁 Project Structure

```text
NexusEnroll/
├── Models/
│   ├── User.cs            # User, Student, Faculty, Admin models & role definitions
│   └── Course.cs          # Course, CourseSchedule, CourseRecord, Enrollment, DegreeProgram
├── Patterns/
│   ├── Facade.cs          # UniversityFacade (unified domain entry point)
│   ├── Factory.cs         # IUserFactory, Student/Faculty/Admin Factories & UserFactoryManager
│   └── Observer.cs        # NotificationEvent, INotificationObserver, Email/SMS/Console Observers
├── Services/
│   ├── StudentService.cs  # Catalog browsing, enrollment validation, degree audit
│   ├── FacultyService.cs  # Class rosters, batch grading, change requests
│   ├── AdminService.cs    # Account creation/deletion, course updates, utilization reports
│   └── NotificationService.cs # Event dispatcher & history tracking
├── MainWindow.xaml        # WPF Desktop Client XAML View layout
├── MainWindow.xaml.cs     # WPF Desktop Client Code-behind & ViewModels
├── Program.cs             # Application Main entry point [STAThread]
└── NexusEnroll.csproj     # .NET 9 WPF Windows project configuration
```