# Proposly

Business management platform for small and medium agencies. Covers the full client lifecycle — from writing offers to running projects and tracking profitability — in a single tool.

The core pitch: most agencies use a proposals tool (PandaDoc, Bonsai) **and** a project management tool (Asana, Monday) separately. Proposly combines both — an accepted offer becomes a project automatically, and the quoted budget flows into profitability tracking.

---

## Features

| Area | What it covers |
|---|---|
| **Offers** | Create, send, accept/reject with line items, discounts, VAT, PDF export, email delivery |
| **Projects** | Kanban board, Gantt chart, burndown chart, task dependencies, time logging, expenses, milestones, notes |
| **Clients** | Profiles with status (Active / Lead / Inactive), notes, offer history and revenue stats |
| **Calendar** | Schedule meetings, invite team members, accept / decline / propose reschedule, pending invitations inbox |
| **Reports** | Quarterly P&L — offer funnel, labor costs, expenses by category, gross profit, PDF export |
| **Team** | Email invites, role-based access (Owner / Admin / Member), disable/enable accounts |
| **Notifications** | In-app notifications for task assignments, meeting invitations, and meeting lifecycle events |

---

## Tech stack

| Layer | Technology |
|---|---|
| API | .NET 10, ASP.NET Core, C# |
| Database | PostgreSQL via Entity Framework Core + Npgsql |
| Auth | JWT Bearer tokens |
| PDF | QuestPDF |
| Email | MailKit |
| Validation | FluentValidation |
| Frontend | React 19, TypeScript, Vite |
| UI | Tailwind CSS, shadcn/ui, Recharts, Tiptap |
| State | TanStack Query |

---

## Architecture

Five-layer clean architecture with CQRS (no MediatR):

```
src/
├── Proposly.Shared/        # Value objects, base classes — no dependencies
├── Proposly.Domain/        # Entities, aggregates, repository interfaces
├── Proposly.Application/   # Commands, queries, handlers, service interfaces
├── Proposly.Infrastructure/# EF Core, repositories, PDF/email services
└── Proposly.API/           # Controllers, middleware, DI wiring

frontend/                   # React + TypeScript SPA
tests/
├── Proposly.Domain.Tests/
└── Proposly.Application.Tests/
```

**Dependency flow:** `API → Application → Domain ← Infrastructure`

**Multi-tenancy:** Every entity implements `ITenantEntity` (`CompanyId`). EF Core global query filters enforce tenant isolation automatically on every query — no per-query `.Where(x => x.CompanyId == ...)` required.

**CQRS:** Custom `ICommandHandler<T>` / `IQueryHandler<T, R>` interfaces auto-registered by reflection. No pipeline library.

**Domain events:** Raised inside aggregates, dispatched in `AppDbContext.SaveChangesAsync`, handled by `IDomainEventHandler<T>` implementations (auto-registered). Used to create in-app notifications for task and calendar events.

**Snapshot pattern:** Financial figures (offer amounts, hourly rates) are stored at the moment of use. Changing a rate later does not alter historical records.

---

## Getting started

### Prerequisites

- .NET 10 SDK
- Node.js 20+
- PostgreSQL

### Run the API

```bash
# Configure connection string in appsettings.Development.json
# "ConnectionStrings__DefaultConnection": "Host=localhost;Database=proposly;Username=...;Password=..."

dotnet run --project src/Proposly.API/Proposly.API.csproj
# API available at http://localhost:5143
```

### Apply migrations

```bash
dotnet ef database update --project src/Proposly.Infrastructure --startup-project src/Proposly.API
```

### Run the frontend

```bash
cd frontend
npm install
npm run dev
# Runs on http://localhost:5173 — proxies /api to http://localhost:5143
```

---

## Commands

```bash
# Build
dotnet build

# Test
dotnet test

# Add a migration
dotnet ef migrations add <Name> --project src/Proposly.Infrastructure --startup-project src/Proposly.API

# Apply migrations
dotnet ef database update --project src/Proposly.Infrastructure --startup-project src/Proposly.API
```

---

## Domain overview

### OfferManagement
- `Offer` aggregate — lifecycle: Draft → Sent → Accepted / Rejected / Expired
- `Client` entity — status, notes, linked offers
- `OfferExpiryJob` background service — auto-expires sent offers past their validity date

### ProjectManagement
- `Project` aggregate — linked to a client via FK (name snapshot prevents drift)
- `ProjectTask` — Todo → InProgress → Done, estimated + actual hours, task dependencies with cycle detection
- `TimeEntry`, `Expense`, `Milestone`, `ProjectNote`, `TaskComment` sub-entities

### CalendarManagement
- `Termin` aggregate — meeting with start/end, location, organizer
- `TerminInvitation` — per-invitee status: Pending / Accepted / Declined / RescheduleProposed
- Full lifecycle: create → invite → respond → reschedule → cancel / delete

### CompanyManagement
- `Company` — multi-tenant root, stores fiscal year start month for quarterly reports
- `User` — roles, email invite flow, password reset tokens

---

## License

MIT
