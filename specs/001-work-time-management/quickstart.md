# Quickstart: Validating Work Time Management

**Date**: 2026-09-10 | **Plan**: [plan.md](./plan.md) | **Contracts**: [contracts/](./contracts/)

How to prove each phase works end to end. Scenarios map to the spec's success criteria and
acceptance scenarios; details of shapes and rules live in [contracts/](./contracts/) and
[data-model.md](./data-model.md) rather than being repeated here.

## Prerequisites

PostgreSQL running locally, and `src/Proposly.API/appsettings.Development.json` present with a
connection string and JWT secret (see the root `README.md`). Note that `AppDbContextFactory`
hardcodes `Host=localhost;Port=5432;Database=proposly;Username=postgres;Password=1234` for
`dotnet ef` — adjust it locally if your Postgres differs.

```bash
# Backend — migration is applied automatically at startup
dotnet build
dotnet run --project src/Proposly.API/Proposly.API.csproj

# Frontend, in a second shell
cd frontend && npm run dev            # http://localhost:5173
```

The dev seeder creates a demo company and a SuperAdmin (`admin@proposly.io`). You need **three**
logins to validate visibility properly: an Owner, an Admin, and a Member. Create the Member and
Admin through the existing team invite flow if the seed data does not already include them.

Scalar API docs are at `/scalar` in Development for exercising endpoints directly.

## Automated checks

```bash
dotnet test                                        # all projects
dotnet test tests/Proposly.Domain.Tests            # entities + the 3 calculators
dotnet test tests/Proposly.Application.Tests       # handlers + the filter regression

cd frontend && npm run build && npm run lint       # must both be clean
```

**The one test that must never be skipped**: the query-filter regression in
`Proposly.Application.Tests/WorkTimeManagement/`. It asserts that an entity implementing only
`ITenantEntity` produces the same filter predicate as before this feature. It guards the single
shared-infrastructure change in the plan — see plan.md, Complexity Tracking.

---

## Phase 1 — Record working time

Sign in as the Member.

1. Open **My work time**. The current month renders as `Draft` with an empty grid.
2. Record Mon 08:30–17:00 with a 30-minute break → the row shows **8.0 h** and the month total
   updates. *(US1 sc. 1)*
3. Edit the same day to 08:15–16:45 → the row and total both change. *(US1 sc. 2)*
4. Leave Tue and Wed empty, fill in Thu and Fri → both accepted; gaps are allowed and past days can
   be filled retrospectively. *(US1 sc. 3, FR-041, FR-042)*
5. Try 17:00–08:30 → rejected with a field error. Try 8h with a 600-minute break → rejected.
   *(US1 sc. 4–5, 422)*
6. **Submit** → status `Submitted`, grid becomes read-only, and the Owner receives a notification.
   *(US1 sc. 6)*
7. As the Owner, open **Approvals**, approve the month → status `Approved`, Member notified, edits
   refused. *(US1 sc. 7)*
8. Reopen it, confirm it returns to `Draft` and records who reopened it. Approve again, then
   **Lock** it, then attempt a day edit → refused. *(US1 sc. 8–9, FR-006, FR-007)*

**Visibility check — the important one.** As the Member, call
`GET api/timesheets/company/2026/3` → **403** (policy). Then take the Owner's timesheet id and call
`GET api/worktime-reports/monthly/2026/3?userId={owner-id}` as the Member → **404**, because the row
is invisible to the filter rather than merely forbidden. *(FR-036, FR-038, SC-007)*

Confirm two companies stay separated by repeating step 2 in a second company and checking neither
sees the other. *(US1 sc. 10, FR-039)*

---

## Phase 2 — Compliance guardrails

Sign in as the Owner and set a policy: `POST api/worktime-settings/policy` seeded from
`GET policy/defaults?jurisdiction=AT`.

1. Before any policy exists, open the work time area → the "no rule set active" prompt appears
   rather than silent no-checking. *(FR-064)*
2. As the Member, record a **13.5-hour day**. It **saves**, and is flagged as a daily-maximum breach
   stating limit 12 and actual 13.5. **A breach must never block the save.** *(US6 sc. 2, FR-051,
   SC-015)*
3. Record a 7-hour day with a 20-minute break → flagged as an insufficient break, stating the
   30-minute minimum. *(US6 sc. 3)*
4. Switch the policy to `DE` and record a 9.5-hour day with a 30-minute break → flagged against the
   45-minute tier, proving tier selection. *(US6 sc. 4)*
5. Record a day ending 22:00 followed by one starting 06:00 → **both** days flagged for
   insufficient rest. *(US6 sc. 5)*
6. Record a week totalling over 60 hours → the week is flagged. *(US6 sc. 6)*
7. Submit, then as the Owner attempt to approve **without** acknowledging → refused. Acknowledge and
   approve → succeeds, and the breach rows persist with your name and timestamp. Reopen and check
   the acknowledged breaches are still listed. *(US6 sc. 8–10, FR-059, FR-060, SC-016)*
8. Create a new policy version with a higher daily maximum → the already-approved month's breaches
   are unchanged. *(US6 sc. 12, FR-062)*
9. As the Owner, open the company compliance overview for the month → every employee's outstanding
   breaches in one view, answerable in under a minute. *(US6 sc. 11, FR-063, SC-017)*

---

## Phase 3 — Absence

1. As the Owner, set the Member's entitlement for the year:
   `PUT api/absences/entitlement/{userId}/2026` with 25 days.
2. As the Member, open **Time off**. The request form shows remaining entitlement live as you pick
   dates, via `GET api/absences/preview`. *(SC-002)*
3. Request 6–17 July with a half-day on the last day → created `Pending`, consumed days shown, Owner
   notified. Confirm the weekend is excluded from the count. *(US2 sc. 1, sc. 7)*
4. Request an overlapping range → refused as overlapping. *(US2 sc. 6, FR-014)*
5. Request 30 days of vacation → refused, with the remaining balance shown. *(US2 sc. 5, FR-016)*
6. As the Owner, approve the July request → Member notified, used days increase by 9.5.
   *(US2 sc. 2, FR-017)*
7. As the Member, cancel the approved future absence → entitlement returned. *(US2 sc. 4)*
8. Request sick leave and inspect the created row and the API response: **no field for a diagnosis
   or medical detail exists anywhere**. *(US2 sc. 8, FR-019, SC-008)*
9. As the Member, `GET api/absences` → only your own rows. As the Owner, the same call → the whole
   company. Same endpoint, no role branch in the controller. *(US2 sc. 9–10)*
10. Reject a request with a reason → Member notified with the reason, no entitlement consumed.
    *(US2 sc. 3)*

---

## Phase 4 — Employment terms and holidays

1. As the Owner, create terms for the Member: 38.5 weekly hours, Mon–Fri, 25 vacation days,
   effective 1 January. *(US3 sc. 1)*
2. Add the year's public holidays by hand. Then
   `GET api/worktime-settings/terms/{userId}/target?year=2026&month=3` → expected working days and
   target hours, with holidays on working weekdays excluded. *(US3 sc. 4, FR-025)*
3. Add a holiday on a Saturday → no effect on a Mon–Fri employee's target. *(US3 sc. 5)*
4. Create a second terms version effective 1 July with 30 weekly hours → March still uses 38.5,
   August uses 30. *(US3 sc. 2, FR-023, SC-006)*
5. Attempt to modify the January version → refused; there is no endpoint for it. *(US3 sc. 3,
   FR-022)*
6. Check an employee with **no** terms: actual hours still report, target reports as unavailable
   rather than zero. *(US3 sc. 6, FR-026)*
7. Add a `CompanyClosure` day and a `PublicHoliday`. Take absence covering both → the holiday
   consumes no entitlement, the closure consumes one day. *(US3 sc. 13, FR-073, SC-019)*
8. Attempt a second entry on a date that already has one → refused, which is what makes a closure
   overlapping a holiday count once rather than twice.
9. Open the existing **Calendar**. Holidays, closure days, and your own approved absences appear as
   read-only markers. Confirm via the database that **no `Termin` row was created** for any of them.
   *(US3 sc. 14, FR-074)*
10. As the Member, `GET api/worktime-settings/terms/mine` → your own terms; `GET terms/{otherId}`
    → 403. *(US3 sc. 7)*

Scenarios 8–12 of Story 3 cover holiday import and are **deferred** — skip them.

---

## Phase 5 — Month-end report

1. With Phases 1–4 data in place, open the Member's **March report**: target, actual, absence days
   by type, the monthly difference, opening and closing balance. *(US4 sc. 1)*
2. Verify the arithmetic reconciles exactly — opening balance plus difference equals closing
   balance. *(SC-005)*
3. Set the surplus cap to 80 with an opening balance of 74, then record a 9-hour surplus → closing
   balance holds at 80 and **0.3 forfeited hours appear as a distinct labelled figure**, never
   silently dropped. *(US4 sc. 4, FR-045, SC-012)*
4. Confirm the approaching-cap warning shows for the Member. *(FR-047)*
5. Drive a deficit past the floor → reported to both Member and approver, **not clamped**.
   *(US4 sc. 6, FR-046)*
6. Produce December then January → January's opening balance equals December's closing, with no
   annual reset. *(US4 sc. 7, FR-044)*
7. Export the PDF → contains name, period, daily records, absence summary, target, actual,
   difference, forfeited hours, and both balances, in under 30 seconds. *(FR-029, SC-004)*
8. Produce a report for a `Draft` month → clearly marked provisional. *(US4 sc. 12, FR-030)*
9. Approve a month, then retroactively approve an absence inside it → the month is marked
   **revised** rather than silently altered. *(FR-032)*
10. As the Owner, open the company month overview → every employee in one list. *(US4 sc. 10,
    FR-031)*

---

## Phase 6 — Project reconciliation

1. As the Member, log project time against two projects via the existing project time logging, then
   record working hours for the same month.
2. As the Owner, open the reconciliation view → recorded hours, booked hours, the unbooked
   difference, and a per-project breakdown. *(US5 sc. 1)*
3. Book more project hours than recorded working hours → flagged for review, **not** rejected as an
   error. *(US5 sc. 2)*
4. Book no project hours → all recorded hours report as unbooked. *(US5 sc. 3)*
5. **Regression, the critical one.** Record a project's profitability figures before and after using
   this module, on a project that has both project time logs and working-time records. They must be
   identical, and no `TimeEntry` row may be created, changed, or deleted. *(US5 sc. 4–5, FR-034,
   FR-035, SC-009)*

---

## Cross-cutting checks before calling it done

| Check | Requirement |
|---|---|
| A Member cannot reach any colleague's timesheet, absence, entitlement, terms, or report through any endpoint or parameter | FR-036, FR-038, SC-007 |
| Nothing in the module calls `IgnoreQueryFilters()` — grep the module to confirm | research.md Decision 2 |
| No absence field can hold health information beyond the type | FR-019, SC-008 |
| No working-time entry is ever refused for breaching a limit | FR-051, SC-015 |
| Project profitability is byte-identical before and after | FR-035, SC-009 |
| The holiday list, absence requests, and month-end close all work with no external service configured | FR-070, SC-020 |
| Every command with a payload has a sibling validator | Constitution V |
| A company below Pro can read existing records but cannot create new ones | research.md Decision 9 |
| `dotnet build`, `dotnet test`, `npm run build`, `npm run lint` all clean | Constitution — definition of done |
