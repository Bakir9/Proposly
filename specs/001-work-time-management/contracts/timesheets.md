# Contract: `TimesheetsController`

**Route**: `api/timesheets` | **Phase**: 1 (recording), 2 (breaches) | **Class policy**:
`Policies.RecordOwnWorkTime`

Conventions per CLAUDE.md: `sealed` class, `[ApiController]`, `[Route("api/[controller]")]`,
handlers injected per action via `[FromServices]`, `CancellationToken ct` last. Errors are produced
by throwing — the `Program.cs` handler maps `CommandValidationException` → 422 and
`InvalidOperationException` → 400. No try/catch in the controller.

Per-employee scoping is enforced by the query filter, not by these routes — a Member requesting
another employee's month receives 404, not 403, because the row is invisible to them
(research.md Decision 2).

---

## Recording

### `GET api/timesheets/{year:int}/{month:int}`

Own timesheet for a month, created lazily as `Draft` on first read if absent.

`200` → `TimesheetDetailResponse`

```jsonc
{
  "id": "…", "userId": "…", "year": 2026, "month": 3,
  "status": "Draft",
  "totalWorkedHours": 23.75,
  "targetHours": 168.0,            // null until Phase 4 supplies terms
  "isRevised": false,
  "isSelfApproved": false,
  "days": [
    { "id": "…", "date": "2026-03-02", "startTime": "08:30", "endTime": "17:00",
      "breakMinutes": 30, "workedHours": 8.0, "note": null }
  ],
  "breaches": [                     // computed live while Draft/Submitted (Phase 2)
    { "kind": "InsufficientBreak", "date": "2026-03-03", "weekStartDate": null,
      "limitValue": 30, "actualValue": 20 }
  ],
  "submittedAt": null, "approvedAt": null, "approvedByName": null
}
```

### `PUT api/timesheets/{year:int}/{month:int}/days/{date}`

Create or replace one day. Idempotent on `date`. Requires `Draft`.

Body → `UpsertWorkDayCommand { StartTime, EndTime, BreakMinutes, Note? }`

- `204` on success — **a working-time breach never blocks this call (FR-051)**
- `400` when the month is not `Draft`, or `date` falls outside the month
- `422` when `end <= start` without a midnight crossing, or `breakMinutes` ≥ the span

### `DELETE api/timesheets/{year:int}/{month:int}/days/{date}`

`204` · `400` when the month is not `Draft`.

### `POST api/timesheets/{year:int}/{month:int}/submit`

`Draft → Submitted`. Raises `TimesheetSubmittedDomainEvent` → approver notification.

`204` · `400` when not `Draft`.

---

## Approval — `Policies.ApproveWorkTime`

### `GET api/timesheets/pending`

Submitted timesheets awaiting a decision across the company.

`200` → `TimesheetSummaryResponse[]` — `{ id, userId, employeeName, year, month, status,
totalWorkedHours, targetHours, breachCount, submittedAt }`

### `POST api/timesheets/{id:guid}/approve`

Body → `ApproveTimesheetCommand { AcknowledgeBreaches: bool }`

- `204` on success; writes the breach snapshot and the target/actual/balance snapshot
- `400` when not `Submitted`, or when breaches exist and `acknowledgeBreaches` is `false` (FR-059)

An Owner approving their own month succeeds and sets `isSelfApproved`.

### `POST api/timesheets/{id:guid}/return`

`Submitted → Draft` for correction. Body → `{ Reason? }`. `204` · `400` when not `Submitted`.

### `POST api/timesheets/{id:guid}/lock`

`Approved → Locked`, the payroll close. `204` · `400` when not `Approved`.

### `POST api/timesheets/{id:guid}/reopen`

`Approved | Locked → Draft`, recording actor and time. `204` · `400` from any other state.

---

## Company views — `Policies.ViewAllWorkTime`

### `GET api/timesheets/company/{year:int}/{month:int}`

Every employee's month in one call (FR-031).

`200` → `TimesheetSummaryResponse[]`

### `GET api/timesheets/company/breaches?from={date}&to={date}`

Outstanding breaches across all employees for a period (FR-063).

`200` → `BreachOverviewResponse[]` — `{ userId, employeeName, kind, date, weekStartDate,
limitValue, actualValue, timesheetStatus }`

---

## Commands and queries

| Route | Handler |
|---|---|
| `GET {year}/{month}` | `GetMyTimesheetQuery → TimesheetDetailResponse?` |
| `PUT …/days/{date}` | `UpsertWorkDayCommand` |
| `DELETE …/days/{date}` | `DeleteWorkDayCommand` |
| `POST …/submit` | `SubmitTimesheetCommand` |
| `GET pending` | `GetPendingTimesheetsQuery → IReadOnlyList<TimesheetSummaryResponse>` |
| `POST {id}/approve` | `ApproveTimesheetCommand` |
| `POST {id}/return` | `ReturnTimesheetCommand` |
| `POST {id}/lock` | `LockTimesheetCommand` |
| `POST {id}/reopen` | `ReopenTimesheetCommand` |
| `GET company/{year}/{month}` | `GetCompanyMonthQuery → IReadOnlyList<TimesheetSummaryResponse>` |
| `GET company/breaches` | `GetCompanyBreachesQuery → IReadOnlyList<BreachOverviewResponse>` |

Validators required for `UpsertWorkDayCommand`, `ApproveTimesheetCommand`, and
`ReturnTimesheetCommand`. The id-only commands (`Submit`, `Lock`, `Reopen`, `DeleteWorkDay`) follow
the existing grandfathered pattern and carry none.

Write paths call `Company.HasWorkTimeModule()` and throw `InvalidOperationException` → 400 for
companies below Pro. Read paths do not (research.md Decision 9).
