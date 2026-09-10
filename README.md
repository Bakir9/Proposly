# Proposly — Source Code

Multi-tenant SaaS platform for small and medium businesses to manage proposals, projects, and profitability.

**The core idea:** most businesses use a proposals tool and a project management tool separately. Proposly combines both — an accepted proposal becomes a project automatically, and the quoted budget flows into real-time profitability tracking.

---

## What's included

| Module | Features |
| --- | --- |
| **Proposals** | Itemized line items, VAT, discounts, PDF generation, email delivery, status lifecycle (Draft → Sent → Accepted / Rejected / Expired) |
| **Projects** | Kanban board, Gantt chart, tasks with dependencies, time logging, expenses, milestones, notes |
| **Profitability** | Labor cost (hours × rate) + expenses vs. quoted amount — with historical snapshots so past records never change |
| **Clients** | Profiles, notes, offer history |
| **Calendar** | Meeting scheduling, invitations, accept / decline / reschedule flow |
| **Reports** | Quarterly P&L breakdown — offer funnel, labor, expenses by category, PDF export |
| **Team** | Email invites, role-based access (Owner / Admin / Member), disable accounts |
| **Notifications** | In-app notifications for task assignments, meeting invitations, and calendar events |
| **Subscription Plans** | Free / Starter / Pro / Business tiers with user and project limits enforced at domain level |
| **SuperAdmin Panel** | Manage all tenants, update plans, view usage across all companies |

---

## Tech stack

| Layer | Technology |
| --- | --- |
| Backend | .NET 10, ASP.NET Core, Clean Architecture (5 layers) |
| Database | PostgreSQL + Entity Framework Core |
| Frontend | React 19 + TypeScript + Vite |
| UI | Tailwind CSS + shadcn/ui + Recharts + Tiptap |
| PDF | QuestPDF |
| Email | MailKit (any SMTP provider) |
| Auth | JWT Bearer tokens |
| Deployment | Docker + Render.com (`render.yaml` included) |

---

## Local setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org)
- PostgreSQL running locally

### 1. Configure the API

Create `src/Proposly.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=proposly;Username=YOUR_USER;Password=YOUR_PASSWORD"
  },
  "Jwt": {
    "Secret": "your-secret-key-minimum-32-characters-long"
  },
  "Smtp": {
    "Host": "your.smtp.host",
    "Port": 465,
    "UseSsl": true,
    "Username": "your@email.com",
    "Password": "your-smtp-password",
    "SenderEmail": "noreply@yourdomain.com",
    "SenderName": "Proposly"
  },
  "AppUrl": "http://localhost:5173",
  "Cors": {
    "AllowedOrigin": "http://localhost:5173"
  }
}
```

### 2. Run database migrations

```bash
dotnet ef database update --project src/Proposly.Infrastructure --startup-project src/Proposly.API
```

This creates the schema and seeds a SuperAdmin user and demo data automatically.

### 3. Start the API

```bash
dotnet run --project src/Proposly.API/Proposly.API.csproj
# Runs on http://localhost:5143
```

### 4. Start the frontend

```bash
cd frontend
npm install
npm run dev
# Runs on http://localhost:5173
```

Open [http://localhost:5173](http://localhost:5173) in your browser.

### Default credentials

```text
SuperAdmin
  Email:    admin@proposly.io
  Password: Admin@123!

Demo company user
  Email:    owner@demo.com
  Password: Owner@123!
```

> Change these passwords immediately after first login.

---

## Deploy to Render

A `render.yaml` is included — it defines a Docker web service for the API and a static site for the frontend.

### Steps

1. Push the repo to GitHub
1. Go to [render.com](https://render.com) → New → Blueprint → connect your repo
1. Set the following environment variables in the Render dashboard:

| Variable | Value |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Secret` | Random string, min 32 characters |
| `Smtp__Host` | Your SMTP server |
| `Smtp__Port` | `465` |
| `Smtp__UseSsl` | `true` |
| `Smtp__Username` | SMTP username |
| `Smtp__Password` | SMTP password |
| `Smtp__SenderEmail` | From address |
| `Smtp__SenderName` | `Proposly` (or your brand name) |
| `AppUrl` | Your frontend URL (e.g. `https://app.yourdomain.com`) |
| `Cors__AllowedOrigin` | Same as `AppUrl` |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

1. Set `VITE_API_BASE_URL` in the frontend static site settings to your API URL

Render will build and deploy automatically on every push to `main`.

---

## Customization

| What | Where |
| --- | --- |
| App name and branding | `frontend/src/` — layout components and Tailwind config |
| Email templates | `src/Proposly.Infrastructure/Services/Email/` |
| PDF templates | `src/Proposly.Infrastructure/Services/Pdf/` |
| Plan limits (users/projects per tier) | `src/Proposly.Domain/CompanyManagement/Entities/Company.cs` → `SetPlan()` |
| SMTP provider | Any provider works — Mailgun, SendGrid, Brevo, your own server |

---

## Useful commands

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Add a new migration
dotnet ef migrations add <MigrationName> --project src/Proposly.Infrastructure --startup-project src/Proposly.API

# Apply migrations
dotnet ef database update --project src/Proposly.Infrastructure --startup-project src/Proposly.API
```

---

## Architecture notes

- **Multi-tenancy** — every entity is scoped by `CompanyId`. EF Core global query filters enforce isolation automatically on every query.
- **Snapshot pattern** — hourly rates and offer amounts are stored at the moment of use. Changing a rate later never corrupts historical records.
- **CQRS** — custom `ICommandHandler` / `IQueryHandler` interfaces, no MediatR.
- **Domain events** — raised inside aggregates, dispatched on `SaveChangesAsync`, used to trigger in-app notifications.

---

## License

Single-site commercial use. You may deploy this code, sell access to it as a SaaS, and modify it freely. You may not resell the source code itself.
