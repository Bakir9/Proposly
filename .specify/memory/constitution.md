<!--
SYNC IMPACT REPORT
==================
Version change: none (template placeholders) → 1.0.0
Rationale: Initial ratification. The prior file was the unfilled scaffold, so this is the
first concrete constitution rather than an amendment.

Principles defined (all new):
  - I. Additive Modules Only (NON-NEGOTIABLE)
  - II. Existing Conventions Are the Spec
  - III. Reuse Platform Seams, Never Fork Them
  - IV. Tenant Isolation and Financial Integrity (NON-NEGOTIABLE)
  - V. Validated Commands, Rich Domain, Tested Behavior

Sections added:
  - Technology and Structural Constraints (template slot SECTION_2)
  - Development Workflow and Quality Gates (template slot SECTION_3)
  - Governance

Sections removed: none (no prior content existed)

Placeholders resolved: PROJECT_NAME, PRINCIPLE_1..5_NAME, PRINCIPLE_1..5_DESCRIPTION,
SECTION_2_NAME, SECTION_2_CONTENT, SECTION_3_NAME, SECTION_3_CONTENT, GOVERNANCE_RULES,
GUIDANCE_FILE, CONSTITUTION_VERSION, RATIFICATION_DATE, LAST_AMENDED_DATE

Follow-up TODOs: none

AMENDMENT 1.0.0 → 1.0.1 (2026-09-08, PATCH — factual correction after a full code audit):
  - Principle IV: the Money rule said raw `decimal` in any command/query/DTO was a defect. The
    codebase deliberately carries `decimal` + `Currency` on the wire and constructs `Money` in the
    handler, so the old wording branded all 59 existing commands as defects. Reworded to require
    `Money` for domain/EF state only, and to exclude non-money decimals.
  - Principle V: scoped the validator requirement to new commands and grandfathered the 19
    existing id-only commands that have no validator, so Principle I is not violated to satisfy it.
  - Review gates: reworded the money trigger to match Principle IV.
-->

# Proposly Constitution

## Core Principles

### I. Additive Modules Only (NON-NEGOTIABLE)

Proposly is a running, deployed product. Every new capability MUST arrive as a new module that
sits alongside existing code, never as a reshaping of it.

- New work MUST be added as new files in new module folders (a new bounded-context or feature
  namespace under `Proposly.Domain/`, `Proposly.Application/`, `Proposly.Infrastructure/`, a new
  controller under `Proposly.API/Controllers/`, a new folder under `frontend/src/features/`).
- Existing code MUST NOT be restructured: no renaming or relocating existing projects, folders,
  namespaces, types, endpoints, or database tables; no re-layering; no "while I was in here"
  reformatting; no consolidating existing duplication.
- Existing files MAY be touched only for the minimum edits a new module requires — typically a
  DI registration, a `DbSet` plus its configuration, a route registration, or a navigation entry.
- Known inconsistencies in the existing tree (for example both `frontend/src/context/` and
  `frontend/src/contexts/`) are frozen. New code MUST join whichever one already holds the
  analogous file and MUST NOT merge, rename, or deprecate either.
- Removing or renaming a public API endpoint, DB column, or shared contract is a breaking change
  that requires an explicit amendment under Governance before any code is written.

Rationale: the codebase is in production on Render with live tenant data and migration history.
Restructuring risks silent data and contract breakage that no feature benefit justifies.

### II. Existing Conventions Are the Spec

When adding a module, the nearest existing module of the same kind is the authoritative template.
Conventions are copied, not reinvented.

- **Layout**: five backend projects (`Proposly.Shared`, `.Domain`, `.Application`,
  `.Infrastructure`, `.API`) registered in `Proposly.slnx`; tests in `tests/`; frontend in
  `frontend/`. New projects MUST be justified and MUST be added to `Proposly.slnx` and, if the
  API depends on them, to the `Dockerfile` restore layer.
- **Naming**: one folder per operation under `<Module>/Commands/<Name>/` or
  `<Module>/Queries/<Name>/`, containing `<Name>Command.cs` / `<Name>Query.cs`,
  `<Name>CommandHandler.cs` / `<Name>QueryHandler.cs`, and for commands
  `<Name>CommandValidator.cs`. Response DTOs live in `<Module>/Responses/`.
- **CQRS**: the hand-rolled `ICommand`, `ICommandHandler<T>`, `ICommandHandler<T,R>`, `IQuery`,
  `IQueryHandler<T,R>` in `Proposly.Application/Abstractions/`. MediatR or any other CQRS
  pipeline library MUST NOT be introduced.
- **DI registration**: handlers, queries, validators, and domain event handlers are discovered by
  assembly scanning in `Proposly.Application.DependencyInjection.AddApplication`, so they need no
  manual wiring — they only need to live in the Application assembly and implement the interface.
  Repositories and infrastructure services MUST be registered explicitly in
  `Proposly.Infrastructure.DependencyInjection.AddInfrastructure`, preserving existing lifetimes
  (repositories and per-request services `Scoped`; stateless services such as `IPdfService`,
  `IPasswordHasher`, `IAppSettings` `Singleton`).
- **API conventions**: `[ApiController]`, `[Route("api/[controller]")]`, `sealed` controller
  classes, a class-level `[Authorize(Policy = Policies.X)]`, handlers injected per-action via
  `[FromServices]`, `CancellationToken ct` as the last parameter, and the established return
  shapes (`CreatedAtAction` for creates, `NoContent()` for state transitions, `NotFound()` for a
  null query result). Policy names MUST come from `Policies` constants — never inline strings.
- **Frontend conventions**: pages under `frontend/src/features/<feature>/`, one typed API client
  module per backend controller in `frontend/src/api/<feature>.ts` built on the shared axios
  client, TanStack Query for server state, react-hook-form + zod for forms, Tailwind and
  `components/ui` primitives for presentation.

Rationale: a single consistent shape keeps the code navigable and makes assembly-scanned
registration and generated API docs work without per-feature special cases.

### III. Reuse Platform Seams, Never Fork Them

Cross-cutting concerns already have exactly one implementation. New modules consume it.

- **Auth**: JWT bearer via `AddJwtAuthentication`, roles and policies via
  `AddProposlyAuthorization` and `Policies`, current identity via `ICurrentUserService`, tenant
  resolution via `TenantMiddleware` and `ITenantContext`. New modules MUST NOT add an
  authentication scheme, token format, or bespoke permission check.
- **Persistence**: the single `AppDbContext`. New entities MUST be added as a `DbSet` with an
  `IEntityTypeConfiguration<T>` under `Persistence/Configurations/` and reached through a
  repository interface declared in `Proposly.Domain/<Module>/Repositories/` and implemented in
  `Persistence/Repositories/`. A second `DbContext`, a second connection string, or raw ADO.NET
  access MUST NOT be introduced.
- **Error handling**: the terminal `UseExceptionHandler` block in `Program.cs` is the only place
  that maps exceptions to HTTP responses. Handlers signal failure by throwing
  `CommandValidationException` (→ 422), `InvalidOperationException` / `ArgumentException`
  (→ 400), or by returning `null` from a query for the controller to turn into `NotFound()`.
  Controllers MUST NOT contain try/catch-to-status-code blocks, and new global exception
  middleware MUST NOT be added.
- **Other seams**: `IEmailService`, `IPdfService`, `IReportPdfService`, `IAppSettings`,
  `IJwtService`, `IPasswordHasher`, `IDomainEventDispatcher`. A genuinely new concern means a new
  interface in `Proposly.Application/Abstractions/` with its implementation in
  `Proposly.Infrastructure/Services/` — never a direct SMTP, filesystem, or HTTP call from a
  handler.
- Auditing (`IAuditableEntity`) and domain event dispatch are performed centrally in
  `AppDbContext.SaveChangesAsync`. Handlers MUST NOT set `CreatedAt` / `UpdatedAt` or dispatch
  events by hand.

Rationale: one implementation per concern means a security or correctness fix lands once and
protects every module.

### IV. Tenant Isolation and Financial Integrity (NON-NEGOTIABLE)

- Every tenant-scoped entity MUST implement `ITenantEntity` (`CompanyId`) so that
  `AppDbContext.OnModelCreating` applies the global query filter automatically. A new entity that
  holds company data and does not implement `ITenantEntity` is a defect.
- Global query filters MUST NOT be bypassed with `IgnoreQueryFilters()` except in explicitly
  cross-tenant SuperAdmin paths guarded by `Policies.SuperAdminOnly`, and each such use MUST
  carry a comment naming the reason.
- Monetary state MUST be held as the `Money` value object from `Proposly.Shared/ValueObjects/` in
  domain entities and their EF configurations. Wire contracts (commands, queries, response DTOs)
  carry a raw `decimal` plus a `Currency` string, and the handler MUST construct `Money` at that
  boundary — a domain entity that stores a bare `decimal` amount is a defect. Non-money decimals
  (quantities, hours, VAT rate, discount percent) stay `decimal` throughout.
- Financial figures MUST be snapshotted at the moment of use (offer amounts, hourly rates, cost
  bases). Historical records MUST NOT be recomputed from live master data.
- Secrets MUST come from configuration (`render.yaml` `sync: false` env vars, mirrored locally in
  the untracked `appsettings.Development.json`). Secrets MUST NOT be committed, logged, or
  returned in an API response.

Rationale: tenant leakage and retroactively mutating financial history are the two failures a
multi-tenant profitability product cannot recover from.

### V. Validated Commands, Rich Domain, Tested Behavior

- Every new command MUST have a FluentValidation `AbstractValidator<TCommand>` in the same folder
  as the command. `ValidatingCommandHandler` resolves it automatically; a missing validator
  silently disables input validation, so its absence is a defect, not an omission. The 19 existing
  id-only commands (deletes, accepts, rejects, status toggles) that carry nothing beyond a `Guid`
  are grandfathered and MUST NOT be retrofitted under Principle I.
- Business rules — price and VAT calculation, status transitions, profitability, plan limits —
  MUST live in domain entities in `Proposly.Domain`. Handlers orchestrate: load aggregate, invoke
  a domain method, save. Anemic entities with logic in handlers or services are a defect.
- `Proposly.Domain` MUST NOT reference EF Core or any infrastructure package; the dependency flow
  `API → Application → Domain ← Infrastructure`, all over `Shared`, is fixed.
- Every new module MUST ship tests placed to mirror the source namespace: domain rules in
  `tests/Proposly.Domain.Tests/<Module>/`, handler behavior in
  `tests/Proposly.Application.Tests/<Module>/`, end-to-end flows in
  `tests/Proposly.Integration.Tests/`. Tests use xUnit with NSubstitute for fakes, named
  `<TypeUnderTest>Tests.cs`.
- Domain invariants and status-transition rules MUST have negative tests proving the invalid
  transition is rejected, not only happy-path coverage.

Rationale: validation, domain-owned rules, and tests at the matching layer are what let modules
be added quickly without regression sweeps across the product.

## Technology and Structural Constraints

The stack is settled. Adding a dependency requires a Governance amendment, and replacing an
existing one requires a MAJOR amendment.

| Concern | Fixed choice |
|---|---|
| Runtime | .NET 10, ASP.NET Core, `net10.0` target across all projects |
| ORM / DB | EF Core over PostgreSQL (Npgsql), single `AppDbContext` |
| Validation | FluentValidation via `ValidatingCommandHandler` |
| PDF | QuestPDF (Community license set in `AddInfrastructure`) |
| Email | MailKit (`MailKitEmailService`) over configurable SMTP |
| Auth | JWT bearer tokens, policy-based authorization |
| API docs | `AddOpenApi` + Scalar, exposed in Development only |
| Testing | xUnit + NSubstitute |
| Frontend | React 19 + TypeScript + Vite, Tailwind + `components/ui`, TanStack Query, axios, react-hook-form + zod, Recharts, Tiptap |
| Deployment | Docker (`Dockerfile`) on Render (`render.yaml`), Frankfurt region |

Additional constraints:

- Schema changes MUST be delivered as EF Core migrations committed under
  `src/Proposly.Infrastructure/Migrations/`. Existing migrations MUST NOT be edited, squashed, or
  deleted — migrations run automatically at startup, so an altered history breaks deployed
  environments.
- New configuration MUST be added to `render.yaml` (`sync: false` for secrets, an inline value
  otherwise) and read through `IConfiguration` or `IAppSettings`, never hardcoded.
- New backend projects MUST be added to both `Proposly.slnx` and the `Dockerfile` explicit
  `.csproj` copy list, or the container build will fail.
- CORS stays a single configured origin (`Cors:AllowedOrigin`); wildcard origins MUST NOT be
  introduced. Auth-adjacent endpoints MUST stay behind the `"auth"` rate limiter.
- The frontend MUST remain buildable with `tsc -b && vite build` and clean under `eslint .`.

## Development Workflow and Quality Gates

**Before planning (mandatory reading order).** For any new feature, read in this order before
producing a spec or plan:

1. `CLAUDE.md` — layer responsibilities and design rules.
2. `docs/` (currently `docs/USER_STORIES.md`) — product intent and existing story coverage.
3. `README.md` — module inventory and local setup.
4. The nearest existing analogous module, end to end — entity, repository interface and
   implementation, EF configuration, command/query folders with validators, controller, frontend
   feature folder and API client.

A plan that does not cite the existing module it mirrors is incomplete and MUST be revised before
implementation begins.

**Definition of done for a new module.** All of the following, or the feature is not done:

- Domain entities own the business rules; repository interfaces sit in `Proposly.Domain`.
- EF configuration added under `Persistence/Configurations/`; `DbSet` added to `AppDbContext`; a
  migration generated and committed.
- Every command has a validator in its own folder.
- Repositories and infrastructure services registered in `AddInfrastructure` with the correct
  lifetime; handlers left to assembly scanning.
- Controller follows the conventions in Principle II and is guarded by a `Policies` constant.
- Tests added at each relevant layer, including negative tests for invariants.
- Frontend feature folder plus a typed API module, if the feature is user-facing.
- `dotnet build` and `dotnet test` pass; the frontend builds and lints clean.
- No existing file changed beyond the minimum additive edits Principle I allows.

**Review gates.** Every change is reviewed against this constitution. A reviewer MUST reject:
restructuring of existing code, a command without a validator, a tenant-scoped entity that skips
`ITenantEntity`, a bare `decimal` amount stored on a domain entity, controller-level exception
mapping, a second
`DbContext` or auth scheme, an edited historical migration, and business logic that landed in a
handler instead of an entity.

**Complexity.** Any deviation MUST be justified in the plan's Complexity Tracking section with the
simpler conforming alternative and the concrete reason it fails. Unjustified deviation is rejected.

## Governance

This constitution supersedes ad hoc convention, prior habit, and individual preference. Where it
and `CLAUDE.md` overlap, this document governs; `CLAUDE.md` remains the day-to-day runtime
guidance for layer responsibilities and design rules and MUST be kept consistent with it.

**Amendment procedure.** Amendments are proposed as a written change to this file stating the
motivating problem, the exact wording change, the version bump and its justification, and — for a
MAJOR change — a migration plan for code already written under the old rule. Amendments take
effect only once merged; no change may be applied "in spirit" ahead of the merge. Adding or
replacing a dependency, adding a backend project, or making a breaking API or schema change all
require an amendment before implementation.

**Versioning policy.** Semantic versioning of governance:

- **MAJOR** — a principle is removed or redefined incompatibly, or a fixed technology choice is
  replaced.
- **MINOR** — a principle or section is added, or existing guidance is materially expanded.
- **PATCH** — clarification, rewording, or a typo fix with no change in obligation.

**Compliance review.** Every pull request and every Spec Kit plan MUST be checked against the
Core Principles and the review gates above. Constitution Check failures block the plan, not just
the code. Principles I and IV are non-negotiable: a violation of either is rejected outright and
cannot be waived by Complexity Tracking.

**Version**: 1.0.1 | **Ratified**: 2026-09-08 | **Last Amended**: 2026-09-08
