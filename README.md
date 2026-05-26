# 🚀 DotNetHub

AI-powered .NET application hosting platform. Upload, build, deploy, and manage .NET applications with an intuitive web interface — designed for both humans and AI agents.

## Architecture

```
dotnethub/
├── backend/          # .NET 8 Web API
│   ├── Controllers/  # Auth, Projects, Admin
│   ├── Models/       # User, Project, Auth models
│   ├── Services/     # AuthService, ProjectService
│   └── Data/         # EF Core + SQLite
└── frontend/         # Vue 3 + TypeScript
    ├── src/views/    # Login, Dashboard, Projects, Admin
    ├── src/stores/   # Pinia state management
    └── src/api/      # Axios HTTP client
```

## Tech Stack

| Layer    | Technology        |
|----------|-------------------|
| Backend  | .NET 8 Web API    |
| Database | SQLite (EF Core)  |
| Auth     | JWT Bearer        |
| Frontend | Vue 3 + TypeScript |
| State    | Pinia             |
| Router   | Vue Router 4      |
| Build    | Vite              |

## Features

- 🔐 **User authentication** (register/login with JWT)
- 📦 **Project CRUD** — create, update, delete .NET projects
- 📤 **File upload** — upload project source as .zip or individual files
- 🔨 **Build & Deploy** — compile and run .NET apps with one click
- 🛑 **Process management** — start/stop deployed applications
- 👑 **Admin panel** — manage users, roles, and platform stats
- 🤖 **AI-friendly API** — designed for AI agents to automate hosting

## Quick Start

### Prerequisites

- .NET SDK 8.0+
- Node.js 18+

### Backend

```bash
cd backend
dotnet restore
dotnet run
# API runs at http://localhost:5100
```

### Frontend

```bash
cd frontend
npm install
npm run dev
# UI runs at http://localhost:5173
```

### Default Admin

- Username: `admin`
- Password: `admin123`

## API Endpoints

### Auth
| Method | Endpoint          | Description      |
|--------|-------------------|------------------|
| POST   | /api/auth/login   | Login            |
| POST   | /api/auth/register| Register         |

### Projects
| Method | Endpoint                   | Description       |
|--------|----------------------------|-------------------|
| GET    | /api/projects              | List projects     |
| POST   | /api/projects              | Create project    |
| PUT    | /api/projects/{id}         | Update project    |
| DELETE | /api/projects/{id}         | Delete project    |
| POST   | /api/projects/{id}/upload  | Upload files      |
| POST   | /api/projects/{id}/build   | Build project     |
| POST   | /api/projects/{id}/deploy  | Deploy & run      |
| POST   | /api/projects/{id}/stop    | Stop project      |

### Admin
| Method | Endpoint                       | Description       |
|--------|--------------------------------|-------------------|
| GET    | /api/admin/stats               | Platform stats    |
| GET    | /api/admin/users               | List users        |
| PUT    | /api/admin/users/{id}/toggle-active | Toggle user    |
| PUT    | /api/admin/users/{id}/role     | Change user role  |

## License

MIT
