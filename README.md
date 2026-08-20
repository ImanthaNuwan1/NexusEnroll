# 🎓 NexusEnroll - University Enrollment Management System

NexusEnroll is a comprehensive **University Course Enrollment and Academic Management System** built with **C# and .NET 9**. The application features both a modern **WPF Graphical User Interface (GUI)** desktop client and an event-driven architecture powered by software design patterns (**Facade**, **Factory Method**, **Observer**, and **Saga Transaction Compensation**).

---

## 🌟 Key Features & User Actors

NexusEnroll caters to all three key university actors:

### 👨‍🎓 1. Student Portal
- **Browse & Search Catalog**: Real-time filtering by department, course keyword, or instructor name.
- **Enrollment Validation**: Enforces active account checks, prerequisite verification, schedule time conflict detection, and seat capacity validation.
- **Saga Transaction Rollback**: Simulates distributed transaction saga compensation if downstream student record updates fail.
- **Waitlist & Auto-Promotion**: Automatic queue management; when an enrolled student drops a course, the top waitlisted student is automatically promoted and enrolled.
- **Academic Progress Tracking**: View enrolled class schedule, past completed course history, and dynamic **Degree Audits** (remaining required courses for degree programs).

### 👩‍🏫 2. Faculty Portal
- **Teaching Schedule**: View assigned courses and instructor schedule.
- **Class Roster Inspection**: View real-time enrolled student rosters for any assigned course.
- **Batch Grade Entry**: Fault-tolerant batch grade submission (`A`, `B`, `C`, `D`, `F`, `W`, `I`) with itemized validation error reporting and submission status tracking (`Pending`, `Submitted`).
- **Course Change Requests**: Submit official requests to modify course capacity, description, or course title for administrative approval.

### 🛠️ 3. Administrator Portal
- **Enrollment & Utilization Analytics**: Generate department-wide enrollment reports highlighting high-capacity utilization courses exceeding threshold percentages (e.g., $\ge 90\%$).
- **Course Change Approval Workflow**: Review, approve, or reject pending course update requests submitted by faculty.
- **User Account Lifecycle Management**: Activate or deactivate user accounts dynamically.
- **Force Enrollment Override**: Administrative privilege to bypass course capacity limits and force-enroll students.

### 🔔 4. Real-Time Notification Stream (Observer Pattern)
- Event-driven notifications dispatched to multiple observer channels: **Console Observer**, **Email Observer**, **SMS Observer**, and **WPF GUI Observer Stream**.

---

## 🏗️ Architecture & Design Patterns

- **Facade Pattern** (`UniversityFacade`): Unified entry point encapsulating sub-services (`StudentService`, `FacultyService`, `AdminService`, `NotificationService`).
- **Factory Method Pattern** (`IUserFactory`, `UserFactoryManager`): Dynamic object creation for different user roles (`Student`, `Faculty`, `Admin`).
- **Observer Pattern** (`ISubject`, `INotificationObserver`, `NotificationService`): Decoupled event notification dispatcher.
- **Saga Pattern Simulation**: Transaction rollback handling for multi-step course seat reservation and student enrollment.

---

## 💻 Prerequisites & Environment Setup

### Prerequisites
- **Operating System**: Windows 10 / 11 (required for WPF desktop application).
- **SDK**: [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or higher.
- **IDE** (Optional but recommended): Visual Studio 2022 (v17.12+ with .NET Desktop Development workload) or Visual Studio Code with the C# Dev Kit extension.

---

## 🚀 Installation & Running the System

### 1. Clone or Download the Repository
Open PowerShell or Command Prompt and clone the repository:
```bash
git clone https://github.com/ImanthaNuwan1/NexusEnroll.git
cd NexusEnroll
```

### 2. Build the Project
Restore dependencies and compile the solution using the .NET CLI:
```bash
dotnet build
```

### 3. Launching the Application

#### Launching the WPF Graphical User Interface (GUI)
Run the application directly via the .NET CLI:
```bash
dotnet run
```
Or launch the pre-compiled desktop executable directly:
```powershell
.\bin\Debug\net9.0-windows\NexusEnroll.exe
```

> **Using the GUI**:
> 1. Use the **Active User** dropdown in the top header bar to switch between test actors (e.g. `STU001 - John Doe`, `FAC001 - Dr. Alan Turing`, `ADM001 - Alice Administrator`).
> 2. Navigate through the tabs: **Student Portal**, **Faculty Portal**, **Administrator Portal**, and **Notification History**.

---

## 📁 Project Structure

```text
NexusEnroll/
├── Models/
│   ├── User.cs            # Abstract User, Student, Faculty, Admin models & roles
│   └── Course.cs          # Course, CourseSchedule, CourseRecord, Enrollment, DegreeProgram
├── Patterns/
│   ├── Facade.cs          # UniversityFacade encapsulating all domain services
│   ├── Factory.cs         # IUserFactory implementations & UserFactoryManager
│   └── Observer.cs        # NotificationEvent, INotificationObserver, Email/SMS/Console Observers
├── Services/
│   ├── StudentService.cs  # Course browsing, enrollment validation, degree audit
│   ├── FacultyService.cs  # Class rosters, batch grading, change requests
│   ├── AdminService.cs    # Course updates, user management, utilization reports
│   └── NotificationService.cs # Event dispatcher & history tracking
├── MainWindow.xaml        # WPF GUI XAML view layout
├── MainWindow.xaml.cs     # WPF GUI code-behind & viewmodel bindings
├── Program.cs             # Application main entry point
└── NexusEnroll.csproj     # .NET 9 WPF Windows project configuration
```