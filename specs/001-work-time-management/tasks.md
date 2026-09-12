---

description: "Task list for Work Time Management implementation"
---

# Tasks: Work Time Management

**Input**: Design documents from `/specs/001-work-time-management/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md),
[data-model.md](./data-model.md), [contracts/](./contracts/)

**Tests**: Included and **mandatory** — Constitution Principle V requires tests at each layer with
negative tests for invariants and rejected transitions. This is not optional for this feature.

**Organization**: Grouped by user story so each is independently implementable and shippable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete work)
- **[Story]**: `[US1]`–`[US6]`, mapping to the spec's user stories
- Exact file paths included in every task

## Phase numbering

The plan describes six **delivery** phases (one per story). This file follows the task template's
numbering, so they map as:

| Delivery phase (plan.md) | Story | This file |
|---|---|---|
| — | — | Phase 1 Setup, Phase 2 Foundational |
| 1 · Record working time | US1 (P1) | **Phase 3** 🎯 MVP |
| 2 · Compliance guardrails | US6 (P2) | Phase 4 |
| 3 · Absence | US2 (P2) | Phase 5 |
| 4 · Terms & holidays | US3 (P3) | Phase 6 |
| 5 · Month-end report | US4 (P4) | Phase 7 |
| 6 · Reconciliation | US5 (P5) | Phase 8 |
| — | — | Phase 9 Polish |

**Holiday import (FR-066 to FR-072) is deferred — no task below builds it.**

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the module's folder skeleton. No project is added, so `Proposly.slnx` and the
`Dockerfile` are untouched.

- [X] T001 [P] Create domain folders `src/Proposly.Domain/WorkTimeManagement/{Entities,Enums,Events,Services,Repositories}/`
- [X] T002 [P] Create application folders `src/Proposly.Application/WorkTimeManagement/{Commands,Queries,Responses,EventHandlers,Services}/`
- [X] T003 [P] Create test folders `tests/Proposly.Domain.Tests/WorkTimeManagement/` and `tests/Proposly.Application.Tests/WorkTimeManagement/`
- [X] T004 [P] Create frontend folders `frontend/src/features/worktime/` and `frontend/src/features/absences/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Per-employee visibility, authorization, plan gating, and a testable clock.

**⚠️ CRITICAL**: No user story work may begin until this phase is complete and green. T008 is the
single riskiest change in the feature — it touches the filter every existing query passes through.

- [X] T005 Create `IUserOwnedEntity` with a `Guid UserId { get; }` member in `src/Proposly.Shared/Interfaces/IUserOwnedEntity.cs`
- [X] T006 Add `bool CanViewAllEmployees { get; }` to `src/Proposly.Application/Abstractions/ICurrentUserService.cs`
- [X] T007 Implement `CanViewAllEmployees` in `src/Proposly.API/Services/CurrentUserService.cs` returning true for Owner, Admin, and SuperAdmin roles. Implemented **non-throwing** (no HTTP context ⇒ `false`), because the filter can be evaluated outside a request. Note there are **three** implementations of `ICurrentUserService`, not one — the other two are `DesignTimeCurrentUserService` (T009) and `SeedCurrentUserService` in `src/Proposly.Infrastructure/Persistence/DataSeeder.cs`, which returns `true` to stay consistent with its `Role => "Owner"`
- [X] T008 Refactor `OnModelCreating` in `src/Proposly.Infrastructure/Persistence/AppDbContext.cs` to compose the applicable filter predicates per entity — tenant predicate for `ITenantEntity`, ANDed with `e.UserId == _currentUserService.UserId || _currentUserService.CanViewAllEmployees` for `IUserOwnedEntity`. Entities implementing only `ITenantEntity` MUST keep an identical predicate. Do **not** use EF named filters (research.md Decision 2)
- [X] T009 Add `DesignTimeCurrentUserService.CanViewAllEmployees` in `src/Proposly.Infrastructure/Persistence/AppDbContextFactory.cs` so `dotnet ef` still builds
- [X] T010 Register `TimeProvider.System` as a singleton in `src/Proposly.Infrastructure/DependencyInjection.cs`. New module code only — do **not** retrofit the 53 existing `DateTime.UtcNow` call sites
- [X] T011 Add `HasWorkTimeModule()` to `src/Proposly.Domain/CompanyManagement/Entities/Company.cs` returning true for Pro and Business, honouring the existing expired-plan fallback
- [X] T012 Add `RecordOwnWorkTime`, `ApproveWorkTime`, `ManageWorkTimeSettings`, `ViewAllWorkTime` constants to `src/Proposly.API/Authorization/Policies.cs`
- [X] T013 Register those four policies in `src/Proposly.API/Extensions/AuthorizationExtensions.cs` — `RecordOwnWorkTime` for any authenticated company user; the other three for Owner and Admin

### Foundational tests

- [X] T014 [P] **Query-filter regression test** in `tests/Proposly.Integration.Tests/WorkTimeManagement/QueryFilterRegressionTests.cs` asserting every `ITenantEntity` still has a query filter, and that no tenant-only entity's filter references `UserId`. *(Relocated from `Application.Tests`, which does not reference `Proposly.Infrastructure` and so cannot construct `AppDbContext`. `Integration.Tests` references the API project, and the model builds without a database connection.)*
- [X] T015 [P] Test in the same file that **no existing entity implements `IUserOwnedEntity`**, which is what makes "no existing query changes behaviour" provable in this phase. *(The behavioural test — own rows only when `CanViewAllEmployees` is false, all rows when true, never across companies — requires a user-owned entity to exist, so it lands in Phase 3 with `Timesheet`; see T018. Replace this assertion then rather than deleting it.)*
- [X] T016 [P] Test `Company.HasWorkTimeModule()` for each `PlanTier` and for an expired plan falling back to Free in `tests/Proposly.Domain.Tests/WorkTimeManagement/CompanyPlanGateTests.cs`

**Checkpoint**: `dotnet build` and `dotnet test` green. Existing behaviour provably unchanged.

---

## Phase 3: User Story 1 — Record my working time (Priority: P1) 🎯 MVP

**Goal**: An employee records start, end, and break per day into a monthly timesheet, submits it,
and an approver approves, returns, locks, or reopens it.

**Independent Test**: Record a week, submit, approve as Owner, confirm the month becomes read-only
and a Member cannot see a colleague's month. Delivers a defensible statutory record alone.

### Tests for User Story 1

- [X] T017 [P] [US1] `WorkDayEntry` tests in `tests/Proposly.Domain.Tests/WorkTimeManagement/WorkDayEntryTests.cs` — worked-hours arithmetic, midnight crossing attributed to the start date, and **negative**: end before start, break ≥ span
- [X] T018 [P] [US1] `Timesheet` transition tests in `tests/Proposly.Domain.Tests/WorkTimeManagement/TimesheetTests.cs` — every valid transition plus **negative** tests for editing outside Draft, a date outside the month, submitting a non-Draft, approving a non-Submitted, and reopening a Draft. **Also replace the placeholder assertion in T015**: now that `Timesheet` implements `IUserOwnedEntity`, assert in `tests/Proposly.Integration.Tests/WorkTimeManagement/QueryFilterRegressionTests.cs` that its filter scopes to the current user when `CanViewAllEmployees` is false, opens up when true, and never crosses companies in either case
- [X] T019 [P] [US1] Handler tests in `tests/Proposly.Application.Tests/WorkTimeManagement/TimesheetHandlerTests.cs` with NSubstitute — lazy Draft creation on first read, plan gate rejection below Pro, and approver-vs-member behaviour

### Domain for User Story 1

- [X] T020 [P] [US1] `TimesheetStatus` enum (`Draft, Submitted, Approved, Locked`) in `src/Proposly.Domain/WorkTimeManagement/Enums/TimesheetStatus.cs`
- [X] T021 [P] [US1] `WorkDayEntry` entity in `src/Proposly.Domain/WorkTimeManagement/Entities/WorkDayEntry.cs` — private ctor for EF, static `Create`, computed and stored `WorkedHours`, invariants per data-model.md
- [X] T022 [US1] `Timesheet` aggregate root in `src/Proposly.Domain/WorkTimeManagement/Entities/Timesheet.cs` implementing `ITenantEntity`, `IAuditableEntity`, `IUserOwnedEntity` — `AddOrUpdateDay`, `RemoveDay`, `Submit`, `Approve`, `ReturnForCorrection`, `Lock`, `Reopen`, `TotalWorkedHours`. **A limit breach must never block `AddOrUpdateDay` (FR-051)**
- [X] T023 [P] [US1] Three domain events in `src/Proposly.Domain/WorkTimeManagement/Events/` — `TimesheetSubmittedDomainEvent`, `TimesheetApprovedDomainEvent`, `TimesheetReturnedForCorrectionDomainEvent`
- [X] T024 [US1] `ITimesheetRepository` in `src/Proposly.Domain/WorkTimeManagement/Repositories/ITimesheetRepository.cs` — by user and month, company month, pending queue. No EF types

### Persistence for User Story 1

- [X] T025 [P] [US1] `TimesheetConfiguration` in `src/Proposly.Infrastructure/Persistence/Configurations/TimesheetConfiguration.cs` with a unique index on `(CompanyId, UserId, Year, Month)`
- [X] T026 [P] [US1] `WorkDayEntryConfiguration` in `src/Proposly.Infrastructure/Persistence/Configurations/WorkDayEntryConfiguration.cs` with a unique index on `(TimesheetId, Date)`
- [X] T027 [US1] Add `Timesheets` and `WorkDayEntries` `DbSet`s to `src/Proposly.Infrastructure/Persistence/AppDbContext.cs`
- [X] T028 [US1] `TimesheetRepository` in `src/Proposly.Infrastructure/Persistence/Repositories/TimesheetRepository.cs`. **No `IgnoreQueryFilters()`** — approver access comes from the composed filter
- [X] T029 [US1] Register `ITimesheetRepository` as `Scoped` in `src/Proposly.Infrastructure/DependencyInjection.cs`
- [X] T030 [US1] Generate migration `AddWorkTimeTimesheets` — `dotnet ef migrations add AddWorkTimeTimesheets --project src/Proposly.Infrastructure --startup-project src/Proposly.API`; commit it and never edit it afterwards

### Application for User Story 1

- [X] T031 [P] [US1] `UpsertWorkDayCommand` + handler + **validator** in `src/Proposly.Application/WorkTimeManagement/Commands/UpsertWorkDay/`
- [X] T032 [P] [US1] `DeleteWorkDayCommand` + handler in `src/Proposly.Application/WorkTimeManagement/Commands/DeleteWorkDay/` (id-only, no validator)
- [X] T033 [P] [US1] `SubmitTimesheetCommand` + handler in `src/Proposly.Application/WorkTimeManagement/Commands/SubmitTimesheet/` (id-only)
- [X] T034 [P] [US1] `ApproveTimesheetCommand` + handler + **validator** in `src/Proposly.Application/WorkTimeManagement/Commands/ApproveTimesheet/`
- [X] T035 [P] [US1] `ReturnTimesheetCommand` + handler + **validator** in `src/Proposly.Application/WorkTimeManagement/Commands/ReturnTimesheet/`
- [X] T036 [P] [US1] `LockTimesheetCommand` and `ReopenTimesheetCommand` + handlers in their own folders under `Commands/` (id-only)
- [X] T037 [P] [US1] `TimesheetResponses.cs` in `src/Proposly.Application/WorkTimeManagement/Responses/` — `TimesheetDetailResponse`, `TimesheetSummaryResponse`, `WorkDayResponse` per contracts/timesheets.md
- [X] T038 [P] [US1] `GetMyTimesheetQuery` + handler in `src/Proposly.Application/WorkTimeManagement/Queries/GetMyTimesheet/`, creating a Draft lazily on first read
- [X] T039 [P] [US1] `GetPendingTimesheetsQuery` + handler in `.../Queries/GetPendingTimesheets/`
- [X] T040 [P] [US1] `GetCompanyMonthQuery` + handler in `.../Queries/GetCompanyMonth/`
- [X] T041 [US1] Three `IDomainEventHandler<T>` implementations in `src/Proposly.Application/WorkTimeManagement/EventHandlers/` creating existing `Notification` rows on submit, approve, and return

### API for User Story 1

- [X] T042 [US1] `TimesheetsController` in `src/Proposly.API/Controllers/TimesheetsController.cs` — all routes per contracts/timesheets.md, `sealed`, `[ApiController]`, `[Route("api/[controller]")]`, class policy `RecordOwnWorkTime`, approver actions `ApproveWorkTime`, company actions `ViewAllWorkTime`, handlers via `[FromServices]`, `CancellationToken ct` last, no try/catch

### Frontend for User Story 1

- [X] T043 [P] [US1] `frontend/src/api/timesheets.ts` — typed client over the shared axios instance
- [X] T044 [US1] `TimesheetPage.tsx` in `frontend/src/features/worktime/` — month grid, per-day start/end/break inputs, live monthly total, submit; react-hook-form + zod, TanStack Query
- [X] T045 [US1] `ApprovalsPage.tsx` in `frontend/src/features/worktime/` — pending queue, approve, return for correction
- [X] T046 [US1] `CompanyOverviewPage.tsx` in `frontend/src/features/worktime/` — company month list for Owner and Admin
- [X] T047 [US1] Add routes in `frontend/src/App.tsx` and role-based nav entries in `frontend/src/components/AppShell.tsx`

**Checkpoint**: US1 fully functional. Run the Phase 1 section of [quickstart.md](./quickstart.md),
including the two visibility checks. Shippable.

### Phase 3 deviations from the design docs

Five decisions differ from `data-model.md` / `contracts/timesheets.md`. Each is deliberate; update
the design docs or reverse the code, but do not leave them silently divergent.

1. **`WorkDayEntry.CrossesMidnight` is an explicit flag.** `data-model.md` said to add 24h whenever
   `EndTime <= StartTime`, which makes FR-002's "reject an end before its start" unreachable and
   contradicts Story 1 scenario 4 (17:00–08:30 must be rejected). Start and end times alone cannot
   distinguish a night shift from transposed times, so the caller states which it is. Default
   `false` ⇒ end must be after start.
2. **A real bug this caught**: `TimeOnly` subtraction *wraps* around midnight rather than going
   negative, so `08:30 - 17:00` yields 15.5h, not −8.5h. The first implementation therefore never
   rejected transposed times. `CalculateSpanMinutes` now compares the times explicitly.
3. **Reading a month does not persist it.** `contracts/timesheets.md` said `GET {year}/{month}`
   creates a Draft lazily; a query that writes is the wrong shape, and browsing months would leave
   empty rows behind. `TimesheetDetailResponse.Id` is nullable and the row is created by the first
   `PUT …/days/{date}` instead.
4. **The plan gate applies to `UpsertWorkDay` only**, not to delete, submit, approve, lock, or
   reopen. The spec's intent is that a downgraded company keeps read access but cannot create new
   records — blocking lifecycle transitions would trap a customer mid-close on data they already
   own.
5. **`WorkDayEntry` is not exposed as a `DbSet`.** It carries no `CompanyId`, so a direct query on
   it would bypass both the tenant and the per-employee filter. It is reached via `Timesheet.Days`;
   `Work_day_entries_are_not_directly_queryable` guards this.

Also note: validators were written for **all six** commands, including the id-only ones, per
Constitution Principle V ("every new command MUST have a validator") rather than the grandfathered
id-only exception. `ApproveTimesheetCommand` gains its `AcknowledgeBreaches` flag in Phase 4 (T068).

---

## Phase 4: User Story 6 — Working time compliance guardrails (Priority: P2)

**Goal**: Every recorded day is checked against the company's statutory rule set; breaches are
flagged, never blocked, and an approver must acknowledge them before approving.

**Independent Test**: Record a 13.5-hour day with a 20-minute break — it saves, and is flagged for
both the daily maximum and the insufficient break.

### Tests for User Story 6

- [X] T048 [P] [US6] `ComplianceEvaluator` tests in `tests/Proposly.Domain.Tests/WorkTimeManagement/ComplianceEvaluatorTests.cs` — all five `BreachKind` values, AT single break tier vs DE two tiers, rest breach across a month boundary, and approved absence days contributing nothing
- [X] T049 [P] [US6] **Critical negative test** in the same file: a day exceeding every limit still saves and is only flagged (FR-051, SC-015)
- [X] T050 [P] [US6] `WorkTimePolicy` versioning tests in `tests/Proposly.Domain.Tests/WorkTimeManagement/WorkTimePolicyTests.cs` — successor closes predecessor, forward-only application
- [X] T051 [P] [US6] Handler test in `tests/Proposly.Application.Tests/WorkTimeManagement/ApproveWithBreachesTests.cs` — approving with breaches and `AcknowledgeBreaches: false` is rejected; with true it persists the snapshot

### Implementation for User Story 6

- [X] T052 [P] [US6] `BreachKind` and `EntrySource` enums in `src/Proposly.Domain/WorkTimeManagement/Enums/`
- [X] T053 [P] [US6] `BreakRule` entity in `src/Proposly.Domain/WorkTimeManagement/Entities/BreakRule.cs`
- [X] T054 [US6] `WorkTimePolicy` aggregate in `src/Proposly.Domain/WorkTimeManagement/Entities/WorkTimePolicy.cs` — versioned by `ValidFrom`, owns `BreakRule`s, carries limits plus `SurplusCapHours` and `DeficitFloorHours` per data-model.md
- [X] T055 [P] [US6] `TimesheetBreach` entity in `src/Proposly.Domain/WorkTimeManagement/Entities/TimesheetBreach.cs`
- [X] T056 [US6] `ComplianceEvaluator` pure domain service in `src/Proposly.Domain/WorkTimeManagement/Services/ComplianceEvaluator.cs`, mirroring the `VatCalculator` pattern
- [X] T057 [US6] Extend `Timesheet.Approve` to accept the evaluated breaches and an acknowledgement flag, throwing when breaches exist and acknowledgement is false; add the `_breaches` collection
- [X] T058 [P] [US6] `IWorkTimePolicyRepository` in `src/Proposly.Domain/WorkTimeManagement/Repositories/`
- [X] T059 [P] [US6] `WorkTimePolicyConfiguration`, `BreakRuleConfiguration`, and `TimesheetBreachConfiguration` in `src/Proposly.Infrastructure/Persistence/Configurations/`
- [X] T060 [US6] Add the three `DbSet`s to `AppDbContext`, implement `WorkTimePolicyRepository`, register it `Scoped`
- [X] T061 [US6] Generate and commit migration `AddWorkTimePolicy`
- [X] T062 [P] [US6] Shipped AT and DE default rule sets as a static factory in `src/Proposly.Domain/WorkTimeManagement/Services/WorkTimePolicyDefaults.cs`, values per data-model.md
- [X] T063 [P] [US6] `CreateWorkTimePolicyCommand` + handler + **validator** in `src/Proposly.Application/WorkTimeManagement/Commands/CreateWorkTimePolicy/`
- [X] T064 [P] [US6] `GetWorkTimePolicyQuery` and `GetPolicyDefaultsQuery` + handlers in `.../Queries/`
- [X] T065 [P] [US6] `GetCompanyBreachesQuery` + handler in `.../Queries/GetCompanyBreaches/`
- [X] T066 [US6] Wire live breach computation into `GetMyTimesheetQueryHandler` for Draft and Submitted months; read persisted rows for Approved and Locked (research.md Decision 3)
- [X] T067 [US6] `WorkTimeSettingsController` in `src/Proposly.API/Controllers/` — policy routes only for now, per contracts/worktime-settings.md
- [X] T068 [US6] Add breach routes to `TimesheetsController` and extend the approve action with the acknowledgement flag
- [X] T069 [P] [US6] `frontend/src/api/worktime-settings.ts`
- [X] T070 [US6] Render inline breach flags on `TimesheetPage.tsx`, add the acknowledgement step to `ApprovalsPage.tsx`
- [X] T071 [US6] `WorkTimePolicyPage.tsx` in `frontend/src/features/settings/` — seeded from defaults, editable, "no rule set active" prompt when absent (FR-064)
- [X] T072 [US6] Company compliance overview in `frontend/src/features/worktime/CompanyOverviewPage.tsx`

**Checkpoint**: US1 + US6 work. Run the Phase 2 section of quickstart.md. Shippable.

### Phase 4 deviations and carry-overs

1. **`WorkTimePolicy` consolidates two spec entities**, as planned in research.md Decision 4 — the
   rule set and the flexitime bounds are one versioned aggregate, since both come from the same
   written agreement.
2. **Approved absence days are not yet excluded from limits (FR-057).** `AbsenceRequest` does not
   exist until Phase 5, so `ComplianceEvaluator` has no absence parameter. **Phase 5 must add it**
   — see the added task T096a. Until then a day of approved leave simply has no recorded hours, so
   it contributes nothing anyway; the gap only shows once absence can credit or excuse hours.
3. **`GetCompanyBreachesQuery` takes a year and month**, not the arbitrary `from`/`to` range in
   `contracts/worktime-settings.md`. It matches the other company view and stays a single indexed
   lookup. Update the contract or widen the query later.
4. **A new repository method was needed**: `ITimesheetRepository.GetDaysInRangeAsync`. Rest checks
   reach back one day across the month boundary, and the averaging window reaches back up to 24
   weeks, so the evaluator needs days from outside the month being judged.
5. **`Reopen()` now clears the acknowledged breaches**, because they belonged to the withdrawn
   approval. The month is evaluated live again while open.
6. **Neither `TimesheetBreach` nor `BreakRule` is exposed as a `DbSet`**, for the same reason as
   `WorkDayEntry` — no `CompanyId`, so a direct query would bypass both filters.
7. **Two frontend lint errors were introduced and fixed rather than left.** `WorkTimePolicyPage`
   originally synced form state in a `useEffect` (the pattern that already fails lint in
   `SettingsPage`); it now derives the form from the query and only holds a draft once edited.
   `breachedDates` moved from `BreachList.tsx` to `api/worktime-settings.ts` to keep the component
   file exporting only components.

---

## Phase 5: User Story 2 — Request and approve time off (Priority: P2)

**Goal**: Employees request absence with a live entitlement balance; approvers decide; approved
vacation consumes entitlement.

**Independent Test**: Request five vacation days, approve as Owner, confirm the balance drops by
five.

### Tests for User Story 2

- [X] T073 [P] [US2] `AbsenceRequest` tests in `tests/Proposly.Domain.Tests/WorkTimeManagement/AbsenceRequestTests.cs` — transitions plus **negative** tests for approving a non-Pending, cancelling a started absence, and a second decision on a decided request
- [X] T074 [P] [US2] `AbsenceEntitlement` tests in `.../AbsenceEntitlementTests.cs` — consume, release, remaining, and **negative** over-consumption
- [X] T075 [P] [US2] Handler tests in `tests/Proposly.Application.Tests/WorkTimeManagement/AbsenceHandlerTests.cs` — overlap refusal, entitlement refusal, sick leave not consuming entitlement, member sees only own

### Implementation for User Story 2

- [X] T076 [P] [US2] `AbsenceType` and `AbsenceStatus` enums in `src/Proposly.Domain/WorkTimeManagement/Enums/`
- [X] T077 [US2] `AbsenceRequest` aggregate in `src/Proposly.Domain/WorkTimeManagement/Entities/AbsenceRequest.cs` implementing the three marker interfaces. **No field for a diagnosis or medical detail (FR-019)**
- [X] T078 [P] [US2] `AbsenceEntitlement` aggregate in `src/Proposly.Domain/WorkTimeManagement/Entities/AbsenceEntitlement.cs`
- [X] T079 [P] [US2] Three domain events in `src/Proposly.Domain/WorkTimeManagement/Events/` — requested, approved, rejected
- [X] T080 [P] [US2] `IAbsenceRepository` in `.../Repositories/` — overlap check, pending queue, entitlement by user and year
- [X] T081 [P] [US2] `AbsenceRequestConfiguration` and `AbsenceEntitlementConfiguration` in `src/Proposly.Infrastructure/Persistence/Configurations/`, unique index `(CompanyId, UserId, Year)` on entitlement
- [X] T082 [US2] Add both `DbSet`s to `AppDbContext`, implement `AbsenceRepository`, register it `Scoped`
- [X] T083 [US2] Generate and commit migration `AddWorkTimeAbsence`
- [X] T084 [P] [US2] `RequestAbsenceCommand` + handler + **validator** in `.../Commands/RequestAbsence/` — computes consumed days server-side and freezes them
- [X] T085 [P] [US2] `ApproveAbsenceCommand` + handler in `.../Commands/ApproveAbsence/` (id-only) — consumes entitlement for Vacation only
- [X] T086 [P] [US2] `RejectAbsenceCommand` + handler + **validator** in `.../Commands/RejectAbsence/`
- [X] T087 [P] [US2] `CancelAbsenceCommand` + handler in `.../Commands/CancelAbsence/` (id-only) — releases entitlement
- [X] T088 [P] [US2] `SetEntitlementCommand` + handler + **validator** in `.../Commands/SetEntitlement/`
- [X] T089 [P] [US2] `AbsenceResponses.cs` in `.../Responses/` per contracts/absences.md
- [X] T090 [P] [US2] `GetAbsencesQuery`, `GetPendingAbsencesQuery`, `GetEntitlementQuery`, `PreviewAbsenceQuery` + handlers in `.../Queries/`
- [X] T091 [US2] Three notification event handlers in `src/Proposly.Application/WorkTimeManagement/EventHandlers/`
- [X] T092 [US2] `AbsencesController` in `src/Proposly.API/Controllers/AbsencesController.cs` per contracts/absences.md
- [X] T093 [P] [US2] `frontend/src/api/absences.ts`
- [X] T094 [US2] `AbsencesPage.tsx` in `frontend/src/features/absences/` — own list plus a request dialog showing live remaining balance via the preview endpoint
- [X] T095 [US2] `AbsenceApprovalsPage.tsx` in `frontend/src/features/absences/` — pending queue, approve, reject with reason
- [X] T096 [US2] Add routes and nav entries
- [X] T096a [US2] **Carried over from Phase 4**: add an approved-absence parameter to
  `ComplianceEvaluator.Evaluate` and to `TimesheetBreachEvaluation`, so approved absence days
  contribute nothing to the daily, weekly, or averaged limits (FR-057). Add the test that Phase 4
  could not write, since `AbsenceRequest` did not exist yet

**Checkpoint**: US1 + US6 + US2 work. Run the Phase 3 section of quickstart.md. Shippable.

### Phase 5 deviations and notes

1. **`WorkingDayCalculator` was created now rather than in Phase 6**, with a Monday-to-Friday
   assumption and no holiday awareness. Phase 6 (T103) extends it with a working-day pattern and
   the non-working-day list instead of replacing it, so **T112's "retrofit" is now just passing
   more arguments** — the call sites in `RequestAbsenceCommandHandler` and
   `PreviewAbsenceQueryHandler` do not move.
2. **T096a is done, and it caught a real bug.** `AbsenceRequest.CoversDate` returns true for
   Pending *and* Approved (correct for overlap checks), so the first version of the compliance
   exclusion let a **pending** absence excuse a breach. `ComplianceEvaluator` now re-checks
   `Status == Approved` itself rather than trusting the caller to have filtered.
   `A_pending_absence_does_not_excuse_anything` guards it.
3. **The absence exclusion spans the whole averaging window**, not just the month — a preceding
   day covered by approved leave must drop out of the rest and average checks too.
4. **`IAbsenceRepository` covers requests and entitlements together.** They change in one
   operation on approval and cancellation, so splitting them would mean a two-repository dance
   with no transaction boundary between them.
5. **A vacation request with no entitlement row for the year is refused** with a message pointing
   at the administrator, rather than treated as zero days available. Other absence types are
   unaffected, since they do not draw on the allowance.
6. **Entitlement is drawn down on approval, not on request**, so a pending request does not hold
   days hostage. Cancelling an approved vacation returns them; cancelling a pending one has
   nothing to return.
7. **`Release` floors at zero** rather than allowing negative used-days, so a double cancellation
   or a manually adjusted allowance cannot produce a nonsensical balance.
8. **The query-filter set assertion had to be widened deliberately** to include `AbsenceRequest`
   and `AbsenceEntitlement`. That test failing on each new user-owned entity is the intended
   behaviour — it forces the visibility decision to be explicit.

---

## Phase 6: User Story 3 — Employment terms and company calendar (Priority: P3)

**Goal**: Versioned employment terms and a manual holiday calendar, making target hours computable
and correcting absence day counts.

**Independent Test**: Enter 38.5 hours Mon–Fri effective 1 January plus the year's holidays, and
read back the correct expected working days and target hours for March.

### Tests for User Story 3

- [X] T097 [P] [US3] `EmploymentTerms` versioning tests in `tests/Proposly.Domain.Tests/WorkTimeManagement/EmploymentTermsTests.cs` — successor closes predecessor, and **negative**: history is immutable, `ValidFrom` must advance
- [X] T098 [P] [US3] `WorkingDayCalculator` tests in `.../WorkingDayCalculatorTests.cs` — weekends and holidays excluded, half days, holiday on a non-working weekday has no effect, null target when no terms, closure day stays a working day
- [X] T099 [P] [US3] Handler test asserting a non-working day cannot be created twice on one date

### Implementation for User Story 3

- [X] T100 [P] [US3] `WeekDays` flags enum and `NonWorkingDayKind` enum in `src/Proposly.Domain/WorkTimeManagement/Enums/`
- [X] T101 [P] [US3] `EmploymentTerms` entity in `.../Entities/EmploymentTerms.cs` — no mutators beyond `CloseAt`
- [X] T102 [P] [US3] `NonWorkingDay` entity in `.../Entities/NonWorkingDay.cs` — `Kind`, `ConsumesVacation`, `Source`; holidays never consume, closures consume by default
- [X] T103 [US3] `WorkingDayCalculator` pure domain service in `.../Services/WorkingDayCalculator.cs`
- [X] T104 [P] [US3] `IEmploymentTermsRepository` and `INonWorkingDayRepository` in `.../Repositories/`
- [X] T105 [P] [US3] `EmploymentTermsConfiguration` and `NonWorkingDayConfiguration` in `src/Proposly.Infrastructure/Persistence/Configurations/`, unique index `(CompanyId, Date)` on non-working days
- [X] T106 [US3] Add both `DbSet`s to `AppDbContext`, implement both repositories, register them `Scoped`
- [X] T107 [US3] Generate and commit migration `AddWorkTimeTermsAndCalendar`
- [X] T108 [P] [US3] `CreateEmploymentTermsCommand` + handler + **validator** in `.../Commands/CreateEmploymentTerms/`
- [X] T109 [P] [US3] `CreateNonWorkingDayCommand`, `UpdateNonWorkingDayCommand` + handlers + **validators**, and `DeleteNonWorkingDayCommand` + handler (id-only) in `.../Commands/`
- [X] T110 [P] [US3] `WorkTimeSettingsResponses.cs` in `.../Responses/` per contracts/worktime-settings.md
- [X] T111 [P] [US3] `GetEmploymentTermsQuery`, `GetMyEmploymentTermsQuery`, `GetTargetHoursQuery`, `GetNonWorkingDaysQuery` + handlers in `.../Queries/`
- [X] T112 [US3] Retrofit `PreviewAbsenceQueryHandler` and `RequestAbsenceCommandHandler` to use `WorkingDayCalculator` with real terms and holidays instead of the Phase 5 placeholder
- [X] T113 [US3] Add terms and non-working-day routes to `WorkTimeSettingsController`, with `GET terms/mine` and `GET non-working-days` at action-level `RecordOwnWorkTime`
- [X] T114 [US3] Guard `UpdateNonWorkingDayCommand` and `DeleteNonWorkingDayCommand` against dates inside a month already approved for any employee — flag the month for review instead (FR-032, FR-071)
- [X] T115 [US3] `EmploymentTermsPage.tsx` in `frontend/src/features/settings/` — version history plus new-version form
- [X] T116 [US3] `HolidayCalendarPage.tsx` in `frontend/src/features/settings/` — manual entry, holiday vs closure day. **No import button (deferred)**
- [X] T117 [US3] Render non-working days and the viewer's own approved absences as **read-only markers** in the existing calendar components under `frontend/src/features/calendar/`. Create no `Termin` rows (FR-074)
- [X] T118 [US3] Add routes and nav entries

**Checkpoint**: US1 + US6 + US2 + US3 work. Run the Phase 4 section of quickstart.md, skipping the
deferred import scenarios 8–12. Shippable.

### Phase 6 deviations and notes

1. **One chargeability rule serves both calculations.** A day counts when it is in the employee's
   working pattern *and* is not a non-working day that costs them nothing. So a public holiday
   neither consumes vacation nor adds to the target, while a company closure does both — the
   employee is expected to cover it, and covers it with entitlement. Absence counting and target
   hours cannot drift apart, because they ask the same question.
2. **Terms are resolved per day, not per month.** `TargetHoursForMonth` picks the version in force
   on each date, so a contract change taking effect mid-month produces a genuine blend rather than
   whichever version happened to start the month. Tested.
3. **T112 was not a retrofit.** Because `WorkingDayCalculator` was built in Phase 5 with optional
   parameters, the call sites in `RequestAbsenceCommandHandler` and `PreviewAbsenceQueryHandler`
   only gained arguments. A new `WorkCalendarContext` helper loads the pattern and calendar once so
   the request and its preview cannot disagree.
4. **Daily hours divide by the pattern, not by five.** A 32-hour four-day week is 8-hour days, not
   6.4 — which is what makes a four-day employee's target correct.
5. **Deleting a holiday is refused when an approved absence covers it** (beyond the approved-month
   guard). Removing it would make the day chargeable again and leave the absence short, so rather
   than silently adjusting someone's entitlement the deletion is blocked and the administrator is
   pointed at the affected requests. Needed a new repository method,
   `GetApprovedCoveringDateAsync` — the existing per-user one would have matched nothing.
6. **`NonWorkingDay` and `WorkTimePolicy` are deliberately NOT user-owned.** They are company
   reference data that shapes everyone's figures; scoping them per user would hide the rules from
   the people they apply to. A new test asserts their filters mention `CompanyId` and never
   `UserId`.
7. **A public holiday that consumes vacation is refused at the domain level**, not merely
   defaulted — it would be wrong in every jurisdiction this module targets.
8. **Calendar markers are read-only and private.** Non-working days plus *the viewer's own*
   approved absences render as markers in the existing month grid. Colleagues' absences are not
   shown, and no `Termin` row is created.
9. **Encoding note**: `tasks.md` had a stray `?` byte at offset 0, left by the Phase 5 PowerShell
   repair — a UTF-8 BOM has no Windows-1252 mapping, so it round-tripped to `?`. Stripped; the
   frontmatter now parses again.

---

## Phase 7: User Story 4 — Month-end working time report (Priority: P4)

**Goal**: Target vs actual with a capped running balance, exportable as a PDF, plus a company
overview.

**Independent Test**: For 38.5 weekly hours, 21 expected working days, 2 approved vacation days and
160 recorded hours, the report shows the correct target, actual, difference and closing balance,
and exports.

### Tests for User Story 4

- [X] T119 [P] [US4] `BalanceCalculator` tests in `tests/Proposly.Domain.Tests/WorkTimeManagement/BalanceCalculatorTests.cs` — surplus held at the cap with forfeiture reported, deficit past the floor reported but **not** clamped, null cap and floor unbounded, and December to January carry with no reset
- [X] T120 [P] [US4] Snapshot test asserting an approved month's figures do not change when terms, policy, or holidays change afterwards (FR-060, SC-006)

### Implementation for User Story 4

- [X] T121 [US4] `BalanceCalculator` pure domain service in `src/Proposly.Domain/WorkTimeManagement/Services/BalanceCalculator.cs` returning closing balance, forfeited hours, and a deficit-floor-breached flag
- [X] T122 [US4] Add snapshot fields to `Timesheet` — `TargetHoursSnapshot`, `ActualHoursSnapshot`, `OpeningBalanceHours`, `ClosingBalanceHours`, `ForfeitedHours`, `IsRevised`, `IsSelfApproved` — written by `ApplySnapshot` from the approve path only
- [X] T123 [US4] Update `TimesheetConfiguration` for the new columns; generate and commit migration `AddWorkTimeReportSnapshots`
- [X] T124 [US4] Compute and persist the snapshot inside `ApproveTimesheetCommandHandler`, reading the prior month's closing balance as the opening balance
- [X] T125 [P] [US4] `IWorkTimeReportPdfService` in `src/Proposly.Application/WorkTimeManagement/Services/IWorkTimeReportPdfService.cs`
- [X] T126 [US4] `WorkTimeReportPdfService` in `src/Proposly.Infrastructure/Services/Pdf/WorkTimeReportPdfService.cs` using QuestPDF, modelled on `QuarterlyReportPdfService`; content per FR-029, provisional months watermarked. Register as `Singleton`
- [X] T127 [P] [US4] `WorkTimeReportResponses.cs` in `.../Responses/` per contracts/worktime-reports.md
- [X] T128 [P] [US4] `GetMonthlyWorkTimeReportQuery` + handler in `.../Queries/GetMonthlyWorkTimeReport/` — snapshot for Approved and Locked, live and marked provisional otherwise
- [X] T129 [P] [US4] `GetMonthlyWorkTimeReportPdfQuery` and `GetCompanyMonthOverviewQuery` + handlers in `.../Queries/`
- [X] T130 [US4] Mark a locked month revised when an absence, holiday, or terms change affects it (FR-032)
- [X] T131 [US4] `WorkTimeReportsController` in `src/Proposly.API/Controllers/` per contracts/worktime-reports.md
- [X] T132 [P] [US4] `frontend/src/api/worktime-reports.ts`
- [X] T133 [US4] `MonthlyReportPage.tsx` in `frontend/src/features/worktime/` — figures, absence breakdown, breaches, forfeiture shown explicitly, approaching-cap warning, PDF download
- [X] T134 [US4] Extend `CompanyOverviewPage.tsx` with the company month figures
- [X] T135 [US4] Add routes and nav entries

**Checkpoint**: Five stories work. Run the Phase 5 section of quickstart.md. Shippable.

---

## Phase 8: User Story 5 — Reconcile against project bookings (Priority: P5)

**Goal**: Show recorded working hours against hours booked to projects, read-only.

**Independent Test**: 160 recorded hours against 141 booked reports 19 unbooked.

### Tests for User Story 5

- [X] T136 [P] [US5] Handler tests in `tests/Proposly.Application.Tests/WorkTimeManagement/ReconciliationTests.cs` — hours summed across multiple `ProjectMember` rows for one user, over-booking flagged not errored, unresolvable memberships reported as unattributed
- [X] T137 [P] [US5] **Regression test** asserting project labour cost and profitability are unchanged and no `TimeEntry` row is written (FR-034, FR-035, SC-009)

### Implementation for User Story 5

- [X] T138 [US5] `GetReconciliationQuery` + handler in `src/Proposly.Application/WorkTimeManagement/Queries/GetReconciliation/` — joins `TimeEntry.MemberId` → `ProjectMember.Id` → `ProjectMember.UserId`, strictly read-only (research.md Decision 8)
- [X] T139 [US5] Add `ReconciliationResponse` to `WorkTimeReportResponses.cs` and the route to `WorkTimeReportsController`
- [X] T140 [US5] `ReconciliationPanel.tsx` in `frontend/src/features/worktime/`, surfaced on the monthly report for Owner and Admin

**Checkpoint**: All six stories work. Run the Phase 6 section of quickstart.md.

### Phase 7 and 8 deviations and notes

1. **`MonthEndFigures` is the single arbiter of a month's numbers.** It decides snapshot vs live
   and is used by both the approval path (to create the snapshot) and every read path (to display
   it), so what an approver signed off is exactly what the report and the PDF show.
2. **The deficit floor reports; the cap forfeits.** Deliberately asymmetric. Clamping a deficit
   would quietly forgive hours the employee still owes, so it is flagged instead. Surplus above the
   cap really is lost, so it is held at the cap and the forfeited figure is stated everywhere —
   report, overview, and PDF.
3. **A month with no employment terms moves the balance by nothing**, rather than by a fictitious
   full-month deficit. `MonthlyDifference` comes back null and the UI says "unavailable".
4. **Revision is marked at the point of change**, not detected on read. `ApproveAbsence` and
   `CancelAbsence` call `RevisedMonthMarker`, which flags any already-reported month the absence
   touches. The reported figures never move; reopening stays an approver's decision.
5. **Reopening clears the snapshot as well as the breaches.** Stale figures would be worse than
   none — the month is recomputed live until it is approved again.
6. **Project bookings are read through a new `IProjectBookingReader` seam**, not through
   `IProjectRepository`. This module must never write project data, and `ProjectManagement` should
   not grow an interface for another context's benefit. The implementation walks
   `TimeEntry → ProjectMember → User` and sums across every membership one person holds.
7. **`UnbookedHours` is never negative.** An over-booked month is reported through the `OverBooked`
   flag instead, since a negative "unbooked" figure would read as nonsense.
8. **A test caught a sloppy expectation, not a bug**: the approaching-cap warning fires within
   5 hours of the cap, and 74 against an 80 cap is 6 away. Expectations corrected and the boundary
   case (75) added.
9. **The PDF watermarks a provisional month**, so an unapproved draft cannot be passed off as a
   final record.

---

## Phase 9: Polish & Cross-Cutting Concerns

- [X] T141 [P] Confirm no file in the module calls `IgnoreQueryFilters()` — `grep -rn IgnoreQueryFilters src/` and check every hit predates this feature
- [X] T142 [P] Confirm every command carrying a payload has a sibling validator; list any id-only commands deliberately without one
- [X] T143 Run the full cross-cutting checklist at the end of [quickstart.md](./quickstart.md)
- [X] T144 [P] Update `CLAUDE.md` — 6 domain contexts, 13 application modules, the new `IUserOwnedEntity` rule, and the four new policies
- [X] T145 [P] Update `.specify/memory/constitution.md` if the per-employee filter should become a stated principle for future modules
- [X] T146 Verify `dotnet build`, `dotnet test`, `npm run build`, and `npm run lint` are all clean
- [X] T147 Confirm the container still builds — no new project was added, so the `Dockerfile` and `Proposly.slnx` must be unchanged
- [X] T148 **Pre-existing test debt, found during Phase 2 — not caused by this feature.** 8 tests in `Proposly.Application.Tests` fail on `main`, from two earlier features that landed without updating their mocks: (a) 6 tests in `ProjectManagement/UpdateTaskStatusCommandHandlerTests.cs` and `ProjectManagement/AddTaskCommentCommandHandlerTests.cs` stub `GetByIdAsync` while the handlers now call `GetByIdForWriteAsync`, so the stub returns null; (b) 2 tests in `Auth/LoginCommandHandlerTests.cs` never mark the user email-verified, which login now requires. Fix the mocks so the suite is green, since the constitution's definition of done includes `dotnet test` passing

---

## Phase 10: Employment type and contract overtime arrangements (FR-075 to FR-081)

Added 2026-09-12, after the feature shipped. An increment on top of Phase 6's employment terms,
not a rework of it: three columns on `EmploymentTerms`, two snapshot columns on `Timesheet`, and
one rewritten domain calculation. No existing table or endpoint changed shape.

- [X] T149 `Domain/WorkTimeManagement/Enums/EmploymentType.cs` — `FullTime`, `PartTime`, `MarginalEmployment`, `Apprentice`, `Other`
- [X] T150 `EmploymentTerms` — add `EmploymentType`, `IsAllIn`, `OvertimeLumpSumHours`; guard in `Create` that all-in and a lump sum cannot coexist (FR-078)
- [X] T151 Rewrite `BalanceCalculator` to return a `BalanceResult` record: opening, monthly difference, absorbed by lump sum, covered by all-in, carried forward, closing, forfeited, floor breached (FR-079, FR-080)
- [X] T152 `Timesheet` — snapshot fields `AbsorbedByLumpSumHours` and `CoveredByAllInHours`; `ApplySnapshot` takes both, `Reopen` clears them (FR-081)
- [X] T153 [P] `EmploymentTermsConfiguration` and `TimesheetConfiguration` — the five new columns, `EmploymentType` stored as a string with a `FullTime` default so existing rows stay valid
- [X] T154 `MonthEndFigures.ContractForMonthAsync` — resolve `(IsAllIn, LumpSum)` from the terms version in force on the **last day** of the reported month (FR-081)
- [X] T155 [P] Extend `CreateEmploymentTermsCommand` + validator (mutual exclusion, lump sum 0–200 h) and `MonthlyWorkTimeReportResponse` with the five new figures
- [X] T156 [P] `ContractCompensationTests` and `EmploymentTypeTests` — absorption order, all-in coverage, neither offsetting a shortfall, and every hour reconciling
- [X] T157 `dotnet ef migrations add AddEmploymentTypeAndContractTerms` — verify it is five `AddColumn`s and nothing else
- [X] T158 [P] Frontend `EmploymentTermsPage` — type dropdown that *suggests* hours (FR-075), all-in checkbox, Überstundenpauschale input, the two mutually exclusive in the form; type and overtime columns in the history table
- [X] T159 [P] Frontend `MonthlyReportPage` — absorbed / covered / carried rows in the flexitime balance, with a note explaining why a surplus did not bank
- [X] T160 [P] `WorkTimeReportPdfService` — the same three lines and the same explanatory note in the exported document (FR-080)

### Phase 10 decisions

- **The type suggests, never enforces.** `SUGGESTED_WEEKLY_HOURS` pre-fills the form and the field
  stays freely editable. A part-time employee on 32 hours and one on 12 are both just `PartTime`.
- **All-in surplus is recorded and reported, never banked.** Refusing to record it would falsify
  the statutory record; banking it would pay twice.
- **The lump sum absorbs first, all-in covers the remainder, then the cap applies.** Order matters
  and is asserted by test.
- **Neither offsets a shortfall.** An employee below target still carries the deficit — overtime
  compensation is not a credit line.

---

## Dependencies & Execution Order

### Phase dependencies

- **Phase 1 Setup**: no dependencies
- **Phase 2 Foundational**: depends on Setup — **blocks every story**
- **Phase 3 (US1)**: depends on Phase 2
- **Phase 4 (US6)**: depends on Phase 3 — breach detection needs `Timesheet` and its day entries
- **Phase 5 (US2)**: depends on Phase 2; independent of US1 and US6 in principle, but T112 in Phase 6 revisits its day counting
- **Phase 6 (US3)**: depends on Phase 5 for the retrofit in T112
- **Phase 7 (US4)**: depends on Phases 3, 5 and 6 — needs recorded hours, absences and terms for a correct target
- **Phase 8 (US5)**: depends on Phase 3 only
- **Phase 9 Polish**: depends on the stories you choose to ship

### Story dependency notes

This feature is more layered than the template's ideal of fully independent stories. US2 ships in
Phase 5 with simplified day counting (weekends only) and gains holiday and contract awareness in
Phase 6 via T112. That is deliberate: it keeps Phase 5 shippable rather than blocking absence
behind the whole calendar.

### Parallel opportunities

- Phase 1: T001–T004 all parallel
- Phase 2: T014–T016 parallel after T013; T005–T007 sequential into T008
- Within each story: entities, configurations, commands, queries and response DTOs marked [P] are
  separate files and safe to run together
- Frontend api clients (T043, T069, T093, T132) are parallel with their backend phase once the
  contract is settled

**Not parallel, ever**: migrations (T030, T061, T083, T107, T123) are stateful and ordered, and the
shared-file edits in Phase 2 (T008, T012, T013) plus `AppDbContext` `DbSet` additions must be done
one at a time to avoid conflicting edits.

---

## Implementation Strategy

### MVP (Phases 1–3)

1. Setup, then Foundational — **stop and confirm `dotnet test` is green**, especially T014
2. User Story 1
3. Validate with the Phase 1 quickstart section including both visibility checks
4. Deploy — a working statutory working-time record

### Incremental delivery

Ship after every checkpoint: US1 → +US6 → +US2 → +US3 → +US4 → +US5. Each adds value without
breaking what shipped before.

### Notes

- Commit after each task or logical group; never edit a committed migration
- The riskiest task in the feature is **T008**. Do it deliberately, and do not proceed past T014
  and T015 until they pass
- `TimeProvider` is injected in new code only — the 53 existing `DateTime.UtcNow` call sites stay
  as they are (Principle I)
- Holiday import stays unbuilt. If it is picked up later it needs a constitution amendment first
