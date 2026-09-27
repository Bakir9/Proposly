# Feature Specification: Work Time Management

**Feature Branch**: `001-work-time-management`

**Created**: 2026-09-08

**Status**: Draft

**Input**: User description: "WorkTimeManagement — a new module for company working-time recording (Arbeitszeiterfassung) and absence management, kept separate from the existing project-costing time logs. Attendance: employees record working time per day into a monthly timesheet with a Draft → Submitted → Approved → Locked lifecycle; covers non-project work, never derived from project time logs. Absence: vacation, sick leave, unpaid leave, parental/special leave with request/approve workflow, half-days, per-year entitlement with carry-over. Versioned employment records so a mid-year contract change does not rewrite earlier months. Monthly report per employee: target hours vs actual with running overtime balance and export. Read-only reconciliation of recorded hours against hours booked to projects. Members must not see colleagues' records; sick leave records the type only, never medical detail."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record my working time (Priority: P1)

An employee opens the current month and records, for each working day, when they started, when they
finished, and how long they took for breaks. Times are typed in by the employee rather than
captured by a live clock, so a day can be entered as it happens or a whole week completed
afterwards. A running monthly total of recorded hours is always visible. At month end the employee
submits the month for review; an approver checks it and approves it, after which the month is
locked against further edits.

**Why this priority**: This is the statutory record. Austrian and German employers must record
employee working time, and today the company has no way to do it at all. Recorded actual hours plus
an approval trail is a complete, legally useful artefact even before target hours, absences, or
reporting exist.

**Independent Test**: Fully testable on its own — an employee records a week of days, submits the
month, an approver approves it, and the month becomes read-only. Delivers a defensible working-time
record with no dependency on any other story.

**Acceptance Scenarios**:

1. **Given** an employee with no entries for the current month, **When** they record a day with a
   start time, end time, and break duration, **Then** the day shows the resulting worked hours and
   the monthly total increases by that amount.
2. **Given** a day already recorded, **When** the employee changes the times while the month is
   still in Draft, **Then** the day and the monthly total update accordingly.
3. **Given** several past days in the current Draft month with nothing recorded, **When** the
   employee fills them in retrospectively, **Then** each is accepted and the monthly total reflects
   all of them.
4. **Given** an end time earlier than the start time, **When** the employee tries to save,
   **Then** the entry is rejected with a message explaining the times are inconsistent.
5. **Given** a break duration longer than the span between start and end, **When** the employee
   tries to save, **Then** the entry is rejected.
6. **Given** a month in Draft, **When** the employee submits it, **Then** its status becomes
   Submitted, it is no longer editable by the employee, and the approver is notified.
7. **Given** a Submitted month, **When** an approver approves it, **Then** its status becomes
   Approved and Locked, the employee is notified, and no further edits are accepted.
8. **Given** a Submitted month, **When** an approver returns it for correction, **Then** it goes
   back to Draft, the employee is notified, and the employee can edit it again.
9. **Given** a Locked month, **When** anyone attempts to add or change a day in it, **Then** the
   change is rejected.
10. **Given** two people in different companies, **When** each views working-time records,
    **Then** neither can see any record belonging to the other company.

---

### User Story 2 - Request and approve time off (Priority: P2)

An employee requests time off, choosing a type (vacation, sick leave, unpaid leave, parental leave,
special leave), a date range, and optionally half-days at either end. Vacation requests show the
employee's remaining entitlement before submitting. An approver sees pending requests and approves
or rejects them; the employee is notified either way. Approved vacation reduces the remaining
entitlement.

**Why this priority**: Absence tracking is the other half of the month-end picture and today lives
in spreadsheets. It is independently valuable — a company can manage vacation and sick leave with
this alone — and approved absences are required for the target-hour calculation in Story 4 to be
correct.

**Independent Test**: Testable without any working-time recording — an employee requests five days
of vacation, an approver approves it, and the employee's remaining balance decreases by five days.

**Acceptance Scenarios**:

1. **Given** an employee with remaining vacation entitlement, **When** they request a date range,
   **Then** the request is created as Pending, the working days it consumes are shown, and the
   approver is notified.
2. **Given** a pending vacation request, **When** an approver approves it, **Then** its status
   becomes Approved, the employee's used days increase by the working days consumed, and the
   employee is notified.
3. **Given** a pending request, **When** an approver rejects it with a reason, **Then** its status
   becomes Rejected, no entitlement is consumed, and the employee is notified with the reason.
4. **Given** an approved future absence, **When** the employee cancels it, **Then** its status
   becomes Cancelled and the consumed entitlement is returned.
5. **Given** a vacation request for more working days than the employee has remaining, **When**
   they submit it, **Then** they are warned and the request is refused.
6. **Given** a request whose range overlaps an existing Pending or Approved absence for the same
   employee, **When** they submit it, **Then** it is refused as overlapping.
7. **Given** a request spanning a weekend or public holiday, **When** the consumed days are
   calculated, **Then** non-working days are excluded from the count.
8. **Given** a sick leave request, **When** it is recorded, **Then** only the absence type and
   dates are stored and no field exists to capture a diagnosis or medical detail.
9. **Given** an employee with the Member role, **When** they open the absence area, **Then** they
   see only their own requests and balance, never a colleague's.
10. **Given** an approver, **When** they open the absence area, **Then** they see pending and
    decided requests for every employee in their company.

---

### User Story 3 - Define employment terms and the company calendar (Priority: P3)

An owner or admin records each employee's employment terms — the date they take effect, weekly
contracted hours, which weekdays they normally work, and annual vacation entitlement — and
maintains the company's public holiday list, entering each day by hand. Public holidays never
consume anyone's vacation entitlement; a company closure day, recorded separately, does by default.
Holidays and closure days appear as read-only markers in the existing company calendar. Importing a
year's holidays for the company's holiday region is specified but deferred to a later increment
(FR-066 to FR-072), so scenarios 8 to 12 below are not in scope for the initial build. When terms
change, a new dated version is added rather than overwriting the old one, so past months keep the
terms that applied at the time. Each employee's expected working days and target hours for any
month become visible.

**Why this priority**: This is the reference data that turns recorded hours into a meaningful
comparison. It has standalone value — an owner can see who is contracted for how many hours and how
much vacation each person is owed — but it is only fully exercised by Story 4.

**Independent Test**: Testable on its own — an admin enters 38.5 weekly hours over Monday to Friday
effective 1 January, adds the year's public holidays, and the system shows the correct number of
expected working days and target hours for March.

**Acceptance Scenarios**:

1. **Given** no employment terms for an employee, **When** an admin records weekly hours, working
   weekdays, annual vacation days, and an effective-from date, **Then** those terms apply from that
   date onward.
2. **Given** existing terms effective 1 January, **When** an admin records new terms effective
   1 July, **Then** months before July still use the January terms and months from July use the new
   ones.
3. **Given** terms already in effect, **When** an admin attempts to change the historical version
   in place, **Then** the change is refused and they are directed to add a new dated version.
4. **Given** a month and an employee with terms in effect, **When** target hours are calculated,
   **Then** the result reflects that employee's working weekdays in that month minus public
   holidays falling on those weekdays.
5. **Given** a public holiday on a Saturday for an employee who does not work Saturdays, **When**
   target hours are calculated, **Then** the holiday has no effect.
6. **Given** an employee with no terms recorded, **When** their month is viewed, **Then** actual
   recorded hours are still shown and target hours are reported as unavailable rather than zero.
7. **Given** an employee with the Member role, **When** they view employment terms, **Then** they
   see only their own and cannot create or change any.
8. *(deferred)* **Given** a holiday region of Austria / Upper Austria, **When** an admin imports the year's
   holidays, **Then** they are shown the list that would be added and nothing changes until they
   confirm it.
9. *(deferred)* **Given** an imported holiday list, **When** an admin edits one entry by hand and repeats the
   import, **Then** the manual edit is not silently overwritten and the conflict is shown for a
   decision.
10. *(deferred)* **Given** a year already imported, **When** the same year is imported again, **Then** no
    duplicate entries are created.
11. *(deferred)* **Given** the external holiday source is unreachable, **When** an admin opens the holiday list
    or an employee requests absence, **Then** the existing list is used and nothing fails or is
    blocked.
12. *(deferred)* **Given** an approved month covering March, **When** an import would change a March holiday,
    **Then** March's holidays are left as they were and the month is flagged for review.
13. **Given** a public holiday and a company closure day in the same month, **When** an employee is
    absent on both, **Then** the public holiday consumes no vacation entitlement and the closure day
    consumes one day unless an owner has overridden it.
14. **Given** a public holiday, a company closure day, and the viewer's own approved absence,
    **When** they open the company calendar, **Then** all three appear as read-only markers and no
    calendar appointment has been created for any of them.

---

### User Story 4 - Month-end working time report (Priority: P4)

At the end of a month each employee has a report showing target hours, actual recorded hours,
approved absences broken down by type, the resulting difference for the month, and a running
overtime balance carried forward from previous months. The balance is bounded in both directions by
the company's flexitime agreement: surplus above the agreed cap is forfeited and shown as such, and
a deficit past the agreed floor is flagged for the employer to act on. The report can be exported
as a document for payroll or for the employee's records. Owners and admins can see this for any
employee and get a company-wide overview of the month.

**Why this priority**: This is the deliverable the module exists to produce, but it is meaningful
only once hours are recorded (Story 1), absences are known (Story 2), and contracted terms exist
(Story 3).

**Independent Test**: Testable once its inputs exist — for an employee with 38.5 weekly hours, 21
expected working days, 2 approved vacation days, and 160 recorded hours, the report shows the
correct target, actual, difference, and updated running balance, and exports as a document.

**Acceptance Scenarios**:

1. **Given** an employee with terms, recorded hours, and approved absences in a month, **When**
   they open the month-end report, **Then** it shows target hours, actual hours, absence days by
   type, the month's difference, and the running balance including previous months.
2. **Given** actual hours exceeding target, **When** the report is produced, **Then** the
   difference is shown as a surplus and added to the running balance.
3. **Given** actual hours below target, **When** the report is produced, **Then** the difference is
   shown as a deficit and subtracted from the running balance.
4. **Given** a balance of 74 hours, a cap of 80 hours, and a month's surplus of 9 hours, **When**
   the report is produced, **Then** the closing balance is 80 hours and 3 forfeited hours are shown
   as a separate labelled figure.
5. **Given** a balance that has reached the cap, **When** the following month is reported,
   **Then** the employee is warned that further surplus will be forfeited.
6. **Given** a deficit that would fall below the configured floor, **When** the report is
   produced, **Then** the breach is shown to both the employee and the approver rather than being
   clamped.
7. **Given** a December report with a closing balance, **When** the following January is reported,
   **Then** that balance is the January opening balance with no annual reset.
8. **Given** approved absence days in the month, **When** the target is calculated, **Then** those
   days do not count as hours the employee still owes.
9. **Given** a completed report, **When** the employee exports it, **Then** they receive a document
   containing the employee name, period, daily records, absence summary, target, actual, difference,
   forfeited hours if any, and running balance.
10. **Given** an owner or admin, **When** they open the company overview for a month, **Then** they
    see every employee's target, actual, difference, and timesheet status in one list.
11. **Given** an employee with the Member role, **When** they open the report area, **Then** they
    can produce and export only their own report.
12. **Given** a month that is still in Draft, **When** the report is produced, **Then** it is
    clearly marked as provisional.

---

### User Story 5 - Reconcile recorded hours against project bookings (Priority: P5)

An owner or admin compares an employee's recorded working hours for a month against the hours that
employee booked to projects in the same month, and sees the unaccounted difference. The view is
informational only — it never changes either record.

**Why this priority**: Useful margin insight — it surfaces work that was performed but never booked
to a project, and therefore never costed. It is a reporting convenience rather than a compliance
need, so it comes last.

**Independent Test**: Testable once Story 1 exists and project time logs are present — an employee
recorded 160 working hours and booked 141 hours to projects, and the view reports 19 unbooked hours.

**Acceptance Scenarios**:

1. **Given** an employee with recorded working hours and project bookings in the same month,
   **When** an admin opens the reconciliation view, **Then** it shows recorded hours, booked hours,
   and the difference.
2. **Given** booked hours exceeding recorded hours, **When** the view is opened, **Then** the
   discrepancy is flagged for review rather than treated as an error.
3. **Given** an employee who booked no project hours, **When** the view is opened, **Then** all
   recorded hours appear as unbooked.
4. **Given** the reconciliation view, **When** it is opened or refreshed, **Then** neither the
   working-time records nor the project time logs are modified in any way.
5. **Given** an existing project cost or profitability figure, **When** this module is in use,
   **Then** that figure is unchanged by anything in this module.

---

### User Story 6 - Working time compliance guardrails (Priority: P2)

As employees record their days, the system checks each one against the statutory working-time rules
that apply to the company — maximum hours per day and per week, the minimum break for a day of that
length, and the minimum rest between finishing one day and starting the next. Where a day breaches
a rule it is accepted and flagged, never refused. The employee sees the flag immediately, the
approver sees every outstanding breach before approving the month and has to acknowledge them
explicitly, and an owner can see all breaches across the company for a period.

**Why this priority**: This is what makes the module a compliance tool rather than a spreadsheet
replacement. It shares the P2 band with absence and is the natural increment straight after Story 1,
because it needs only recorded days and the rule set — not contracts, absences, or reporting.

**Independent Test**: Testable with Story 1 alone — an employee records an 11-hour day with a
20-minute break, and the day is saved and flagged for both the daily maximum and the insufficient
break, with the applicable limits stated.

**Acceptance Scenarios**:

1. **Given** a company in Austria with no rule set configured, **When** the module is first used,
   **Then** a rule set is initialised from Austrian statutory defaults and an owner can review and
   adjust every value.
2. **Given** a day whose worked hours exceed the maximum daily hours, **When** the employee saves
   it, **Then** the day is **accepted** and flagged as a daily-limit breach stating the limit and
   the excess.
3. **Given** a day of 7 worked hours with a 20-minute break and a 30-minute minimum for days over
   6 hours, **When** the employee saves it, **Then** the day is accepted and flagged as a break
   breach stating the required minimum.
4. **Given** a day of 9.5 worked hours in a company whose rules require 45 minutes above 9 hours,
   **When** a 30-minute break is recorded, **Then** the day is flagged against the higher tier.
5. **Given** a day ending at 22:00 and the next beginning at 06:00 with an 11-hour minimum rest,
   **When** both are recorded, **Then** both days are flagged as a rest-period breach.
6. **Given** a calendar week whose total exceeds the maximum weekly hours, **When** the week is
   complete, **Then** the week is flagged as a weekly-limit breach.
7. **Given** a rolling averaging window whose average weekly hours exceed the average cap, **When**
   the period is evaluated, **Then** it is flagged as an averaging breach.
8. **Given** a month containing flagged breaches, **When** the employee submits it, **Then** the
   approver sees every outstanding breach before deciding.
9. **Given** a submitted month containing breaches, **When** an approver approves it, **Then** they
   must acknowledge the breaches explicitly and the acknowledgement is recorded with their identity
   and the time.
10. **Given** an approved month, **When** it is viewed later, **Then** it still lists the breaches
    that were acknowledged at approval.
11. **Given** an owner or admin, **When** they open the compliance overview for a period, **Then**
    they see every outstanding breach across all employees in their company.
12. **Given** a rule set change made today, **When** an already-approved month is viewed, **Then**
    its breaches are unchanged.
13. **Given** an approved full-day absence, **When** limits are evaluated, **Then** that day
    contributes nothing to daily, weekly, or averaged working time.
14. **Given** an employee with the Member role, **When** they view breaches, **Then** they see only
    their own.

### Edge Cases

- An employee is invited mid-month: target hours count only the days from their employment start
  date onward.
- An employee is disabled or leaves mid-month: their locked months remain intact and readable, and
  no new records can be added after their end date.
- An absence spans a month boundary: the consumed days are attributed to the months in which they
  fall, so each month's target is reduced correctly.
- An absence is approved for a month that is already Locked: the month's report is recalculated and
  flagged as revised, since the underlying record must stay accurate.
- Sick leave is reported retroactively for days the employee already recorded as worked: the
  conflict is surfaced to the approver rather than silently overwriting either record.
- A public holiday is added or corrected after months have been reported: affected months are
  flagged as needing review rather than silently recomputed.
- An imported holiday falls on a day the employee does not work: it has no effect on that
  employee's target hours or absence day count, exactly as a manually entered one would.
- The holiday region is changed part-way through a year: already-recorded holidays are left alone
  and the change affects only what is imported or entered afterwards.
- An employee has an approved absence on a date that later becomes a public holiday: the absence is
  flagged for review so the consumed day can be returned, rather than the entitlement being
  adjusted silently.
- A company closure day overlaps an employee's already-approved vacation: the day is counted once,
  not twice, against their entitlement.
- An employee records a day with zero hours (for example a full-day absence): accepted, and it does
  not count toward actual hours.
- A day's recorded span crosses midnight: handled as a single shift attributed to the start date.
- Two approvers act on the same request at once: the first decision wins and the second is told the
  request has already been decided.
- An owner records their own working time: they may submit and approve their own month, and the
  report notes that it was self-approved.
- A company on a plan tier without this module: existing records stay readable but no new records
  can be created.
- A company has not configured a surplus cap or deficit floor: the balance accumulates unbounded
  and the report notes that no flexitime bounds are set, rather than assuming a figure.
- The cap is lowered below an employee's existing balance: the existing balance is preserved and
  flagged as over the new cap, since retroactively forfeiting already-reported hours would rewrite
  a statutory record.
- An employee leaves with an outstanding surplus or deficit: the final month's report states the
  closing balance for settlement, and the module does not itself calculate any payment.
- A rest-period breach spans a month boundary — the last day of one month and the first of the
  next: it is visible in both months, and acknowledging it in one does not hide it in the other.
- A recorded day crosses midnight: it counts as one shift attributed to its start date, and the
  rest-period check measures from its actual end time, not from midnight.
- The rule set is changed mid-month while the month is still open: the remaining days are evaluated
  against the new values and already-flagged days keep the limit that applied when they were
  recorded, with the report stating that two rule versions applied.
- A breach is detected on a day inside a month that is already Locked, because an absence was
  approved retroactively: the month is marked revised, per FR-032, and the new breach is listed
  without altering the earlier acknowledgement.
- The averaging window reaches back before the company started using the module: the average is
  computed from available data and clearly marked partial.
- An employee consistently records exactly the maximum permitted hours: no breach is flagged, and
  the pattern is not treated as suspicious — the module reports, it does not infer intent.

## Requirements *(mandatory)*

### Functional Requirements

**Working time recording**

- **FR-001**: Employees MUST be able to record, for a given date, a start time, an end time, and a
  break duration, from which worked hours are derived. Times are entered by the employee; the
  system MUST NOT require a live clock-in/clock-out to capture working time, and manual entry MUST
  remain available at all times.
- **FR-002**: The system MUST reject a day entry whose end precedes its start, or whose break
  duration equals or exceeds the span between start and end.
- **FR-003**: The system MUST group a person's day entries into a single record per calendar month
  per employee.
- **FR-004**: A monthly record MUST progress through exactly these states: Draft, Submitted,
  Approved, Locked; and MUST support being returned from Submitted to Draft for correction.
- **FR-005**: Employees MUST be able to edit their own day entries only while the month is in
  Draft.
- **FR-006**: The system MUST reject any addition or modification of a day entry belonging to a
  Locked month.
- **FR-007**: Approvers MUST be able to reopen a Locked month, and the system MUST record who
  reopened it and when.
- **FR-008**: The system MUST display a running total of recorded hours for the month being viewed.
- **FR-009**: The system MUST capture working time for all work, including work not attributable to
  any project, and MUST NOT derive working time from project time logs.
- **FR-041**: Employees MUST be able to record or amend any date within an open Draft month
  regardless of whether that date has already passed, so a week or a month can be completed
  retrospectively.
- **FR-042**: The system MUST NOT require an employee to record time on a date in order to record
  time on a later date, so gaps in a Draft month are permitted until it is submitted.

**Absence**

- **FR-010**: Employees MUST be able to request an absence with a type, a start date, an end date,
  and an optional half-day marker on the first and last day.
- **FR-011**: The system MUST support at least these absence types: vacation, sick leave, unpaid
  leave, parental leave, special leave.
- **FR-012**: An absence request MUST progress through: Pending, then Approved, Rejected, or
  Cancelled.
- **FR-013**: The system MUST calculate the working days an absence consumes using the employee's
  working weekdays and the company holiday calendar, excluding weekends, non-working weekdays, and
  public holidays.
- **FR-014**: The system MUST reject an absence request that overlaps an existing Pending or
  Approved absence for the same employee.
- **FR-015**: The system MUST track, per employee and per year, the entitled vacation days, days
  carried over from the previous year, and days used.
- **FR-016**: The system MUST refuse a vacation request that would exceed the employee's remaining
  entitlement, and MUST show the remaining balance before the request is submitted.
- **FR-017**: Approving a vacation absence MUST decrease remaining entitlement by the working days
  consumed; cancelling or rejecting it MUST return them.
- **FR-018**: Sick leave, unpaid leave, parental leave, and special leave MUST NOT consume vacation
  entitlement.
- **FR-019**: The system MUST NOT provide any field for a diagnosis, medical note, or other health
  detail on any absence record; sick leave MUST be identifiable by type and dates only.
- **FR-020**: Employees MUST be notified when their request is approved or rejected, and approvers
  MUST be notified when a request is submitted.

**Employment terms and calendar**

- **FR-021**: Owners and admins MUST be able to record an employee's employment terms comprising an
  effective-from date, weekly contracted hours, the weekdays normally worked, and annual vacation
  entitlement.
- **FR-022**: Employment terms MUST be versioned by effective date; recording new terms MUST create
  a new version and MUST NOT alter or delete any earlier version.
- **FR-023**: Any calculation for a past period MUST use the employment terms that were in effect
  during that period.
- **FR-024**: Owners and admins MUST be able to maintain a company public holiday list, each entry
  having a date and a name.
- **FR-025**: The system MUST derive an employee's expected working days and target hours for a
  month from their effective employment terms and the holiday calendar.
- **FR-026**: Where an employee has no employment terms for a period, the system MUST report target
  hours as unavailable and MUST still report actual recorded hours.

**Employment type and contract overtime arrangements** *(added 2026-09-12)*

- **FR-075**: Employment terms MUST carry an employment type — full-time, part-time, marginal
  employment, apprentice, or other. The type MUST be descriptive only: it MAY suggest a weekly
  figure when terms are being entered, but MUST NOT constrain or validate the hours recorded,
  because a full-time week is 38.5 hours in one company and 40 in another.
- **FR-076**: Employment terms MUST be able to record an all-in arrangement, meaning the salary
  covers additional hours.
- **FR-077**: Employment terms MUST be able to record an overtime lump sum
  (*Überstundenpauschale*) as a number of hours per month.
- **FR-078**: An all-in arrangement and an overtime lump sum MUST be mutually exclusive on the same
  terms version, since all-in already covers every additional hour and a lump sum on top would
  compensate the same overtime twice.
- **FR-079**: Where a compensation arrangement applies, the month's surplus MUST be absorbed before
  it reaches the flexitime balance — the lump sum absorbing up to its monthly hours first, an
  all-in arrangement covering whatever remains. Neither MUST ever offset a *shortfall*: an employee
  who worked less than target still carries that deficit.
- **FR-080**: The month-end report and its exported document MUST state the hours absorbed by a
  lump sum, the hours covered by an all-in arrangement, and the hours that actually reached the
  balance, as separate figures. Compensated surplus MUST NOT simply disappear from the report.
- **FR-081**: The absorbed and covered figures MUST be frozen into the month-end snapshot at
  approval, alongside the other reported figures, and MUST be resolved from the terms version in
  force on the last day of the month being reported.
- **FR-065**: The system MUST allow a holiday region to be set per company — a country and, where
  the country has them, a subdivision — because public holidays differ by subdivision in both
  Austria and Germany. The holiday region MUST be settable independently of the company's postal
  address country.
**FR-066 to FR-072 are DEFERRED to a later increment.** Holiday import is not in scope for the
initial build: the manual list (FR-024) already satisfies the requirement that holidays never
consume vacation entitlement, and importing would add the codebase's first outbound HTTP
integration and therefore a constitution amendment. The requirements are retained, fully specified,
so the increment can be picked up without re-analysis. Everything else in this group — FR-065,
FR-073, FR-074 — is in scope now.

- **FR-066** *(deferred)*: Owners and admins MUST be able to populate the holiday list for a chosen
  year from an external public-holiday source for the configured region, as an alternative to
  entering each entry by hand.
- **FR-067** *(deferred)*: An import MUST first present what it would add, change, and leave untouched, and MUST
  take effect only on explicit confirmation.
- **FR-068** *(deferred)*: Each holiday entry MUST record whether it was entered manually or imported. Imported
  entries MUST remain editable and deletable exactly like manual ones.
- **FR-069** *(deferred)*: An import MUST NOT silently overwrite an entry that was created or edited manually;
  such conflicts MUST be surfaced for a person to decide.
- **FR-070** *(deferred)*: Where the external source is unavailable, incomplete, or changed, the existing holiday
  list MUST remain fully usable and no absence calculation, target-hour calculation, or month-end
  close may fail or be blocked. Import is optional enrichment, never a runtime dependency.
- **FR-071** *(deferred)*: An import MUST NOT alter holidays in a period already covered by an approved month;
  affected approved months MUST be flagged for review instead, per FR-032.
- **FR-072** *(deferred)*: Repeating an import for a year already imported MUST NOT create duplicate entries.
- **FR-073**: The system MUST distinguish a public holiday from a company closure day, and each
  entry MUST carry whether it consumes vacation entitlement. A public holiday MUST NOT consume
  entitlement; a company closure day MUST consume it by default, with an owner able to override
  that per entry.
- **FR-074**: Public holidays, company closure days, and the viewer's own approved absences MUST
  appear in the existing company calendar as read-only markers. Doing so MUST NOT create or modify
  any calendar appointment, and removing a holiday or absence MUST remove its marker.

**Month-end reporting**

- **FR-027**: The system MUST produce, per employee and per month, a report showing target hours,
  actual recorded hours, approved absence days by type, the difference between target and actual,
  and a running balance carried forward from prior months.
- **FR-028**: Approved absence days MUST reduce the month's target hours rather than being counted
  as hours worked.
- **FR-029**: The month-end report MUST be exportable as a self-contained document suitable for
  payroll and for the employee's own records, stating target hours, actual hours, absences by type,
  the monthly difference, any forfeited surplus, and the opening and closing balance.
- **FR-030**: A report produced for a month not yet Approved MUST be marked as provisional.
- **FR-031**: Owners and admins MUST be able to view a company-wide overview listing every
  employee's target, actual, difference, and monthly status for a chosen month.
- **FR-032**: Where a report changes after being Locked — for example a retroactively approved
  absence — the system MUST mark that month as revised rather than silently altering it.
- **FR-043**: Owners and admins MUST be able to configure, per company, an upper cap on a carried
  surplus balance and a lower floor on a carried deficit balance, reflecting the terms of the
  company's flexitime agreement.
- **FR-044**: The running balance MUST carry forward across month and year boundaries without an
  automatic annual reset, bounded by the configured cap and floor.
- **FR-045**: Where a month's surplus would take the balance above the configured cap, the balance
  MUST be held at the cap and the forfeited hours MUST be shown as a distinct, labelled figure on
  the month-end report — never silently discarded.
- **FR-046**: Where a month's deficit would take the balance below the configured floor, the
  system MUST show the breach to both the employee and the approver rather than silently clamping
  it, since a deficit beyond the agreed floor requires a decision by the employer.
- **FR-047**: The system MUST warn an employee when their balance approaches the configured cap,
  so surplus hours can be taken as time off before they are forfeited.
- **FR-048**: A change to the configured cap or floor MUST apply only from the month in which it is
  made onward, and MUST NOT retroactively alter any previously reported month or forfeiture figure.

**Working time compliance**

- **FR-049**: The system MUST maintain, per company, a working-time rule set comprising: maximum
  hours per day, maximum hours per calendar week, an averaging window with its average weekly cap,
  minimum break durations tiered by the length of the working day, minimum rest between the end of
  one working day and the start of the next, and minimum weekly rest.
- **FR-050**: The rule set MUST be initialised from the company's country and MUST remain
  adjustable by an owner or admin, so a stricter company or collective agreement can be reflected.
- **FR-051**: The system MUST NOT refuse a day entry because it breaches a working-time rule. A
  truthful record takes precedence over a compliant-looking one; the entry MUST be accepted and the
  breach flagged. Only internally inconsistent data — an end before its start, or a break equal to
  or longer than the span — is refused, per FR-002.
- **FR-052**: The system MUST flag a day whose worked hours exceed the maximum hours per day,
  stating the applicable limit and the excess.
- **FR-053**: The system MUST flag a day whose recorded break is shorter than the minimum required
  for a working day of that length, stating the required minimum and the tier that applies.
- **FR-054**: The system MUST flag both days of a consecutive pair where the interval between the
  first day's end and the next day's start is shorter than the minimum rest.
- **FR-055**: The system MUST flag a calendar week whose total worked hours exceed the maximum
  hours per week.
- **FR-056**: The system MUST flag a rolling averaging window whose average weekly hours exceed
  the average weekly cap, and where the window extends before the company adopted the module the
  average MUST be reported as partial rather than presented as complete.
- **FR-057**: Approved absence days MUST NOT contribute to daily, weekly, or averaged working time
  for the purposes of FR-052 to FR-056.
- **FR-058**: The timesheet view and the month-end report MUST list every flagged breach for the
  period, each stating its type, the date or week affected, the applicable limit, and the recorded
  value.
- **FR-059**: An approver MUST be shown every outstanding breach on a submitted month before
  deciding, and approving a month that contains breaches MUST require an explicit acknowledgement
  recorded with the approver's identity and timestamp.
- **FR-060**: An approved or Locked month MUST retain the breaches that were acknowledged at
  approval, so the record of what was known and accepted is preserved.
- **FR-061**: Breach flags MUST be recalculated whenever a day in the affected period changes while
  the month is still open.
- **FR-062**: A change to the rule set MUST apply from the date of the change onward, and MUST NOT
  create or clear breaches in any already-approved month.
- **FR-063**: Owners and admins MUST be able to view every outstanding breach across all employees
  in their company for a chosen period.
- **FR-064**: Where a company's country is not set or has no shipped defaults, the system MUST
  report that no rule set is active and MUST prompt an owner to configure one, rather than silently
  applying no checks or guessing a jurisdiction.

**Project reconciliation**

- **FR-033**: Owners and admins MUST be able to view, per employee and per month, recorded working
  hours alongside hours booked to projects and the difference between them.
- **FR-034**: The reconciliation view MUST be read-only and MUST NOT create, modify, or delete
  either working-time records or project time logs.
- **FR-035**: This module MUST NOT change any existing project cost, labour cost, or profitability
  figure.

**Visibility and access**

- **FR-036**: Employees with the Member role MUST be able to see only their own working-time
  records, absences, entitlement, employment terms, and reports.
- **FR-037**: Owners and admins MUST be able to see and act on records for every employee in their
  own company.
- **FR-038**: Per-employee visibility MUST be enforced wherever records are retrieved, so that no
  request can return another employee's records regardless of the parameters supplied.
- **FR-039**: No user MUST be able to see any working-time or absence record belonging to another
  company.
- **FR-040**: The system MUST record who approved, rejected, reopened, or revised each record, and
  when.

### Key Entities

- **Monthly Timesheet**: One employee's working time for one calendar month. Holds the month, the
  owning employee, a status in the Draft → Submitted → Approved → Locked lifecycle, who acted on it
  and when, and the day entries it contains.
- **Work Day Entry**: One recorded day within a timesheet — date, start time, end time, break
  duration, derived worked hours, and an optional note.
- **Absence Request**: One employee's request to be away — type, start and end date, half-day
  markers, consumed working days, status, requester, approver, decision timestamp, and an optional
  non-medical reason.
- **Absence Entitlement**: One employee's vacation position for one year — entitled days, days
  carried over, and days used.
- **Employment Terms**: A dated version of an employee's working arrangement — effective-from date,
  optional effective-to date, weekly contracted hours, working weekdays, and annual vacation days.
  Superseded versions are retained.
- **Non-Working Day**: A company-level day on which work is not expected — date, name, kind
  (public holiday or company closure day), whether it consumes vacation entitlement, and whether it
  was entered manually or imported.
- **Holiday Region**: The country and, where applicable, subdivision used to import public
  holidays, held per company and independent of the postal address.
- **Monthly Working Time Summary**: The derived month-end figures for one employee — target hours,
  actual hours, absence days by type, monthly difference, opening and closing balance, and any
  surplus forfeited at the cap. Derived from the entities above rather than entered directly.
- **Flexitime Agreement Settings**: The company-level bounds on the carried balance — the surplus
  cap and the deficit floor, with the date from which each applies. Reflects the terms of the
  company's written flexitime agreement.
- **Working Time Rule Set**: The company-level statutory limits in force — maximum hours per day
  and per week, averaging window and its average cap, break minimums tiered by day length, minimum
  daily and weekly rest, and the date from which the values apply. Initialised from the company's
  country.
- **Compliance Breach**: One detected violation — its type (daily maximum, weekly maximum,
  averaging, insufficient break, insufficient rest), the date or week affected, the applicable
  limit, the recorded value, and, once a month is approved, who acknowledged it and when.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An employee can record a full working day in under 15 seconds and a full week in
  under 90 seconds.
- **SC-002**: An employee can see their remaining vacation entitlement without leaving the page on
  which they request time off.
- **SC-003**: An approver can review and decide all pending absence requests for a 20-person
  company in under 5 minutes.
- **SC-004**: A complete month-end working time report for any employee can be produced and
  exported in under 30 seconds, with no manual calculation by the user.
- **SC-005**: 100% of month-end reports reconcile exactly: target hours minus absence-adjusted
  hours plus the opening balance equals the reported closing balance.
- **SC-006**: A change to an employee's contracted hours effective from a given date leaves every
  previously reported month's figures unchanged.
- **SC-012**: Every hour an employee loses to the surplus cap is visible to that employee on the
  month-end report in which it is forfeited — no forfeiture happens without being stated.
- **SC-013**: An employee can complete a full month of working time in one sitting from an empty
  timesheet in under 10 minutes.
- **SC-014**: 100% of days breaching the configured daily maximum, break minimum, or rest minimum
  are flagged, verified against a month deliberately containing one of each.
- **SC-015**: No working-time entry is ever refused for breaching a limit — verified by recording a
  day well beyond every configured maximum and confirming it saves, with flags raised.
- **SC-016**: An approver can see every outstanding breach for a submitted month without leaving
  the approval screen, and cannot approve a month containing breaches without acknowledging them.
- **SC-017**: An owner can answer "did anyone exceed their limits last month, and where" in under
  60 seconds for a 20-person company.
- **SC-018**: An administrator can populate a full year of public holidays for their region in
  under 2 minutes, and can verify every entry before it takes effect.
- **SC-019**: No public holiday consumes vacation entitlement, and every company closure day
  consumes exactly one day per employee unless explicitly overridden — verified across a year
  containing both.
- **SC-020**: The holiday list, absence requests, and month-end close all remain fully usable with
  the external holiday source unreachable.
- **SC-007**: An employee with the Member role cannot retrieve any colleague's working time or
  absence record through any available means, verified by attempting access with another employee's
  identifier.
- **SC-008**: No absence record can store health information beyond the absence type, verified by
  reviewing every field available on the record.
- **SC-009**: Existing project profitability figures are byte-for-byte identical before and after
  this module is in use, verified on a project with both project time logs and working-time
  records.
- **SC-010**: The company replaces its working-time and vacation spreadsheets entirely — no
  month-end figure requires a spreadsheet to produce.
- **SC-011**: Month-end close for a 20-person company takes under 30 minutes of administrator time,
  down from a full day of spreadsheet work.

## Assumptions

- **Approvers are Owners and Admins.** The existing role model has no manager or supervisor
  relationship, so any Owner or Admin can approve any employee's timesheet or absence in their
  company. A per-employee approver hierarchy is out of scope.
- **Owners may self-approve.** With no higher authority in the company, an Owner can approve their
  own month; the report notes that it was self-approved.
- **Every user is an employee.** Anyone in the company can record working time and request absence;
  there is no separate non-employee user type.
- **Absence reduces target rather than crediting hours.** Approved absence days lower the hours the
  employee is expected to work, rather than being recorded as hours worked. Net effect on the
  balance is the same, and the actual-hours figure stays a truthful record of time worked.
- **Sick leave does not consume vacation entitlement**, per standard practice.
- **Carried-over vacation days do not expire** by default; an administrator can adjust the
  carry-over figure manually if company policy requires forfeiture.
- **Absence granularity is full and half days**, not hours. Hour-level partial absence is out of
  scope.
- **Working time is entered manually and remains so.** Manual entry of start time, end time, and
  break duration is the permanent capture method, not a stopgap. No live clock-in/clock-out is in
  scope, and recorded times are employee-asserted rather than system-observed.
- **Flexitime bounds follow DACH practice.** Balances are bounded in both directions, as an
  Austrian or German flexitime agreement (Gleitzeitvereinbarung) requires the transferable surplus
  and deficit to be stated. The cap and floor are therefore configured per company rather than
  fixed by the platform, with suggested starting values of +80 hours and −20 hours that an
  administrator can change. Forfeiture is always shown rather than applied silently, since the
  employee must be able to see hours they are about to lose.
- **The balance never resets annually.** It carries across the year boundary, bounded only by the
  cap and floor. Payout, conversion to leave, and any other settlement of a balance are payroll
  decisions outside this module; the report states the figure and stops there.
- **Statutory working-time limits are in scope, as advisory flags rather than blocks.** Daily and
  weekly maxima, averaging, minimum breaks, and minimum rest are all checked (Story 6). The design
  principle is that a breach never prevents a record being saved: refusing an 11-hour day the
  employee actually worked would make the statutory record false and push people toward
  under-reporting, which is the opposite of what working-time recording exists for. The system
  therefore records the truth and surfaces the breach to the employee, the approver, and the owner.
- **Shipped rule-set defaults cover Austria and Germany.** Austrian defaults follow the pattern of
  a 12-hour daily and 60-hour weekly ceiling, a 48-hour average over a 17-week window, a 30-minute
  break above 6 hours, 11 hours daily rest and 36 hours weekly rest. German defaults follow a
  10-hour daily ceiling, an 8-hour daily average over a 24-week window, a 30-minute break above
  6 hours rising to 45 minutes above 9 hours, and 11 hours daily rest. Other jurisdictions are
  supported by configuring the same rule set rather than by new code.
- **The rule-set defaults are a starting point, not legal advice.** Collective agreements, sector
  rules, and company agreements frequently impose stricter limits, and the values shipped may lag
  legislative change. Each company remains responsible for confirming its own limits, which is why
  every value is adjustable and why the module flags rather than certifies.
- **Public holidays are maintained per company, with optional import.** An administrator can enter
  them by hand or import a year for the company's holiday region, reviewing the result before it
  takes effect. The manual list is the authority: import is a convenience that can be skipped
  entirely, and no calculation may depend on an external service being reachable. Which external
  source is used is an implementation choice for planning, not a business requirement.
- **Holiday region is separate from the company address.** Public holidays differ by subdivision in
  Austria and Germany, so region is set explicitly rather than inferred from the company's country
  field, which today has no subdivision.
- **A company closure day is not a public holiday.** A public holiday costs the employee nothing; a
  company shutdown or bridge day is normally taken from the employee's vacation entitlement under
  Austrian and German practice, so the two are recorded as different kinds of non-working day with
  different entitlement effects. The default is that a closure day consumes entitlement, overridable
  per entry.
- **Reference data is entered manually** — there is no import from payroll or HR systems, and no
  export to payroll beyond the month-end document.
- **The module is available on the Pro and Business plan tiers.** Companies on lower tiers keep read
  access to any existing records but cannot create new ones.
- **Working-time records are retained for at least 7 years**, covering statutory working-time and
  payroll retention obligations.
- **Existing capabilities are reused rather than rebuilt**: authentication and roles, company-level
  data separation, notification delivery, and document generation all come from what the platform
  already provides.
- **Project time logging is untouched.** The existing per-project time logs, hourly rate snapshots,
  and profitability calculations keep their current behaviour; this module only reads them, and only
  for Story 5.
- **Per-employee visibility is a new access dimension.** Existing data is visible to everyone within
  a company; these records are the first that must be restricted to a single employee, so visibility
  has to be enforced at the point of retrieval rather than in the user interface.

## Resolved Decisions

Both open questions from the first draft were decided on 2026-09-08 and are reflected in the
requirements above.

- **Time capture method (FR-001, FR-041, FR-042)** — manual entry of start time, end time, and
  break duration per day, available at all times and usable retrospectively. No live clock.
- **Balance policy (FR-043 to FR-048)** — carried forward continuously with no annual reset,
  bounded by a company-configured surplus cap and deficit floor per DACH flexitime practice.
  Surplus above the cap is forfeited and shown explicitly; a deficit past the floor is flagged for
  the employer.
- **Statutory limits, breaks and rest periods (Story 6, FR-049 to FR-064)** — brought into scope on
  2026-09-08, reversing the earlier decision to defer them. Checked against a per-company rule set
  seeded from the company's country, and always surfaced as flags rather than enforced as blocks,
  so the record stays truthful.
- **Holiday import and calendar visibility (Story 3, FR-065 to FR-074)** — added 2026-09-08.
  Manual holiday maintenance and their exclusion from vacation counting were already specified
  (FR-013, FR-024, FR-025); this decision adds an optional import for a configured holiday region,
  a review step before it takes effect, the separation of a company closure day from a public
  holiday because their entitlement effects differ, and read-only display of non-working days and
  the viewer's own absences in the existing calendar.
