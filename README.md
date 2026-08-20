# NexusEnroll

A university enrollment system: a C# domain layer (Students, Faculty, Admins, Courses,
enrollment/waitlist/grading rules) exposed to a PHP/HTML/CSS/JS web frontend over a small
HTTP API.

## Project layout

```
NexusEnroll/
├─ backend/     C# domain logic (Models, Services, Factory/Observer/Facade patterns)
│               + Program.cs, an interactive console demo of the same logic
├─ api/         ASP.NET Core minimal API - exposes UniversityFacade over HTTP for the
│               web frontend. Wraps backend/ as a library; doesn't modify it.
└─ frontend/    PHP pages + HTML/CSS/JS, calling the API via frontend/includes/config.php
```

## Prerequisites

- **[.NET 9 SDK](https://dotnet.microsoft.com/download)** — for the API and console demo
- **PHP 8+ with the `curl` extension enabled** — for the frontend. Any of these work:
  - PHP installed directly (`winget install PHP.PHP` on Windows, or your OS's package manager)
  - XAMPP / MAMP / WAMP
  - Your own Apache+PHP Docker setup

## Setup

```
git clone <this repo's URL>
cd NexusEnroll
```

### 1. Start the backend API

```
cd api
dotnet run
```

Leave this running. It listens on `http://localhost:5000` and seeds itself with demo
data (students, courses, faculty) on startup — everything resets when you restart it,
since there's no database, just in-memory state.

### 2. Start the frontend

In a second terminal, from the repo root, pick whichever you have available:

```
php -S localhost:8000 -t frontend
```

or point your Apache/XAMPP/Docker setup's document root at `frontend/`. If PHP is
running somewhere other than directly alongside the API (e.g. inside a container),
set the `API_BASE_URL` environment variable for it, otherwise it defaults to
`http://localhost:5000/api`.

### 3. Open it

Visit `http://localhost:8000/login.php` and sign in. The domain model has no password
storage, so **any password works for a known seeded email** — try:

| Email | Role |
|---|---|
| `john.doe@nexus.edu` | Student |
| `jane.smith@nexus.edu` | Student |
| `turing@nexus.edu` | Faculty |
| `alice.admin@nexus.edu` | Admin |

(Full list in `api/DemoData.cs`.) You can also register a new account from the login
page. If the API isn't running, pages fall back to hardcoded placeholder data instead
of failing outright.

## Alternative: console demo

The original interactive console app (same domain logic, no web layer) still runs
standalone:

```
dotnet run --project NexusEnroll.csproj
```

## Troubleshooting

- **Frontend only ever shows placeholder-looking data** → the API isn't running or
  isn't reachable; check the `api` terminal for errors and confirm `API_BASE_URL` in
  `frontend/includes/config.php` points at it.
- **Port already in use** → change the port in `api/Program.cs`'s `UseUrls(...)` (and
  update `API_BASE_URL` to match), or the `php -S` port.
- **PHP can't reach the API** → confirm the `curl` extension is enabled (`php -m`).
