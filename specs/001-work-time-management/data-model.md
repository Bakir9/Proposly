# Phase 1 Data Model: Work Time Management

**Date**: 2026-09-10 | **Plan**: [plan.md](./plan.md) | **Research**: [research.md](./research.md)

Namespace: `Proposly.Domain.WorkTimeManagement`. Eight entities across four aggregates, plus three
pure domain services. All types live in `Proposly.Domain`, which keeps zero package references.

## Marker interfaces

| Entity | `ITenantEntity` | `IAuditableEntity` | `IUserOwnedEntity` |
|---|:--:|:--:|:--:|
| `Timesheet` | ✔ | ✔ | ✔ |
| `WorkDayEntry` | — child of Timesheet — | | |
| `TimesheetBreach` | — child of Timesheet — | | |
| `AbsenceRequest` | ✔ | ✔ | ✔ |
| `AbsenceEntitlement` | ✔ | ✔ | ✔ |
| `EmploymentTerms` | ✔ | ✔ | ✔ |
| `NonWorkingDay` | ✔ | ✔ | — company-wide |
| `WorkTimePolicy` | ✔ | ✔ | — company-wide |
| `BreakRule` | — child of WorkTimePolicy — | | |

`NonWorkingDay` and `WorkTimePolicy` are deliberately **not** user-owned: they are company reference
data every employee must be able to read (FR-013, FR-025 depend on it).

### New shared interface

```csharp
// Proposly.Shared/Interfaces/IUserOwnedEntity.cs
public interface IUserOwnedEntity
{
    Guid UserId { get; }
}
```

Applied in `AppDbContext.OnModelCreating` as a composed predicate — see
[research.md Decision 2](./research.md). Effective filter for a user-owned entity:

```text
e.CompanyId == currentUser.CompanyId
&& (e.UserId == currentUser.UserId || currentUser.CanViewAllEmployees)
```

Entities implementing only `ITenantEntity` must keep a byte-identical predicate to today's. A
regression test asserts this.

---

## Aggregate 1 — `Timesheet`

One employee's working time for one calendar month. Bounded to ≤31 day entries, so the whole
aggregate loads safely — no `GetByIdForWriteAsync` split is needed.

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `CompanyId` | `Guid` | tenant key |
| `UserId` | `Guid` | owning employee |
| `Year` | `int` | |
| `Month` | `int` | 1–12 |
| `Status` | `TimesheetStatus` | see state machine |
| `SubmittedAt` | `DateTime?` | |
| `ApprovedById` / `ApprovedAt` | `Guid?` / `DateTime?` | |
| `LockedAt` | `DateTime?` | period close |
| `ReopenedById` / `ReopenedAt` | `Guid?` / `DateTime?` | FR-007 audit |
| `IsRevised` | `bool` | FR-032 — set when a locked month's inputs change |
| `IsSelfApproved` | `bool` | Owner approving their own month; surfaced on the report |
| `TargetHoursSnapshot` | `decimal?` | frozen at approval |
| `ActualHoursSnapshot` | `decimal?` | frozen at approval |
| `OpeningBalanceHours` | `decimal?` | frozen at approval |
| `ClosingBalanceHours` | `decimal?` | frozen at approval |
| `ForfeitedHours` | `decimal?` | FR-045 — never silently discarded |
| `_days` | `List<WorkDayEntry>` | private backing collection |
| `_breaches` | `List<TimesheetBreach>` | written at approval only |

Unique index: `(CompanyId, UserId, Year, Month)`.

**Computed**: `TotalWorkedHours` = sum of `_days[].WorkedHours`.

### Behaviour

```csharp
static Timesheet Create(Guid companyId, Guid userId, int year, int month)
WorkDayEntry AddOrUpdateDay(DateOnly date, TimeOnly start, TimeOnly end, int breakMinutes, string? note)
void RemoveDay(DateOnly date)
void Submit()
void Approve(Guid approverId, IReadOnlyCollection<TimesheetBreach> breaches, bool breachesAcknowledged)
void ReturnForCorrection(Guid approverId)
void Lock()
void Reopen(Guid approverId)
void ApplySnapshot(decimal target, decimal actual, decimal opening, decimal closing, decimal forfeited)
void MarkRevised()
```

### Invariants

| Rule | Enforced by | Requirement |
|---|---|---|
| Day entries mutate only in `Draft` | `AddOrUpdateDay`, `RemoveDay` throw otherwise | FR-005, FR-006 |
| `date` must fall inside `Year`/`Month` | `AddOrUpdateDay` | FR-003 |
| `end` must be after `start`, unless the shift crosses midnight | `WorkDayEntry.Create` | FR-002 |
| `breakMinutes` must be less than the start→end span | `WorkDayEntry.Create` | FR-002 |
| Gaps are allowed; days need not be contiguous | no invariant — by design | FR-042 |
| Past dates within the open month are accepted | no invariant — by design | FR-041 |
| A breach never blocks a save | `AddOrUpdateDay` does not consult the policy | **FR-051** |
| Approving with breaches requires acknowledgement | `Approve` throws when breaches exist and `breachesAcknowledged` is false | FR-059 |
| Snapshots are written once, at approval | `ApplySnapshot` callable only from `Approve` path | FR-060 |

### State machine

```text
                 Submit()                Approve(...)            Lock()
   ┌────────┐ ───────────────▶ ┌───────────┐ ──────────▶ ┌──────────┐ ─────────▶ ┌────────┐
   │ Draft  │                  │ Submitted │             │ Approved │            │ Locked │
   └────────┘ ◀─────────────── └───────────┘             └──────────┘            └────────┘
        ▲       ReturnForCorrection()                          │                      │
        │                                                      │                      │
        └──────────────────────── Reopen() ───────────────────┴──────────────────────┘

   MarkRevised() : Locked → Approved  (IsRevised = true, awaits re-acknowledgement)
```

Editable only in `Draft`. Every transition records actor and timestamp (FR-040).

---

## Child — `WorkDayEntry`

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `TimesheetId` | `Guid` | |
| `Date` | `DateOnly` | unique within the timesheet |
| `StartTime` | `TimeOnly` | |
| `EndTime` | `TimeOnly` | |
| `BreakMinutes` | `int` | ≥ 0 |
| `Note` | `string?` | max 500 |
| `WorkedHours` | `decimal` | computed and stored |

`WorkedHours` = `(EndTime − StartTime) − BreakMinutes`, adding 24h when `EndTime <= StartTime` so a
midnight-crossing shift resolves correctly and stays attributed to `Date` (spec edge case).

`DateOnly`/`TimeOnly` follow the existing precedent — `TimeEntry.Date`, `Expense.Date`, and
`Milestone.DueDate` are all `DateOnly`.

## Child — `TimesheetBreach`

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | |
| `TimesheetId` | `Guid` | |
| `Kind` | `BreachKind` | |
| `Date` | `DateOnly?` | set for day-scoped kinds |
| `WeekStartDate` | `DateOnly?` | set for week-scoped kinds |
| `LimitValue` | `decimal` | the limit that applied |
| `ActualValue` | `decimal` | what was recorded |
| `AcknowledgedById` / `AcknowledgedAt` | `Guid?` / `DateTime?` | FR-059 |

Rows exist only for approved months. Open months compute breaches on read (research.md Decision 3).

---

## Aggregate 2 — `AbsenceRequest`

| Field | Type | Notes |
|---|---|---|
| `Id`, `CompanyId`, `UserId` | `Guid` | |
| `Type` | `AbsenceType` | |
| `StartDate` / `EndDate` | `DateOnly` | `End >= Start` |
| `FirstDayIsHalf` / `LastDayIsHalf` | `bool` | FR-010 |
| `ConsumedDays` | `decimal` | snapshotted at creation; `0.5` steps |
| `Status` | `AbsenceStatus` | |
| `Reason` | `string?` | max 500, **non-medical** |
| `ApproverId` / `DecidedAt` / `DecisionReason` | `Guid?` / `DateTime?` / `string?` | |

**No field for a diagnosis or medical detail exists on this entity** — FR-019 is enforced by the
absence of the field, not by validation. SC-008 verifies it by field review.

```csharp
static AbsenceRequest Create(Guid companyId, Guid userId, AbsenceType type,
                             DateOnly start, DateOnly end,
                             bool firstHalf, bool lastHalf,
                             decimal consumedDays, string? reason)
void Approve(Guid approverId)
void Reject(Guid approverId, string reason)
void Cancel()
```

| Rule | Requirement |
|---|---|
| Only `Pending` may be approved or rejected | FR-012 |
| Only `Pending` or a future-dated `Approved` may be cancelled | Story 2 sc. 4 |
| `ConsumedDays` is computed by `WorkingDayCalculator` and frozen on the row | FR-013 |
| Overlap with an existing Pending/Approved request is refused | FR-014 (handler-level, needs a repository query) |
| Vacation exceeding remaining entitlement is refused | FR-016 (handler-level) |

**State machine**: `Pending → Approved | Rejected | Cancelled`; `Approved → Cancelled` while the
start date is in the future. Terminal otherwise.

## Aggregate 3 — `AbsenceEntitlement`

| Field | Type | Notes |
|---|---|---|
| `Id`, `CompanyId`, `UserId` | `Guid` | |
| `Year` | `int` | |
| `EntitledDays` | `decimal` | from `EmploymentTerms.AnnualVacationDays` at year setup |
| `CarriedOverDays` | `decimal` | admin-adjustable; no automatic expiry |
| `UsedDays` | `decimal` | |

Unique index: `(CompanyId, UserId, Year)`. **Computed**: `RemainingDays` =
`EntitledDays + CarriedOverDays − UsedDays`.

```csharp
void Consume(decimal days)   // throws if it would exceed remaining — FR-016
void Release(decimal days)   // on cancel or reject — FR-017
void SetEntitlement(decimal entitled, decimal carriedOver)
```

Only vacation touches this. Sick, unpaid, parental, and special leave never call `Consume`
(FR-018).

---

## Aggregate 4 — `WorkTimePolicy`

One versioned row set per company, consolidating the spec's rule set and flexitime settings
(research.md Decision 4).

| Field | Type | Notes |
|---|---|---|
| `Id`, `CompanyId` | `Guid` | |
| `ValidFrom` | `DateOnly` | version key |
| `ValidTo` | `DateOnly?` | closed when a successor is added |
| `Jurisdiction` | `string` | `"AT"`, `"DE"`, … seeds defaults |
| `HolidayRegionCode` | `string?` | FR-065; consumed by the deferred import |
| `MaxHoursPerDay` | `decimal` | FR-052 |
| `MaxHoursPerWeek` | `decimal` | FR-055 |
| `AveragingWindowWeeks` | `int` | FR-056 |
| `MaxAverageHoursPerWeek` | `decimal` | FR-056 |
| `MinDailyRestHours` | `decimal` | FR-054 |
| `MinWeeklyRestHours` | `decimal` | FR-049 |
| `SurplusCapHours` | `decimal?` | null = unbounded, reported as such |
| `DeficitFloorHours` | `decimal?` | null = unbounded |
| `_breakRules` | `List<BreakRule>` | tiered |

**Child `BreakRule`**: `{ Id, WorkTimePolicyId, AboveHours, MinBreakMinutes }` — ordered by
`AboveHours`; the highest matching tier applies (research.md Decision 5).

Seeded defaults, both fully editable (FR-050):

| | AT | DE |
|---|---|---|
| Max hours/day | 12 | 10 |
| Max hours/week | 60 | 60 |
| Averaging window | 17 weeks @ 48h | 24 weeks @ 48h |
| Break tiers | >6h → 30 min | >6h → 30 min; >9h → 45 min |
| Min daily rest | 11h | 11h |
| Min weekly rest | 36h | 35h |
| Surplus cap / deficit floor | +80 / −20 | +80 / −20 |

Where no policy exists for a company, the module reports "no rule set active" and prompts an owner
rather than guessing (FR-064).

## `EmploymentTerms`

| Field | Type | Notes |
|---|---|---|
| `Id`, `CompanyId`, `UserId` | `Guid` | |
| `ValidFrom` | `DateOnly` | |
| `ValidTo` | `DateOnly?` | closed on successor insert |
| `WeeklyHours` | `decimal` | e.g. `38.5` |
| `WorkingDays` | `WeekDays` (flags) | stored as `int` |
| `AnnualVacationDays` | `decimal` | |

No mutating methods beyond `CloseAt(DateOnly)` — history is immutable (research.md Decision 6,
FR-022). Where an employee has no terms covering a period, target hours are reported unavailable,
not zero (FR-026).

## `NonWorkingDay`

| Field | Type | Notes |
|---|---|---|
| `Id`, `CompanyId` | `Guid` | |
| `Date` | `DateOnly` | |
| `Name` | `string` | max 200 |
| `Kind` | `NonWorkingDayKind` | `PublicHoliday` \| `CompanyClosure` |
| `ConsumesVacation` | `bool` | false for holidays; true by default for closures — FR-073 |
| `Source` | `EntrySource` | `Manual` \| `Imported`; always `Manual` in this build |

Unique index `(CompanyId, Date)` — one non-working day per date, which also delivers the spec's
"counted once, not twice" edge case for a closure overlapping a holiday.

---

## Enums

```csharp
enum TimesheetStatus   { Draft, Submitted, Approved, Locked }
enum AbsenceType       { Vacation, SickLeave, UnpaidLeave, ParentalLeave, SpecialLeave }
enum AbsenceStatus     { Pending, Approved, Rejected, Cancelled }
enum NonWorkingDayKind { PublicHoliday, CompanyClosure }
enum EntrySource       { Manual, Imported }
enum BreachKind        { DailyMaximum, WeeklyMaximum, AveragingWindow,
                         InsufficientBreak, InsufficientDailyRest }
[Flags] enum WeekDays  { None = 0, Monday = 1, Tuesday = 2, Wednesday = 4,
                         Thursday = 8, Friday = 16, Saturday = 32, Sunday = 64 }
```

All serialise as strings via the globally registered `JsonStringEnumConverter`.

## Domain events

`TimesheetSubmittedDomainEvent`, `TimesheetApprovedDomainEvent`,
`TimesheetReturnedForCorrectionDomainEvent`, `AbsenceRequestedDomainEvent`,
`AbsenceApprovedDomainEvent`, `AbsenceRejectedDomainEvent`.

Raised via `AggregateRoot`; dispatched by `AppDbContext.SaveChangesAsync` after the save, and
handled by `IDomainEventHandler<T>` implementations in
`Application/WorkTimeManagement/EventHandlers/` that create existing `Notification` rows — the same
shape as the `CalendarManagement` handlers.

---

## Domain services (pure, no I/O)

Following the `VatCalculator` precedent in `Domain/OfferManagement/Services/`.

### `WorkingDayCalculator`

```csharp
int WorkingDaysInRange(DateOnly from, DateOnly to, WeekDays pattern,
                       IReadOnlyCollection<NonWorkingDay> nonWorkingDays)

decimal ConsumedDaysForAbsence(DateOnly start, DateOnly end, bool firstHalf, bool lastHalf,
                               WeekDays pattern,
                               IReadOnlyCollection<NonWorkingDay> nonWorkingDays)

decimal? TargetHoursForMonth(int year, int month, EmploymentTerms? terms,
                             IReadOnlyCollection<NonWorkingDay> nonWorkingDays,
                             IReadOnlyCollection<AbsenceRequest> approvedAbsences)
```

Excludes non-working weekdays and public holidays; a company closure day that consumes vacation
stays a working day for target purposes, since the employee spends entitlement on it. Returns
`null` target when `terms` is null (FR-026).

### `ComplianceEvaluator`

```csharp
IReadOnlyList<TimesheetBreach> Evaluate(
    IReadOnlyCollection<WorkDayEntry> days,
    WorkTimePolicy policy,
    IReadOnlyCollection<WorkDayEntry> priorMonthTailDays,   // for cross-boundary rest checks
    IReadOnlyCollection<AbsenceRequest> approvedAbsences)
```

Detects all five `BreachKind` values. Approved absence days contribute nothing to any limit
(FR-057). `priorMonthTailDays` carries the last days of the preceding month so a rest breach
spanning a month boundary is detected in both months.

### `BalanceCalculator`

```csharp
readonly record struct BalanceResult(decimal ClosingBalance, decimal ForfeitedHours,
                                     bool DeficitFloorBreached);

BalanceResult Calculate(decimal openingBalance, decimal targetHours, decimal actualHours,
                        decimal? surplusCap, decimal? deficitFloor)
```

Surplus above the cap is held at the cap and returned as `ForfeitedHours` (FR-045). A deficit past
the floor is **not** clamped — it is reported via `DeficitFloorBreached` for the employer to act on
(FR-046). Null cap or floor means unbounded.

---

## Repositories

Interfaces in `Domain/WorkTimeManagement/Repositories/`, implementations in
`Infrastructure/Persistence/Repositories/`, registered `Scoped` in `AddInfrastructure`.

| Interface | Responsibility |
|---|---|
| `ITimesheetRepository` | timesheet by user+month, month across company, prior-month tail days, prior closing balance |
| `IAbsenceRepository` | requests and entitlements — overlap checks, pending queue, per-year balance |
| `IEmploymentTermsRepository` | version history per user, effective version for a date |
| `INonWorkingDayRepository` | by company and date range |
| `IWorkTimePolicyRepository` | effective version for a date, version history |

No repository in this module calls `IgnoreQueryFilters()` (research.md Decision 2).

## Migration

One additive migration, `AddWorkTimeManagement`, creating: `Timesheets`, `WorkDayEntries`,
`TimesheetBreaches`, `AbsenceRequests`, `AbsenceEntitlements`, `EmploymentTerms`,
`NonWorkingDays`, `WorkTimePolicies`, `BreakRules`. No existing table is altered. Applied
automatically at startup by the existing `MigrateAsync()` call.
