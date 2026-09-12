# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

Governance rules for new work live in `.specify/memory/constitution.md`. Where the two overlap, the
constitution wins; this file is the day-to-day map of how the code is actually built.

## Project Overview

**Proposly** is a multi-tenant SaaS platform for small and medium businesses to manage offers,
projects, costs, and profitability. The differentiator is the pipeline: an accepted offer becomes a
project with the quoted amount snapshotted, and profitability is tracked against that snapshot.

It is a working, deployed product (Docker → Render), not a greenfield scaffold. Backend in `src/`,
React frontend in `frontend/`, tests in `tests/`, solution `Proposly.slnx`.

**Domain contexts** (6): `OfferManagement`, `ProjectManagement`, `CompanyManagement`,
`CalendarManagement`, `WorkTimeManagement`, `Notifications`.

**Application modules** (13): `Abstractions`, `Admin`, `Auth`, `CalendarManagement`, `Dashboard`,
`Notifications`, `OfferManagement`, `ProjectManagement`, `Reports`, `Search`, `Settings`,
`UserManagement`, `WorkTimeManagement`.

## Commands

```bash
# Run the API (https://localhost:xxxx; Scalar API docs at /scalar in Development)
dotnet run --project src/Proposly.API/Proposly.API.csproj

# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/Proposly.Domain.Tests

# Add an EF Core migration (output lands in src/Proposly.Infrastructure/Migrations/)
dotnet ef migrations add <Name> --project src/Proposly.Infrastructure --startup-project src/Proposly.API
```

```bash
# Frontend (from frontend/)
npm run dev      # Vite dev server on :5173
npm run build    # tsc -b && vite build
npm run lint     # eslint .
```

Migrations are applied automatically at startup by `db.Database.MigrateAsync()` in `Program.cs`,
in **all** environments — so never edit, squash, or delete a committed migration.

`AppDbContextFactory` supplies the design-time connection for `dotnet ef` and **hardcodes**
`Host=localhost;Port=5432;Database=proposly;Username=postgres;Password=1234`. Adjust it locally if
your Postgres differs; it is unused at runtime.

## Solution Structure

```
src/
├── Proposly.Shared/        # Primitives, value objects, marker interfaces — no dependencies
├── Proposly.Domain/        # Entities, enums, events, repository interfaces — references Shared
├── Proposly.Application/   # CQRS handlers, abstractions, DTOs — references Domain + Shared
├── Proposly.Infrastructure/# EF Core, repositories, external services — references Application + Domain
└── Proposly.API/           # Controllers, middleware, DI wiring — references Application + Infrastructure

tests/
├── Proposly.Domain.Tests/       # xUnit — references Domain
├── Proposly.Application.Tests/  # xUnit + NSubstitute — references Application
└── Proposly.Integration.Tests/  # xUnit — references API (currently a placeholder)

frontend/                   # React 19 + TypeScript + Vite
```

**Dependency flow:** `API → Application → Domain ← Infrastructure` — all reference `Shared`.
`Proposly.Domain` has **no** package references at all (no EF Core) — keep it that way.

## Layer Responsibilities

### Proposly.Shared

- `Primitives/` — `Entity.cs`, `AggregateRoot.cs`, `IDomainEvent.cs`, `IHasDomainEvents.cs`
- `ValueObjects/` — `Money.cs` (amount + currency, operator overloads, throws on currency
  mismatch), `Address.cs`
- `Interfaces/` — `ITenantEntity.cs` (`CompanyId`), `IAuditableEntity.cs` (`CreatedAt`/`UpdatedAt`)

### Proposly.Domain

One folder per bounded context, each with `Entities/`, `Enums/`, and where relevant `Events/`,
`Repositories/`, `Services/`:

```
OfferManagement/
  Entities/       → Offer (aggregate), OfferItem, Client, ClientNote
  Enums/          → OfferStatus, ClientStatus, VatType
  Events/         → OfferCreated/Sent/Accepted/RejectedDomainEvent
  Repositories/   → IOfferRepository, IClientRepository
  Services/       → VatCalculator, VatCalculationResult (pure domain service)
ProjectManagement/
  Entities/       → Project (aggregate), ProjectTask, Milestone, Expense, TimeEntry,
                    ProjectMember, ProjectNote, TaskComment
  Enums/          → ProjectStatus, ProjectTaskStatus, ExpenseCategory
  Events/         → ProjectCreated/ProjectStatusChanged/TaskAssignedDomainEvent
  Repositories/   → IProjectRepository
CompanyManagement/
  Entities/       → Company, User
  Enums/          → PlanTier, UserRole, CompanyStatus
  Repositories/   → ICompanyRepository, IUserRepository
CalendarManagement/
  Entities/       → Termin (aggregate), TerminInvitation
  Enums/          → TerminStatus, InvitationStatus
  Events/         → TerminScheduled/Rescheduled/Cancelled, InvitationResponded, RescheduleProposed
  Repositories/   → ITerminRepository
Notifications/    → Notification, INotificationRepository (flat — no subfolders)
```

Note: `OfferManagement/ValueObjects/` and `ProjectManagement/ValueObjects/` exist but are empty.

### Proposly.Application

```
Abstractions/     → ICommand, ICommand<T>, ICommandHandler<T>, ICommandHandler<T,R>,
                    IQuery<T>, IQueryHandler<T,R>            (custom CQRS, no MediatR)
                  → ValidatingCommandHandler<T> / <T,R>       (validation decorator)
                  → CommandValidationException                (→ HTTP 422)
                  → ICurrentUserService, ITenantContext, IAppSettings
                  → IEmailService, IPdfService, IJwtService, IPasswordHasher
                  → IDomainEventDispatcher, IDomainEventHandler<T>
<Module>/
  Commands/<Name>/  → <Name>Command.cs, <Name>CommandHandler.cs, <Name>CommandValidator.cs
  Queries/<Name>/   → <Name>Query.cs, <Name>QueryHandler.cs
  Responses/        → grouped DTO files, e.g. OfferResponses.cs, ClientResponses.cs
  EventHandlers/    → IDomainEventHandler<T> implementations (CalendarManagement)
  Events/           → IDomainEventHandler<T> implementations (Notifications)
  Services/         → module-specific service interfaces, e.g. Reports/Services/IReportPdfService.cs
```

Roughly 76 commands, 41 query handlers, 57 command validators, 14 domain event handlers.

Exceptions to the folder-per-operation rule: `Search/` is flat (`SearchQuery.cs`,
`SearchQueryHandler.cs`, `SearchResultItem.cs`). Response DTOs are **grouped** into
`<Thing>Responses.cs` files rather than one file per DTO.

`DependencyInjection.AddApplication()` registers everything by **assembly scanning** — command
handlers (wrapped in `ValidatingCommandHandler`), query handlers, domain event handlers, and all
FluentValidation validators. New handlers need no manual wiring; they only need to live in this
assembly and implement the interface.

### Proposly.Infrastructure

```
Persistence/
  AppDbContext.cs          → single DbContext; tenant filters, audit fields, event dispatch
  AppDbContextFactory.cs   → design-time only, hardcoded local connection string
  DataSeeder.cs            → EnsureSuperAdminAsync() (all envs) + SeedAsync() (Development only)
  Configurations/          → 26 IEntityTypeConfiguration<T> classes
  Repositories/            → 12 repositories + ProjectBookingReader (a read-only seam
                             WorkTimeManagement uses to read project time logs without
                             depending on IProjectRepository)
Migrations/                → EF Core migrations (top level, NOT under Persistence/)
Services/
  Auth/                    → JwtService, PasswordHasher
  Email/                   → MailKitEmailService
  Events/                  → DomainEventDispatcher
  Pdf/                     → OfferPdfService, QuarterlyReportPdfService (QuestPDF)
  AppSettings.cs
  OfferExpiryJob.cs        → BackgroundService, expires offers past ValidUntil
Resources/                 → logo.png (embedded in PDFs)
```

`Services/Storage/` exists but is empty — there is no file-storage implementation and no
`IFileStorage` abstraction yet.

`DependencyInjection.AddInfrastructure(configuration)` registers the `DbContext`, repositories,
and services **explicitly**. Preserve the existing lifetimes: repositories and per-request services
`Scoped`; stateless services (`IAppSettings`, `IPasswordHasher`, `IPdfService`,
`IReportPdfService`) `Singleton`.

### Proposly.API

```
Controllers/     → 17 controllers: Admin, Auth, Calendar, Clients, Dashboard, Diagnostics,
                   Notifications, Offers, Projects, Reports, Search, Settings, Users,
                   Timesheets, Absences, WorkTimeSettings, WorkTimeReports
Authorization/   → Policies.cs — the single registry of policy name constants
Extensions/      → JwtExtensions (AddJwtAuthentication), AuthorizationExtensions
                   (AddProposlyAuthorization)
Middleware/      → TenantMiddleware — resolves CompanyId from JWT claims; SuperAdmin bypasses
Services/        → TenantContext, CurrentUserService
Program.cs       → all DI wiring, the global exception handler, and the middleware pipeline
```

Both `AddApplication()` and `AddInfrastructure()` are fully implemented and wired in `Program.cs` —
there are no remaining `TODO` markers.

## Key Design Rules

**Multi-tenancy.** Every tenant-scoped entity implements `ITenantEntity` (`CompanyId`).
`AppDbContext.OnModelCreating` reflects over the model and applies
`HasQueryFilter(e => e.CompanyId == _currentUserService.CompanyId)` to each one automatically — a
new entity gets isolation for free, and one that skips the interface silently leaks across tenants.
Only cross-tenant SuperAdmin paths use `IgnoreQueryFilters()` (see
`CompanyRepository.GetAllWithCountsAsync` and `Admin/`), guarded by `Policies.SuperAdminOnly`.

**Per-employee visibility.** A second marker, `IUserOwnedEntity` (`UserId`), narrows rows to one
person *inside* a company. `OnModelCreating` dispatches: entities with only `ITenantEntity` get
the tenant filter unchanged, entities with both get a composed predicate:

```csharp
e.CompanyId == _currentUserService.CompanyId
&& (e.UserId == _currentUserService.UserId || _currentUserService.CanViewAllEmployees)
```

`CanViewAllEmployees` is true for Owner, Admin, and SuperAdmin, and is deliberately non-throwing —
the filter can be evaluated outside an HTTP request (seeding, background jobs), where it returns
false.

**Approver access is expressed inside the predicate, never via `IgnoreQueryFilters()`.** That
method drops *every* filter including the company one, so using it to reach a colleague's row
would silently allow cross-tenant reads. Nothing in `WorkTimeManagement` calls it. Company
reference data that everyone must read — `NonWorkingDay`, `WorkTimePolicy` — deliberately stays
tenant-only. `QueryFilterRegressionTests` asserts both halves of this.

**CQRS without MediatR.** Hand-rolled `ICommandHandler<T>` / `ICommandHandler<T,R>` /
`IQueryHandler<T,R>` in `Application/Abstractions/`, injected straight into controller actions via
`[FromServices]`. No pipeline library — do not introduce MediatR.

**Validation.** Every command with a user-supplied payload gets a FluentValidation
`AbstractValidator<TCommand>` in the **same folder** as the command.
`ValidatingCommandHandler` resolves it from DI and throws `CommandValidationException` → HTTP 422
with per-field errors. There is no compile-time link, so a missing validator silently disables
validation. New commands must ship one. (19 existing id-only commands — deletes, accepts, rejects,
status toggles — have none, since there is nothing beyond a `Guid` to validate.)

**Error handling.** The terminal `app.UseExceptionHandler` block in `Program.cs` is the *only*
place exceptions become HTTP responses:

| Thrown | Response |
|---|---|
| `CommandValidationException` | 422 + `errors` dictionary |
| `InvalidOperationException`, `ArgumentException` | 400 + `ex.Message` as title |
| anything else | 500 + generic title |

Handlers signal "not found" by throwing `InvalidOperationException`; queries return `null` and the
controller turns that into `NotFound()`. Do not add try/catch-to-status-code blocks in controllers
and do not add competing exception middleware.

**Auditing and domain events are central.** `AppDbContext.SaveChangesAsync` stamps
`CreatedAt`/`UpdatedAt` on `IAuditableEntity` and collects, clears, then dispatches domain events
via `IDomainEventDispatcher` *after* the save succeeds. Never set audit fields or dispatch events
by hand in a handler.

**Money.** `Money` (amount + currency) is used in **domain entities and EF configurations** —
`Offer.CalculateTotal()`, `OfferItem.UnitPrice`, `Project.Budget`/`OfferedAmount`,
`Expense.Amount`, `TimeEntry.HourlyRateSnapshot`. Wire contracts (commands, queries, response
DTOs) carry a raw `decimal` plus a `Currency` string, and the **handler** constructs the `Money` at
the boundary:

```csharp
offer.AddItem(command.Description, command.Quantity, new Money(command.UnitPrice, offer.Currency));
```

Follow that split. Genuinely non-money decimals — `Quantity`, `HoursWorked`, `VatRate`,
`DiscountPercent` — stay `decimal` everywhere.

**Snapshot pattern.** Financial figures are frozen at the moment of use: `Project.OfferedAmount`
from the accepted offer, `TimeEntry.HourlyRateSnapshot` from the member's rate at logging time,
`Project.ClientName` alongside the nullable `ClientId`. Reports must never recompute history from
live master data.

**Rich domain entities.** Business logic lives in entities: VAT and totals on `Offer`, status
transitions and item locking after send, `Project.CalculateLaborCost/TotalCost/Profitability`,
plan limits on `Company` (`IsUserLimitReached`, `IsProjectLimitReached`, with expired plans falling
back to Free-tier limits). Handlers orchestrate only: load aggregate → call a domain method → save.

**Performance gotcha.** `IProjectRepository` exposes a separate `GetByIdForWriteAsync` that
excludes `TimeEntries`; command handlers use it to avoid loading large collections on every save.

## Adding a Feature

The nearest existing module is the template — mirror it rather than inventing a shape. A typical
vertical slice touches:

1. `Domain/<Context>/Entities/` — entity or aggregate with the business rules; implement
   `ITenantEntity` (+ `IAuditableEntity`) if it is tenant data.
2. `Domain/<Context>/Repositories/` — repository interface (no EF Core types).
3. `Infrastructure/Persistence/Configurations/` — `IEntityTypeConfiguration<T>`.
4. `Infrastructure/Persistence/AppDbContext.cs` — add the `DbSet`.
5. `Infrastructure/Persistence/Repositories/` — implementation, registered in `AddInfrastructure`.
6. `dotnet ef migrations add <Name>` — commit the generated migration.
7. `Application/<Module>/Commands/<Name>/` — command + handler + validator (auto-registered).
8. `Application/<Module>/Queries/<Name>/` + `Responses/` — query + handler + DTO.
9. `API/Controllers/` — endpoint following the conventions below.
10. `tests/` — domain rules in `Domain.Tests/<Module>/`, handlers in
    `Application.Tests/<Module>/`, named `<TypeUnderTest>Tests.cs`, including negative tests for
    invariants and invalid status transitions.
11. `frontend/src/api/<feature>.ts` + `frontend/src/features/<feature>/` if user-facing.

### API controller conventions

`sealed` class, `[ApiController]`, `[Route("api/[controller]")]`, class-level
`[Authorize(Policy = Policies.X)]`, handlers injected per action via `[FromServices]`,
`CancellationToken ct` last:

```csharp
[HttpPost]
public async Task<ActionResult<Guid>> Create(
    [FromBody] CreateOfferCommand command,
    [FromServices] ICommandHandler<CreateOfferCommand, Guid> handler,
    CancellationToken ct)
{
    var id = await handler.HandleAsync(command, ct);
    return CreatedAtAction(nameof(GetById), new { id }, id);
}
```

Return shapes: `CreatedAtAction` for creates, `NoContent()` for state transitions, `NotFound()`
when a query returns `null`. Policy names always come from `Policies` constants — never inline
strings. Available policies: `OwnerOnly`, `ManageUsers`, `ManageOffers`, `ManageClients`,
`ManageProjects`, `SuperAdminOnly`, `RecordOwnWorkTime`, `ApproveWorkTime`,
`ManageWorkTimeSettings`, `ViewAllWorkTime`.

## Frontend

```
frontend/src/
  api/          → one typed module per controller (offers.ts, projects.ts, …) over client.ts,
                  a shared axios instance that attaches the JWT and redirects to /login on 401
  features/     → admin, auth, calendar, clients, dashboard, errors, notifications, profile,
                  projects, reports, settings, users
  components/   → AppShell, TopBar, NotificationBell + ui/ (10 shadcn-style primitives)
  context/      → ThemeContext.tsx
  contexts/     → NotificationTaskContext.tsx
  lib/          → api-errors.ts, utils.ts
```

Both `context/` and `contexts/` exist. This is a known inconsistency and is **frozen** — put new
files in whichever already holds the analogous one; do not merge or rename them.

Stack: TanStack Query for server state, react-hook-form + zod for forms, Tailwind + `components/ui`
for presentation, Recharts for charts, Tiptap for rich text, sonner for toasts,
react-router-dom v7 for routing. API base URL comes from `VITE_API_BASE_URL`.

## Tech Stack (actual)

| Concern | Library |
|---|---|
| Runtime | .NET 10 (`net10.0` across all projects), ASP.NET Core |
| ORM | EF Core 10 + `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.1 |
| Validation | FluentValidation 11.11.0 (+ DI extensions) |
| PDF | QuestPDF 2024.10.2 (Community license set in `AddInfrastructure`) |
| Email | MailKit 4.8.0 (`MailKitEmailService`, configurable SMTP host/port/SSL) |
| Auth | JWT Bearer 10.0.6 + `System.IdentityModel.Tokens.Jwt` 8.17.0 |
| API docs | `Microsoft.AspNetCore.OpenApi` + Scalar 2.14.0 (`/scalar`, Development only) |
| Testing | xUnit 2.9.3, NSubstitute 5.3.0 (Application.Tests only), coverlet |
| Frontend | React 19, TypeScript ~6.0, Vite 8, Tailwind 3.4, TanStack Query 5, axios, zod 4 |
| Deployment | Docker (`Dockerfile`) → Render (`render.yaml`), Frankfurt |

**Not installed:** Serilog (default console logging only), SendGrid (MailKit is the email path),
Stripe, and any error-tracking SDK.

## Configuration & Deployment

- `appsettings.json` holds empty placeholders. Real local values go in
  `appsettings.Development.json`, which is untracked — never commit secrets.
- Production config comes from Render env vars declared in `render.yaml` using `__` nesting:
  `ConnectionStrings__DefaultConnection`, `Jwt__Secret/Issuer/Audience`,
  `Smtp__Host/Port/UseSsl/Username/Password/SenderEmail/SenderName`, `AppUrl`,
  `Cors__AllowedOrigin`. Secrets are marked `sync: false`.
- CORS allows a single origin from `Cors:AllowedOrigin` (falls back to `http://localhost:5173`).
  No wildcards.
- Auth endpoints (login, register, forgot-password) sit behind a fixed-window rate limiter named
  `"auth"` — 5 requests/minute, rejecting with 429.
- `GET /health` is mapped for load balancers.
- The `Dockerfile` copies each `.csproj` **explicitly** before restore. A new backend project must
  be added there and to `Proposly.slnx`, or the container build breaks.

## Known Gaps

- `Proposly.Integration.Tests` holds only the query-filter regression tests; there is still no
  database-backed integration coverage. The composed query filter in particular cannot be proven
  correct by a unit test — only a real Postgres round trip shows whether the generated SQL keeps
  one employee's rows away from another. Testcontainers + Respawn would close this, and would need
  a constitution amendment for the dependency.
- No project-level access control: members with `ManageProjects` see all company projects, not only
  those where they appear in `ProjectMembers`. `IUserOwnedEntity` is the mechanism that would fix
  this, but it has deliberately not been retrofitted to `ProjectMember` — that is its own change.
- Holiday import for `WorkTimeManagement` is specified (FR-066–FR-072) but unbuilt. It would add
  the codebase's first outbound HTTP integration and needs a constitution amendment first.
- No structured logging, error tracking, or payment integration.
- `DiagnosticsController` exposes `POST /api/diagnostics/email-test?to=…` for SMTP smoke tests —
  a debug surface, not a product feature.
