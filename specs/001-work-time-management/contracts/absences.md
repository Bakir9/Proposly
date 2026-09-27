# Contract: `AbsencesController`

**Route**: `api/absences` | **Phase**: 3 | **Class policy**: `Policies.RecordOwnWorkTime`

Same conventions and error mapping as [timesheets.md](./timesheets.md). Per-employee scoping comes
from the query filter: a Member listing absences sees only their own rows, an approver sees the
whole company, from the same endpoint and without a role check in the controller.

---

### `GET api/absences?year={int}&status={AbsenceStatus?}`

`200` → `AbsenceResponse[]`

```jsonc
{
  "id": "…", "userId": "…", "employeeName": "B. Malkoc",
  "type": "Vacation",
  "startDate": "2026-07-06", "endDate": "2026-07-17",
  "firstDayIsHalf": false, "lastDayIsHalf": true,
  "consumedDays": 9.5,
  "status": "Approved",
  "reason": null,
  "approverName": "A. Owner", "decidedAt": "2026-06-02T09:14:00Z",
  "decisionReason": null
}
```

### `GET api/absences/entitlement?year={int}`

Own vacation position for a year; approvers may pass `userId` to read another employee's.

`200` → `AbsenceEntitlementResponse` — `{ userId, year, entitledDays, carriedOverDays, usedDays,
remainingDays }`

`404` when no entitlement row exists for that user and year.

### `GET api/absences/preview?type={AbsenceType}&start={date}&end={date}&firstHalf={bool}&lastHalf={bool}`

Working days a prospective request would consume, so the request form can show the cost and the
resulting balance before submission (FR-016).

`200` → `AbsencePreviewResponse` — `{ consumedDays, remainingDaysAfter, exceedsEntitlement,
nonWorkingDatesExcluded: ["2026-07-11", "2026-07-12"] }`

### `POST api/absences`

Body → `RequestAbsenceCommand { Type, StartDate, EndDate, FirstDayIsHalf, LastDayIsHalf, Reason? }`

Consumed days are computed server-side by `WorkingDayCalculator` and frozen on the row — the client
value from `preview` is never trusted.

- `201` + `Guid`, via `CreatedAtAction`
- `400` when the range overlaps an existing Pending or Approved request (FR-014), or when a
  Vacation request exceeds remaining entitlement (FR-016)
- `422` when `endDate < startDate`, or `reason` exceeds 500 characters

Raises `AbsenceRequestedDomainEvent` → approver notification.

### `POST api/absences/{id:guid}/cancel`

Own request, while `Pending` or `Approved` with a future start date. Releases consumed entitlement.

`204` · `400` when already decided against, or when the absence has started.

---

## Approval — `Policies.ApproveWorkTime`

### `GET api/absences/pending`

`200` → `AbsenceResponse[]` — the company's pending queue.

### `POST api/absences/{id:guid}/approve`

`204`. Consumes entitlement for `Vacation` only (FR-018). Raises `AbsenceApprovedDomainEvent`.

`400` when not `Pending`, or when a concurrent decision already resolved it — the first decision
wins and the second is told so (spec edge case).

### `POST api/absences/{id:guid}/reject`

Body → `RejectAbsenceCommand { Reason }` — required.

`204`. Consumes no entitlement. Raises `AbsenceRejectedDomainEvent` carrying the reason.

`400` when not `Pending` · `422` when `reason` is empty.

---

## Entitlement administration — `Policies.ManageWorkTimeSettings`

### `PUT api/absences/entitlement/{userId:guid}/{year:int}`

Body → `SetEntitlementCommand { EntitledDays, CarriedOverDays }`

Seeded from `EmploymentTerms.AnnualVacationDays` when a year is first set up; carry-over is
adjustable by hand because it does not expire automatically (spec assumption).

`204` · `422` on negative values.

---

## Commands and queries

| Route | Handler |
|---|---|
| `GET ""` | `GetAbsencesQuery → IReadOnlyList<AbsenceResponse>` |
| `GET entitlement` | `GetEntitlementQuery → AbsenceEntitlementResponse?` |
| `GET preview` | `PreviewAbsenceQuery → AbsencePreviewResponse` |
| `POST ""` | `RequestAbsenceCommand → Guid` |
| `POST {id}/cancel` | `CancelAbsenceCommand` |
| `GET pending` | `GetPendingAbsencesQuery → IReadOnlyList<AbsenceResponse>` |
| `POST {id}/approve` | `ApproveAbsenceCommand` |
| `POST {id}/reject` | `RejectAbsenceCommand` |
| `PUT entitlement/{userId}/{year}` | `SetEntitlementCommand` |

Validators required for `RequestAbsenceCommand`, `RejectAbsenceCommand`, and
`SetEntitlementCommand`. `CancelAbsenceCommand` and `ApproveAbsenceCommand` are id-only.

**Privacy constraint on every response**: no field carries a diagnosis or medical detail. `reason`
is free text supplied by the employee for their own purposes and is not required for sick leave
(FR-019).
