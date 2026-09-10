# Phase 0 Research: Work Time Management

**Date**: 2026-09-10 | **Plan**: [plan.md](./plan.md) | **Spec**: [spec.md](./spec.md)

The spec carried no `[NEEDS CLARIFICATION]` markers, so this phase resolved design ambiguities
found while mapping requirements onto the existing codebase rather than open product questions.
Nine decisions follow. Decisions 2 and 3 are the consequential ones.

---

## Decision 1 — `Locked` is a distinct state from `Approved`, not a synonym

**Decision**: `TimesheetStatus { Draft, Submitted, Approved, Locked }`. `Submit()` moves
Draft → Submitted. `Approve()` moves Submitted → Approved. `Lock()` moves Approved → Locked as an
explicit period close. Day entries are editable **only** in Draft, so Approved is already immutable
to the employee. `Reopen()` returns either Approved or Locked to Draft and records who did it.

**Rationale**: FR-004 names four states, and FR-007 specifically requires reopening a *Locked*
month — which only makes sense if Locked is reachable and distinct. Story 1 scenario 7 says
approval yields a month that accepts no further edits, which is satisfied by FR-005 restricting
edits to Draft; it does not require Approved and Locked to be the same value. Keeping them separate
gives a place for the payroll close that is distinct from the approver's sign-off, and gives
FR-032's "revised" flag somewhere sensible to sit: a Locked month that changes is marked revised
and returned to Approved pending re-acknowledgement.

**Alternatives considered**: Collapsing to three states with a boolean `IsLocked` — rejected
because FR-004 enumerates four and the boolean would duplicate the status. Making `Approve()` jump
straight to Locked — rejected because it leaves FR-007's "reopen a Locked month" indistinguishable
from reopening an approved one, and removes the payroll-close step.

---

## Decision 2 — Approver access lives inside the query filter, not behind `IgnoreQueryFilters()`

**Decision**: Add `bool CanViewAllEmployees { get; }` to `ICurrentUserService` (true for Owner,
Admin, and SuperAdmin). The filter applied to `IUserOwnedEntity` entities is:

```text
e.UserId == currentUser.UserId || currentUser.CanViewAllEmployees
```

No repository in this module calls `IgnoreQueryFilters()`.

**Rationale**: This is the most important decision in the plan. The obvious approach — filter to
the current user, then have approver repository methods call `IgnoreQueryFilters()` — is actively
dangerous here, because `IgnoreQueryFilters()` removes **all** filters on the query, including the
`ITenantEntity` company filter. An approver listing "all timesheets for March" would silently lose
company isolation and read other tenants' rows. That is precisely the failure Principle IV exists
to prevent, and it would be invisible in testing on a single-tenant dev database.

Expressing approver access as a disjunction inside the predicate means the company filter is never
dropped, there is no bypass path to guard, and no future query can forget the rule. It also reuses
the exact mechanism already in place: the existing filter closes over `_currentUserService` and
reads `CompanyId` per query, so reading a second property from the same service is the same
pattern, evaluated as a captured constant.

**Alternatives considered**:

- `IgnoreQueryFilters()` on approver methods — rejected as above; it trades a per-employee leak for
  a cross-tenant one.
- `IgnoreQueryFilters()` followed by re-applying `.Where(x => x.CompanyId == ...)` by hand —
  works, but reintroduces exactly the forgettable manual step the generic filter was chosen to
  eliminate.
- EF Core 10 **named query filters**, allowing two independent filters per entity with selective
  bypass — the cleanest fit in principle, and worth adopting if confirmed available and stable in
  EF Core 10.0.4. Not chosen as the baseline because the composed-predicate approach is
  version-independent and needs no verification before Phase 1 can proceed. **Action for Phase 1
  implementation**: verify named-filter support; if present, it is a drop-in improvement that
  removes the need to modify the existing single-filter code path at all, which would also retire
  the Complexity Tracking entry.
- Role checks in handlers rather than the data layer — rejected because handlers can be bypassed by
  a new query, and the spec (FR-038) requires enforcement wherever records are retrieved.

**Risk accepted**: `CanViewAllEmployees` is coarse — any Owner or Admin sees every employee's
records. That matches the spec's documented assumption that approvers are Owners and Admins with no
manager hierarchy, and matches the existing role model.

---

## Decision 3 — Compliance breaches are computed live while open, snapshotted at approval

**Decision**: `ComplianceEvaluator` is a pure domain service that computes breaches from a
timesheet's day entries, the effective `WorkTimePolicy`, and approved absences. While a month is
Draft or Submitted, breaches are computed on read and never persisted. At approval, the evaluated
set is written as `TimesheetBreach` child rows carrying the limit, the actual value, and the
acknowledging approver.

**Rationale**: FR-061 requires recalculation whenever a day changes while the month is open, which
argues for computing rather than storing. FR-060 requires an approved month to retain the breaches
acknowledged at approval, which argues for storing. Splitting on the approval boundary satisfies
both and is the same snapshot pattern the codebase already applies to `Project.OfferedAmount` and
`TimeEntry.HourlyRateSnapshot`: live while mutable, frozen once reported.

It also makes FR-062 fall out for free — a later policy change cannot alter an approved month's
breaches, because those rows already exist and are never recomputed.

**Alternatives considered**: Persisting breaches continuously and re-deriving on every day edit —
rejected as write amplification on the hottest path in the module, with a stale-row failure mode.
Never persisting and always recomputing — rejected because it violates FR-060 and FR-062: a policy
change would retroactively rewrite what an approver signed off on.

---

## Decision 4 — One versioned settings aggregate, not two

**Decision**: The spec's `Working Time Rule Set` and `Flexitime Agreement Settings` are implemented
as a single `WorkTimePolicy` aggregate per company, versioned by `ValidFrom`, owning a `BreakRule`
child collection and carrying the surplus cap, deficit floor, jurisdiction, and holiday region.

**Rationale**: Both derive from the same written document — an Austrian or German
Gleitzeitvereinbarung states the transferable surplus and deficit alongside the working-time
arrangement. Both are company-scoped, both are versioned by effective date, and both are changed in
the same administrative act. Two aggregates would mean two versioning mechanisms, two forward-only
rules (FR-048 and FR-062 are the same rule stated twice), and two admin screens for one
conversation with the customer.

The spec describes business concepts; consolidating them in the data model changes no requirement.

**Alternatives considered**: Separate aggregates as literally listed in Key Entities — rejected as
duplicated machinery. Putting the values on `Company` — rejected because it would edit an existing
entity in another context for this module's benefit and would provide no version history.

---

## Decision 5 — Break rules are data rows, not code branches

**Decision**: `BreakRule { AboveHours, MinBreakMinutes }` as an ordered child collection of
`WorkTimePolicy`. Austria seeds one rule (above 6h → 30 min); Germany seeds two (above 6h → 30 min,
above 9h → 45 min). `ComplianceEvaluator` selects the highest matching tier.

**Rationale**: FR-053 requires the tier that applies to be stated in the breach, and the two
jurisdictions differ in the *number* of tiers, not just the values. A tiered-data model covers both
with one code path, extends to a third jurisdiction by inserting a row, and keeps FR-050's
"adjustable by an owner" honest — a collective agreement can add a stricter tier without a code
change.

**Alternatives considered**: Hard-coded per-country logic — rejected because it makes every
collective agreement a code change and fails the extensibility the spec's assumptions promise.
Two nullable columns for the two known tiers — rejected as a data model that encodes today's two
jurisdictions into its shape.

---

## Decision 6 — `EmploymentTerms` versions are closed on insert, and immutable after

**Decision**: A new version supplies `ValidFrom`; inserting it sets the previous version's
`ValidTo` to the day before. Version rows expose no mutating methods other than that closing
operation. Reads for a period select the version whose range contains the period.

**Rationale**: FR-022 forbids altering an earlier version and FR-023 requires past periods to use
the terms then in effect. Closing the predecessor on insert keeps ranges contiguous and
non-overlapping without a background process, and makes the "which terms applied in March" query a
single range predicate. Story 3 scenario 3 explicitly requires an attempt to edit history to be
refused, which an entity with no setters enforces at the domain level rather than in a validator.

**Alternatives considered**: Open-ended versions with `ValidTo` always null and resolution by
"latest `ValidFrom` ≤ date" — simpler to write but makes gaps and overlaps invisible, and makes
FR-026 ("no terms for a period") indistinguishable from "terms not yet started". Soft-deleting and
re-inserting on change — rejected as history rewriting under another name.

---

## Decision 7 — `WeekDays` as a flags enum

**Decision**: `[Flags] enum WeekDays { Monday = 1, Tuesday = 2, ... Sunday = 64 }`, stored as an
`int` column on `EmploymentTerms`.

**Rationale**: A working-day pattern is a set of up to seven fixed members. A flags enum stores it
in one column, compares with a single bitwise test in `WorkingDayCalculator`, and needs no child
table or join for what is read on every target-hour calculation. It also serialises to JSON as a
readable string set via the globally registered `JsonStringEnumConverter`.

**Alternatives considered**: A child table of working days — rejected as a join for seven bits.
Seven boolean columns — rejected as unqueryable as a set and awkward to extend.

---

## Decision 8 — Reconciliation joins through `ProjectMember`, read-only, and tolerates ambiguity

**Decision**: Phase 6's query walks `TimeEntry.MemberId` → `ProjectMember.Id` →
`ProjectMember.UserId` to attribute project-booked hours to a user, then compares the monthly sum
against recorded working hours. It is a projection with no write path and no change to
`TimeEntry`, `Project`, or any profitability method.

**Rationale**: `TimeEntry` is scoped to a project membership rather than to a user, so there is no
direct user axis to join on — this was confirmed against `GetCapacityQueryHandler`, which resolves
members the same way. The join is therefore mandatory, not incidental.

Two data realities have to be tolerated rather than fixed: a user can hold several
`ProjectMember` rows across projects, so booked hours must be summed across all of them; and a
`ProjectMember` row can outlive its `User`, so unresolvable memberships are reported as
unattributed rather than dropped. FR-034 and FR-035 forbid altering either side, and Story 5
scenario 2 requires booked-exceeding-recorded to be flagged for review rather than treated as an
error — so the query reports, it does not reconcile.

**Alternatives considered**: Adding `UserId` to `TimeEntry` to make the join direct — rejected
outright as modifying an existing entity and its table for this module's convenience, forbidden by
Principle I and by FR-035's spirit. Deriving working time from project bookings — rejected by
FR-009.

---

## Decision 9 — Plan gating on `Company`, gating writes only

**Decision**: Add `Company.HasWorkTimeModule()` returning true for Pro and Business, honouring the
existing expired-plan fallback to Free-tier behaviour. Command handlers that create or change
records check it; queries do not.

**Rationale**: This mirrors `IsUserLimitReached()` and `IsProjectLimitReached()` exactly — plan
rules already live on `Company` as domain methods, and putting this one anywhere else would fork an
established pattern. The spec's assumption is that a downgraded company keeps read access to
existing records but cannot create new ones, so the check belongs on the write path only. Adding a
method to an existing entity is an additive edit permitted by Principle I.

**Alternatives considered**: Checking `PlanTier` in each handler via `ICompanyRepository` —
rejected as anemic; the plan rule is business logic and belongs with the other plan rules.
Middleware or a policy — rejected because plan tier is not an authorization concern and the
existing codebase does not model it as one.

---

## Deferred, and explicitly not researched

Holiday import (FR-066–FR-072) is out of scope for this build. No holiday API was evaluated, no
HTTP client design was produced, and no package was selected — deliberately, since adding the
codebase's first outbound integration requires a constitution amendment first. When the increment
is picked up, the requirements are already specified and the region field (FR-065) it consumes is
built in Phase 4.
