# Contract: `WorkTimeReportsController`

**Route**: `api/worktime-reports` | **Phase**: 5 (report), 6 (reconciliation) | **Class policy**:
`Policies.RecordOwnWorkTime`

Same conventions and error mapping as [timesheets.md](./timesheets.md). A separate controller rather
than new actions on the existing `ReportsController`, so the two modules stay independent.

Per-employee scoping again comes from the query filter — a Member can only produce their own report
because the underlying timesheet rows are invisible to them (FR-036, Story 4 scenario 11).

---

## Month-end report — Phase 5

### `GET api/worktime-reports/monthly/{year:int}/{month:int}?userId={guid?}`

Own report by default; approvers may pass `userId`.

`200` → `MonthlyWorkTimeReportResponse`

```jsonc
{
  "userId": "…", "employeeName": "B. Malkoc",
  "year": 2026, "month": 3,
  "isProvisional": false,        // true while the month is not yet Approved (FR-030)
  "isRevised": false,            // true when inputs changed after locking (FR-032)
  "isSelfApproved": false,

  "targetHours": 161.7,          // null when no employment terms cover the month (FR-026)
  "actualHours": 168.0,
  "monthlyDifference": 6.3,

  "absenceDays": [               // approved absences, by type (FR-027)
    { "type": "Vacation", "days": 2.0 },
    { "type": "SickLeave", "days": 1.0 }
  ],

  "openingBalanceHours": 74.0,
  "closingBalanceHours": 80.0,
  "forfeitedHours": 0.3,         // surplus lost to the cap, always stated (FR-045)
  "surplusCapHours": 80.0,
  "deficitFloorHours": -20.0,
  "deficitFloorBreached": false, // reported, never clamped (FR-046)
  "approachingCap": true,        // drives the FR-047 warning

  "breaches": [
    { "kind": "DailyMaximum", "date": "2026-03-11", "weekStartDate": null,
      "limitValue": 12.0, "actualValue": 13.5,
      "acknowledgedByName": "A. Owner", "acknowledgedAt": "2026-04-02T08:30:00Z" }
  ],

  "days": [
    { "date": "2026-03-02", "startTime": "08:30", "endTime": "17:00",
      "breakMinutes": 30, "workedHours": 8.0, "note": null }
  ]
}
```

For an **Approved** or **Locked** month every figure is read from the snapshot frozen at approval —
never recomputed from current master data (FR-060). For a **Draft** or **Submitted** month the
figures are computed live and `isProvisional` is `true`.

`404` when the employee has no timesheet for that month.

### `GET api/worktime-reports/monthly/{year:int}/{month:int}/pdf?userId={guid?}`

`200` → `application/pdf`, filename `worktime-{lastname}-{year}-{month:00}.pdf`.

Produced by `IWorkTimeReportPdfService` (QuestPDF), alongside the existing
`QuarterlyReportPdfService`. Content must state, per FR-029: employee name, period, the daily
records, the absence summary by type, target, actual, the monthly difference, any forfeited
surplus, and both opening and closing balance. A provisional month is watermarked as such.

`404` as above.

### `GET api/worktime-reports/company/{year:int}/{month:int}`

Company-wide month overview — `Policies.ViewAllWorkTime` (FR-031).

`200` → `CompanyMonthOverviewResponse[]` — `{ userId, employeeName, status, targetHours,
actualHours, monthlyDifference, closingBalanceHours, forfeitedHours, breachCount, isProvisional }`

Ordered by employee name. One round trip for the whole company (SC-017).

---

## Project reconciliation — Phase 6

### `GET api/worktime-reports/reconciliation/{year:int}/{month:int}?userId={guid?}`

Recorded working hours against hours booked to projects, for the same employee and month.
`Policies.ViewAllWorkTime` — this is an owner/admin view (Story 5).

`200` → `ReconciliationResponse`

```jsonc
{
  "userId": "…", "employeeName": "B. Malkoc", "year": 2026, "month": 3,
  "recordedWorkingHours": 168.0,
  "projectBookedHours": 141.0,
  "unbookedHours": 27.0,
  "overBooked": false,           // true when booked exceeds recorded — flagged, not an error
  "byProject": [
    { "projectId": "…", "projectName": "Website relaunch", "bookedHours": 96.0 },
    { "projectId": "…", "projectName": "CRM migration",    "bookedHours": 45.0 }
  ],
  "unattributedBookedHours": 0.0 // memberships whose user could not be resolved
}
```

**Strictly read-only.** The query walks `TimeEntry.MemberId` → `ProjectMember.Id` →
`ProjectMember.UserId`, summing across every membership the user holds, and writes nothing
(FR-034). No existing project cost or profitability figure is touched or recomputed (FR-035).

`overBooked: true` is surfaced for review rather than treated as an error (Story 5 scenario 2).
`unattributedBookedHours` covers memberships that no longer resolve to a user, which are reported
rather than dropped (research.md Decision 8).

`404` when the employee has no timesheet for that month.

---

## Commands and queries

All read-only — this controller has no commands.

| Route | Handler |
|---|---|
| `GET monthly/{year}/{month}` | `GetMonthlyWorkTimeReportQuery → MonthlyWorkTimeReportResponse?` |
| `GET monthly/{year}/{month}/pdf` | `GetMonthlyWorkTimeReportPdfQuery → byte[]?` |
| `GET company/{year}/{month}` | `GetCompanyMonthOverviewQuery → IReadOnlyList<CompanyMonthOverviewResponse>` |
| `GET reconciliation/{year}/{month}` | `GetReconciliationQuery → ReconciliationResponse?` |

No validators — queries carry no user-supplied payload beyond route and query values, which are
bound and range-checked by routing constraints.
