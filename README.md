# 🎓 NexusEnroll - University Enrollment Management System

NexusEnroll is an enterprise-grade **University Course Enrollment and Academic Management System** built with **C# and .NET 9**. The platform features an event-driven **Microservices Architecture** with a modern **WPF Graphical User Interface (GUI)** desktop client.

---

## 🏗️ 1. Architecture Overview

NexusEnroll is engineered using a **Distributed Microservices Architecture**, decoupling application capabilities into independently deployable, domain-driven services that communicate synchronously via **HTTP REST** and asynchronously via **RabbitMQ Event Bus**.

```text
                               +-----------------------------+
                               |     NexusEnroll.Client      |
                               |    (WPF Desktop Client)     |
                               +-----------------------------+
                                              |
                                              v  HTTP REST
                               +-----------------------------+
                               |         Gateway.Api         |
                               |    (API Gateway - 5000)     |
                               +-----------------------------+
                                  /           |           \
                    HTTP REST    /            |            \    HTTP REST
                                v             v             v
             +--------------------+  +------------------+  +------------------+
             | StudentService.Api |  | FacultyService.  |  | AdminService.Api |
             |     (Port 5010)    |  |    (Port 5020)    |  |    (Port 5030)    |
             +--------------------+  +------------------+  +------------------+
                       |                      |                      |
                       v                      v                      v
                [ StudentDb.db ]       [ FacultyDb.db ]       [ AdminDb.db ]
                       |                      |                      |
                       +----------------------+----------------------+
                                              |
                                              v  Asynchronous Events
                               +-----------------------------+
                               |      RabbitMQ Event Bus     |
                               |   (AMQP Topic Exchange)     |
                               +-----------------------------+
```

### 🧩 Microservices Breakdown

| Service | Port | Database | Primary Responsibilities |
| :--- | :--- | :--- | :--- |
| **`Gateway.Api`** | `5000` | *None* | Reverse proxy & API Gateway routing client requests to backend services. |
| **`StudentService.Api`** | `5010` | `StudentDb.db` | Catalog search, enrollment rules validation, waitlists, degree audits. |
| **`FacultyService.Api`** | `5020` | `FacultyDb.db` | Teaching schedules, class rosters, batch grade entry, course update requests. |
| **`AdminService.Api`** | `5030` | `AdminDb.db` | User account lifecycle (CRUD), course creation, approval workflows, analytics. |
| **`NexusEnroll.Shared`** | *N/A* | *N/A* | Shared domain DTOs, event models (`StudentEnrolledEvent`, etc.), contracts. |
| **`NexusEnroll.Client`** | *GUI* | *Local Cache* | WPF Desktop client utilizing Facade & Factory patterns to interact with Gateway. |

### 🛠️ Key Microservice Architectural Features

1. **Database-per-Service Pattern**:
   - Each microservice maintains complete data sovereignty over its own dedicated SQLite database (`StudentDb.db`, `FacultyDb.db`, `AdminDb.db`).
   - Direct database access across microservice boundaries is strictly prohibited, preventing tight schema coupling.

2. **Event-Driven Architecture (AMQP / RabbitMQ)**:
   - Microservices publish domain events (`student.enrolled`, `student.dropped`, `faculty.changerequested`, `waitlist.joined`) to a RabbitMQ Topic Exchange.
   - Consumer services asynchronously ingest events to update local read models and trigger notifications (Email, SMS, Console).

3. **API Gateway Pattern**:
   - Single point of entry (`http://localhost:5000`) for the WPF Desktop Client.
   - Encapsulates internal service endpoints, request routing, and unified response mapping.

4. **Software Design Patterns**:
   - **Facade Pattern** (`Facade.cs`): Simplifies microservice HTTP communications and cache management for the WPF UI.
   - **Factory Method Pattern** (`UserFactoryManager`): Encapsulates object instantiation for Student, Faculty, and Admin user accounts.
   - **Observer Pattern**: Broadcasts real-time system notifications to multiple observer sinks (Console, Email, SMS, WPF UI).

---

## 🌟 Key Features & User Portals

### 👨‍🎓 Student Portal
- **Catalog Search**: Real-time course search by department, course code, or instructor name.
- **Enrollment Engine**: Prerequisites verification, seat capacity checking, and schedule conflict detection.
- **Waitlist Auto-Promotion**: Dropping a course automatically promotes and enrolls the next waitlisted student.
- **Degree Audit**: View completed course records and calculate remaining degree requirements.

### 👩‍🏫 Faculty Portal
- **Teaching Schedule**: View assigned teaching courses and class locations.
- **Roster Management**: Inspect real-time enrolled student rosters.
- **Batch Grade Submission**: Submit course grades with itemized error reporting.
- **Course Change Requests**: Submit capacity/title update requests for administrative approval.

### 🛠️ Administrator Portal
- **Account Lifecycle (CRUD)**: Create and delete Student and Faculty accounts with automated cascade cleanup.
- **Approval Workflows**: Review, approve, or reject course change requests submitted by faculty.
- **Capacity Analytics**: Generate department utilization reports highlighting high-capacity courses ($\ge 90\%$).
- **Override Capabilities**: Administrative force-enrollment override.

---

## 🚀 2. Installation & Setup Steps

Follow these steps to set up and run the NexusEnroll microservices platform on your local machine.

### 📋 Prerequisites

Ensure the following tools are installed on your system:
- **[.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** (or .NET 8.0/10.0 runtime)
- **Windows 10 / 11** (Required for the WPF desktop client)
- **[Docker Desktop](https://www.docker.com/products/docker-desktop/)** (Recommended for running RabbitMQ) *OR* a local **RabbitMQ Server**

---

### Step 1: Clone the Repository

Open Command Prompt, PowerShell, or Git Bash:

```bash
git clone https://github.com/ImanthaNuwan1/NexusEnroll.git
cd NexusEnroll
```

---

### Step 2: Start the RabbitMQ Message Broker

Use Docker Compose to launch a local RabbitMQ container with management dashboard enabled:

```bash
docker-compose up -d
```

> **RabbitMQ Ports**:
> - AMQP Broker: `localhost:5672`
> - Web Management Dashboard: `http://localhost:15672` (User: `guest`, Pass: `guest`)

*(Alternatively, if you have RabbitMQ installed natively on Windows, start the RabbitMQ service).*

---

### Step 3: Build the Solution

Restore dependencies and compile all microservices and the client application:

```bash
dotnet build
```

---

### Step 4: Run the Microservices Backend

Start each microservice API in a separate terminal window or running process:

#### Terminal 1: Student Service API (Port 5010)
```powershell
dotnet run --project Microservices/StudentService.Api/StudentService.Api.csproj
```

#### Terminal 2: Faculty Service API (Port 5020)
```powershell
dotnet run --project Microservices/FacultyService.Api/FacultyService.Api.csproj
```

#### Terminal 3: Admin Service API (Port 5030)
```powershell
dotnet run --project Microservices/AdminService.Api/AdminService.Api.csproj
```

#### Terminal 4: API Gateway (Port 5000)
```powershell
dotnet run --project Microservices/Gateway.Api/Gateway.Api.csproj
```

---

### Step 5: Launch the WPF Desktop Client

Once all 4 backend services are running, launch the desktop client:

```powershell
dotnet run --project Microservices/NexusEnroll.Client/NexusEnroll.Client.csproj
```

*Or launch the compiled executable directly from PowerShell/CMD:*
```powershell
.\Microservices\NexusEnroll.Client\bin\Debug\net9.0-windows\NexusEnroll.exe
```

---

## 🧪 Verification & Usage Testing

1. **Log in as Admin**: Select `[Admin] Alice Administrator (ADM001)` from the top user dropdown.
2. **Log in as Faculty**: Select `[Faculty] Dr. Alan Turing (FAC001)` to inspect teaching schedules, class rosters, and submit course update requests.
3. **Log in as Student**: Select `[Student] John Doe (STU001)` to browse catalog courses, enroll in classes, and view degree audits.

---

## 📁 Project Structure

```text
NexusEnroll/
├── docker-compose.yml                     # RabbitMQ message broker container definition
├── Microservices/
│   ├── Gateway.Api/                       # API Gateway (Reverse proxy on Port 5000)
│   ├── StudentService.Api/                # Student Microservice & StudentDb.db (Port 5010)
│   ├── FacultyService.Api/                # Faculty Microservice & FacultyDb.db (Port 5020)
│   ├── AdminService.Api/                  # Admin Microservice & AdminDb.db (Port 5030)
│   ├── NexusEnroll.Shared/                # Shared DTOs, Event Models, Contracts
│   └── NexusEnroll.Client/                # WPF Desktop GUI Application
└── README.md                              # Documentation
```