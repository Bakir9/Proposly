# Implementation Plan: Work Time Management

**Branch**: `001-work-time-management` | **Date**: 2026-09-10 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-work-time-management/spec.md`

## Summary

Add a `WorkTimeManagement` bounded context covering statutory working-time recording
(Arbeitszeiterfassung), absence management, compliance guardrails, and a month-end report — as a
purely additive module alongside the existing five contexts.

The technical core is three things. First, a new `Timesheet` aggregate with a
Draft → Submitted → Approved → Locked lifecycle that mirrors the existing `Offer` lifecycle idiom,
including its lock-after-transition behaviour. Second, three pure domain services
(`WorkingDayCalculator`, `ComplianceEvaluator`, `BalanceCalculator`) following the existing
`VatCalculator` precedent, so every rule — target hours, breach detection, capped balance carry —
lives in the domain rather than in handlers. Third, a new `IUserOwnedEntity` marker plus a composed
query filter in `AppDbContext`, giving per-employee row isolation automatically in the same way
`ITenantEntity` already gives per-company isolation.

Everything is snapshotted at approval — target hours, actual hours, closing balance, forfeited
surplus, and the acknowledged breach list — so a reported month never changes when master data
later does. Holiday import is deferred, so the module adds **no new package dependency** and needs
no constitution amendment.

Delivered in six shippable phases, backend and frontend together in each.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (`net10.0` across all projects); TypeScript ~6.0 for the
frontend

**Primary Dependencies**: No new packages. Existing only — EF Core 10 + Npgsql 10.0.1,
FluentValidation 11.11.0, QuestPDF 2024.10.2, xUnit 2.9.3 + NSubstitute 5.3.0. Frontend: React 19,
Vite 8, TanStack Query 5, react-hook-form + zod 4, Tailwind 3.4, axios.

**Storage**: PostgreSQL via the single existing `AppDbContext`. One new EF Core migration adding
8 tables. No second context, no second connection string.

**Testing**: xUnit. Domain rules and the three calculators in `Proposly.Domain.Tests/
WorkTimeManagement/`; handlers in `Proposly.Application.Tests/WorkTimeManagement/` with NSubstitute
fakes. One regression test asserting that the composed query filter leaves existing
`ITenantEntity`-only entities' behaviour unchanged.

**Target Platform**: Linux container (Docker → Render, Frankfurt); browser frontend

**Project Type**: Web application — ASP.NET Core API plus React SPA, five existing backend projects.
**No new project is added**, so `Proposly.slnx` and the `Dockerfile` restore layer are untouched.

**Performance Goals**: Month-end report for one employee under 30s including PDF (SC-004); company
overview for 20 employees in a single round trip. A timesheet aggregate is bounded to one month
(≤31 day entries), so no equivalent of the `Project`/`TimeEntry` loading problem arises.

**Constraints**: Additive changes only. Committed migrations are immutable and run automatically at
startup in all environments. Compliance breaches flag, never block (FR-051). Nothing in this
module may alter `TimeEntry` or project profitability (FR-035). No money values — this module deals
in hours, not currency.

**Scale/Scope**: 6 user stories, 74 requirements of which 7 are deferred. 8 new entities,
3 domain services, 5 repositories, ~30 commands/queries, 4 controllers, ~7 frontend pages plus
read-only markers in the existing calendar.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design — see bottom of file.*

### I. Additive Modules Only — PASS

New work lands in new folders: `Domain/WorkTimeManagement/`,
`Application/WorkTimeManagement/`, `Infrastructure/Persistence/Configurations` (new files),
`API/Controllers` (new files), `frontend/src/features/worktime/` and `.../absences/`.

Existing files are touched only for the minimum wiring the constitution permits. The complete list,
so it can be reviewed as a closed set:

| File | Edit | Why it is minimal |
|---|---|---|
| `Shared/Interfaces/IUserOwnedEntity.cs` | new file | New marker interface |
| `Infrastructure/Persistence/AppDbContext.cs` | add `DbSet`s; compose the filter loop | Registration + the one mechanism change (see Complexity Tracking) |
| `Application/Abstractions/ICurrentUserService.cs` | add `CanViewAllEmployees` | Lets the filter express approver access without dropping tenant isolation |
| `API/Services/CurrentUserService.cs` | implement the new member | Follows the interface |
| `API/Authorization/Policies.cs` | add 4 constants | Constitution requires policy names come from here |
| `API/Extensions/AuthorizationExtensions.cs` | register the 4 policies | Same |
| `Infrastructure/DependencyInjection.cs` | register 5 repositories + 1 PDF service | Explicit registration is the documented convention |
| `Domain/CompanyManagement/Entities/Company.cs` | add `HasWorkTimeModule()` | Plan gating, mirroring `IsProjectLimitReached()` |
| `frontend/src/components/AppShell.tsx` | add nav entries | Navigation entry, explicitly permitted |
| existing calendar components | accept read-only markers | Additive props; no `Termin` is written |

Nothing is renamed, relocated, re-layered, or reformatted. `TimeEntry`, `Project`, and the
profitability calculation are not modified. The `context/` vs `contexts/` inconsistency is left
frozen. Holiday import is deferred, so no endpoint or contract is removed or changed.

### II. Existing Conventions Are the Spec — PASS

Folder-per-operation under `Commands/<Name>/` and `Queries/<Name>/` with a sibling validator;
grouped `Responses/` DTO files; hand-rolled CQRS with no MediatR; handlers left to assembly
scanning while repositories and services are registered explicitly with existing lifetimes
(repositories `Scoped`, the new PDF service `Singleton` like `IReportPdfService`); `sealed`
controllers with `[ApiController]`, `[Route("api/[controller]")]`, `[FromServices]` handlers and
`CancellationToken ct` last. Frontend follows `api/<feature>.ts` over the shared axios client plus
`features/<feature>/` pages with TanStack Query and react-hook-form + zod.

The `Offer` aggregate is the named template for the `Timesheet` lifecycle; `VatCalculator` is the
named template for the three domain services; `QuarterlyReportPdfService` is the named template for
the monthly report PDF.

### III. Reuse Platform Seams, Never Fork Them — PASS

One `AppDbContext`. Existing JWT auth, `Policies`, `ICurrentUserService`, `TenantMiddleware`.
Failures signalled by `CommandValidationException` and `InvalidOperationException` for the existing
`Program.cs` handler to map — no controller try/catch, no new exception middleware. Approval
notifications reuse the existing domain-event → `Notification` handlers. The report PDF is a new
implementation behind a new Application-layer interface, alongside the existing QuestPDF services.
No SMTP, filesystem, or HTTP call is made from a handler; with import deferred, the module makes no
outbound calls at all.

### IV. Tenant Isolation and Financial Integrity — PASS

All 8 new entities implement `ITenantEntity`. Five of them additionally implement the new
`IUserOwnedEntity`. `IgnoreQueryFilters()` is **not** used anywhere in this module — approver access
is expressed inside the filter predicate, so company isolation can never be dropped by accident
(see research.md, Decision 2).

The `Money` rule is not engaged: this module records hours, days, and percentages. Per CLAUDE.md,
genuinely non-money decimals stay `decimal`, so `WorkedHours`, `WeeklyHours`, `ConsumedDays`, and
the balance figures are all `decimal`. No monetary value is stored, and no rate is read.

The snapshot rule is central rather than incidental: `EmploymentTerms` is versioned by
`ValidFrom` so a mid-year change cannot rewrite earlier months (FR-022, FR-023), and a `Timesheet`
freezes its target, actual, balance, forfeiture, and breach list at approval (FR-060, FR-032).

### V. Validated Commands, Rich Domain, Tested Behavior — PASS

Every command carrying a payload gets a sibling `AbstractValidator`. Id-only commands (submit,
cancel, reopen) follow the existing grandfathered pattern and carry none. All rules live in entities
and the three pure domain services; `Proposly.Domain` gains no package reference. Tests mirror the
source namespace at each layer, with negative tests for every rejected transition and for the
filter regression.

### Technology and Structural Constraints — PASS

No dependency added, so no amendment is required. No new backend project, so `Proposly.slnx` and
the `Dockerfile` are untouched. One new migration, additive, committed. New configuration is
limited to nothing — the rule set and flexitime bounds are per-company database rows, not env vars,
so `render.yaml` is unchanged.

## Project Structure

### Documentation (this feature)

```text
specs/001-work-time-management/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output — 9 resolved design decisions
├── data-model.md        # Phase 1 output — entities, invariants, state machine
├── quickstart.md        # Phase 1 output — end-to-end validation guide
├── contracts/           # Phase 1 output — REST contracts per controller
│   ├── timesheets.md
│   ├── absences.md
│   ├── worktime-settings.md
│   └── worktime-reports.md
└── checklists/
    └── requirements.md  # Spec quality checklist (16/16)
```

### Source Code (repository root)

```text
src/
├── Proposly.Shared/
│   └── Interfaces/
│       └── IUserOwnedEntity.cs                    # NEW — per-employee row ownership
│
├── Proposly.Domain/
│   ├── CompanyManagement/Entities/Company.cs      # EDIT — HasWorkTimeModule()
│   └── WorkTimeManagement/                        # NEW CONTEXT
│       ├── Entities/       Timesheet, WorkDayEntry, TimesheetBreach,
│       │                   AbsenceRequest, AbsenceEntitlement,
│       │                   EmploymentTerms, NonWorkingDay, WorkTimePolicy, BreakRule
│       ├── Enums/          TimesheetStatus, AbsenceType, AbsenceStatus, BreachKind,
│       │                   NonWorkingDayKind, EntrySource, WeekDays (flags)
│       ├── Events/         TimesheetSubmitted/Approved/ReturnedForCorrection,
│       │                   AbsenceRequested/Approved/Rejected
│       ├── Services/       WorkingDayCalculator, ComplianceEvaluator, BalanceCalculator
│       └── Repositories/   ITimesheetRepository, IAbsenceRepository,
│                           IEmploymentTermsRepository, INonWorkingDayRepository,
│                           IWorkTimePolicyRepository
│
├── Proposly.Application/
│   ├── Abstractions/ICurrentUserService.cs        # EDIT — CanViewAllEmployees
│   └── WorkTimeManagement/                        # NEW MODULE
│       ├── Commands/<Name>/  command + handler + validator
│       ├── Queries/<Name>/   query + handler
│       ├── Responses/        TimesheetResponses, AbsenceResponses,
│       │                     WorkTimeSettingsResponses, WorkTimeReportResponses
│       ├── EventHandlers/    → existing Notification entity
│       └── Services/         IWorkTimeReportPdfService
│
├── Proposly.Infrastructure/
│   ├── DependencyInjection.cs                     # EDIT — register repos + PDF service
│   ├── Persistence/
│   │   ├── AppDbContext.cs                        # EDIT — DbSets + composed filter loop
│   │   ├── Configurations/                        # NEW — 8 IEntityTypeConfiguration<T>
│   │   └── Repositories/                          # NEW — 5 implementations
│   ├── Migrations/                                # NEW — AddWorkTimeManagement
│   └── Services/Pdf/WorkTimeReportPdfService.cs   # NEW — QuestPDF
│
└── Proposly.API/
    ├── Authorization/Policies.cs                  # EDIT — 4 constants
    ├── Extensions/AuthorizationExtensions.cs      # EDIT — register 4 policies
    ├── Services/CurrentUserService.cs             # EDIT — implement new member
    └── Controllers/                               # NEW — TimesheetsController,
                                                   # AbsencesController,
                                                   # WorkTimeSettingsController,
                                                   # WorkTimeReportsController

tests/
├── Proposly.Domain.Tests/WorkTimeManagement/      # NEW — entities + 3 calculators
└── Proposly.Application.Tests/WorkTimeManagement/ # NEW — handlers + filter regression

frontend/src/
├── api/          timesheets.ts, absences.ts, worktime-settings.ts, worktime-reports.ts   # NEW
├── features/
│   ├── worktime/  TimesheetPage, ApprovalsPage, CompanyOverviewPage,
│   │              MonthlyReportPage, ReconciliationPanel                                 # NEW
│   ├── absences/  AbsencesPage, AbsenceApprovalsPage                                     # NEW
│   ├── settings/  EmploymentTermsPage, HolidayCalendarPage, WorkTimePolicyPage            # NEW
│   └── calendar/  existing components                                                    # EDIT
└── components/AppShell.tsx                                                                # EDIT
```

**Structure Decision**: The existing five-project Clean Architecture layout is used unchanged. The
module is a sixth bounded context inside `Proposly.Domain` and a thirteenth module inside
`Proposly.Application`, exactly parallel to `CalendarManagement`. No new project is introduced,
which keeps `Proposly.slnx`, the `Dockerfile` restore layer, and the container build untouched.
Frontend work follows the established `api/` + `features/` split, with two new feature folders and
three settings pages.

## Delivery Phases

Each phase is independently shippable and deployable, backend and frontend together.

| Phase | Story | Backend | Frontend |
|---|---|---|---|
| **1** | US1 — Record working time | Foundation (`IUserOwnedEntity`, composed filter, `CanViewAllEmployees`, policies, plan gate) + `Timesheet`/`WorkDayEntry` + repo + migration + 8 commands/queries | Month grid with per-day entry, running total, submit; approver review and return |
| **2** | US6 — Compliance guardrails | `WorkTimePolicy` + `BreakRule` + `TimesheetBreach` + `ComplianceEvaluator` + 4 queries + acknowledgement on approve | Inline breach flags on the grid, approver acknowledgement step, company compliance overview |
| **3** | US2 — Absence | `AbsenceRequest` + `AbsenceEntitlement` + repo + 6 commands/queries + 3 event handlers → notifications | Request dialog with live balance, my-absences list, approvals inbox |
| **4** | US3 — Terms & holidays | `EmploymentTerms` (versioned) + `NonWorkingDay` + `WorkingDayCalculator` + repos + 8 commands/queries | Employment terms admin, holiday calendar admin, policy editor, read-only markers in existing calendar |
| **5** | US4 — Month-end report | `BalanceCalculator` + snapshot fields + `WorkTimeReportPdfService` + 3 queries | Per-employee report page, company month overview, PDF download |
| **6** | US5 — Reconciliation | One read-only query joining `TimeEntry` → `ProjectMember` → `User` | Comparison panel |

Phase ordering note: Phase 2 precedes absence because breach detection needs only recorded days and
the policy. Phases 1–3 flag breaches against a policy whose target hours are not yet contract-aware;
Phase 4 completes that, and Phase 5 depends on Phases 3 and 4 for correct target hours.

## Complexity Tracking

> One item touches shared infrastructure and is recorded here for reviewer attention.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| `AppDbContext.OnModelCreating` filter application is changed from applying one predicate to composing the applicable predicates per entity | EF Core replaces rather than ANDs a second `HasQueryFilter` call on the same entity, so per-employee isolation cannot be added as an independent second filter without either composing predicates or adopting EF 10 named filters. Composition is version-independent and keeps a single expression per entity. | Enforcing user scoping in the new module's repositories by hand was rejected because nothing prevents a future query from omitting it, and the failure mode is one employee reading a colleague's sick leave — health-adjacent data where a forgettable rule is not acceptable. Mitigation: entities implementing only `ITenantEntity` must produce a byte-identical predicate, guarded by a dedicated regression test, and no existing entity implements the new interface. |

## Post-Design Constitution Re-Check

Re-evaluated after Phase 1 artifacts were written. **All gates still PASS.**

Two things were confirmed rather than assumed during design:

- **No `IgnoreQueryFilters()` anywhere.** The initial sketch used it for approver access, which
  would have dropped the *tenant* filter alongside the user filter and allowed cross-company reads
  in approver queries. Approver access moved inside the filter predicate via
  `CanViewAllEmployees` (research.md, Decision 2). This strengthens Principle IV rather than
  bending it.
- **No new dependency.** With holiday import deferred, the module makes no outbound calls, so the
  Technology Constraints gate needs no amendment. Should import be picked up later, FR-066–FR-072
  are already specified and an amendment is required first.

No new violations. The single Complexity Tracking entry stands as the only shared-infrastructure
change, and it is additive with a named regression guard.
