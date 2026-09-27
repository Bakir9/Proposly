# Contract: `WorkTimeSettingsController`

**Route**: `api/worktime-settings` | **Phase**: 2 (policy), 4 (terms + holidays) | **Class policy**:
`Policies.ManageWorkTimeSettings`

Same conventions and error mapping as [timesheets.md](./timesheets.md).

Two read endpoints are exceptions to the class policy and carry
`[Authorize(Policy = Policies.RecordOwnWorkTime)]` at the action level, because every employee must
be able to see the calendar and their own terms: `GET non-working-days` and `GET terms/mine`.

---

## Working time policy — Phase 2

### `GET api/worktime-settings/policy`

Effective policy for the company today, plus version history.

`200` → `WorkTimePolicyResponse`

```jsonc
{
  "id": "…", "validFrom": "2026-01-01", "validTo": null,
  "jurisdiction": "AT", "holidayRegionCode": "AT-4",
  "maxHoursPerDay": 12.0, "maxHoursPerWeek": 60.0,
  "averagingWindowWeeks": 17, "maxAverageHoursPerWeek": 48.0,
  "minDailyRestHours": 11.0, "minWeeklyRestHours": 36.0,
  "surplusCapHours": 80.0, "deficitFloorHours": -20.0,
  "breakRules": [ { "aboveHours": 6.0, "minBreakMinutes": 30 } ],
  "history": [ { "id": "…", "validFrom": "2025-01-01", "validTo": "2025-12-31" } ]
}
```

`404` when no policy exists — the client renders the "no rule set active" prompt required by
FR-064 rather than assuming a jurisdiction.

### `POST api/worktime-settings/policy`

Creates a **new version**. Body → `CreateWorkTimePolicyCommand { ValidFrom, Jurisdiction,
HolidayRegionCode?, MaxHoursPerDay, MaxHoursPerWeek, AveragingWindowWeeks,
MaxAverageHoursPerWeek, MinDailyRestHours, MinWeeklyRestHours, SurplusCapHours?,
DeficitFloorHours?, BreakRules: [{ AboveHours, MinBreakMinutes }] }`

Closes the predecessor at `ValidFrom − 1 day`. Applies forward only — no approved month's breaches
or forfeiture figures change (FR-048, FR-062).

- `201` + `Guid`
- `400` when `ValidFrom` is not after the current version's `ValidFrom`
- `422` on non-positive limits, an empty `BreakRules` when the jurisdiction requires one, duplicate
  `AboveHours` tiers, a `SurplusCapHours` below zero, or a `DeficitFloorHours` above zero

### `GET api/worktime-settings/policy/defaults?jurisdiction={AT|DE}`

Seed values for the form, so an admin starts from the shipped defaults and edits rather than
typing every limit. Returns the same shape as the policy body, unsaved.

`200` · `404` for a jurisdiction with no shipped defaults.

---

## Employment terms — Phase 4

### `GET api/worktime-settings/terms/{userId:guid}`

Full version history for one employee, newest first.

`200` → `EmploymentTermsResponse[]` — `{ id, userId, validFrom, validTo, weeklyHours,
workingDays: ["Monday","Tuesday",…], annualVacationDays }`

### `GET api/worktime-settings/terms/mine`

Own terms only. Action policy `RecordOwnWorkTime` (FR-036).

`200` → `EmploymentTermsResponse[]`

### `POST api/worktime-settings/terms/{userId:guid}`

Creates a new version. Body → `CreateEmploymentTermsCommand { ValidFrom, WeeklyHours, WorkingDays,
AnnualVacationDays }`

Closes the predecessor at `ValidFrom − 1 day` (FR-022). There is **no** update or delete endpoint
for an existing version — history is immutable, and an attempt to change it is refused at the
domain level (Story 3 scenario 3).

- `201` + `Guid`
- `400` when `ValidFrom` is not after the employee's current version
- `422` when `WeeklyHours` ≤ 0 or > 60, `WorkingDays` is `None`, or `AnnualVacationDays` < 0

### `GET api/worktime-settings/terms/{userId:guid}/target?year={int}&month={int}`

Derived expected working days and target hours for a month (FR-025).

`200` → `{ userId, year, month, workingDays: 21, publicHolidaysExcluded: 1, targetHours: 161.7 }`

`targetHours` is `null` where the employee has no terms covering the month — reported as
unavailable, never zero (FR-026).

---

## Non-working days — Phase 4

### `GET api/worktime-settings/non-working-days?from={date}&to={date}`

Company holiday and closure calendar. Action policy `RecordOwnWorkTime` — every employee reads it,
and the frontend also uses it for the calendar markers required by FR-074.

`200` → `NonWorkingDayResponse[]` — `{ id, date, name, kind, consumesVacation, source }`

### `POST api/worktime-settings/non-working-days`

Body → `CreateNonWorkingDayCommand { Date, Name, Kind, ConsumesVacation? }`

`ConsumesVacation` defaults to `false` for `PublicHoliday` and `true` for `CompanyClosure`
(FR-073). `Source` is always `Manual` in this build.

- `201` + `Guid`
- `400` when the date already has an entry — one non-working day per date, which delivers the
  "counted once, not twice" behaviour for a closure overlapping a holiday
- `422` on an empty `Name` or a `PublicHoliday` with `ConsumesVacation: true`

### `PUT api/worktime-settings/non-working-days/{id:guid}`

Body → `UpdateNonWorkingDayCommand { Name, Kind, ConsumesVacation }`. The date is immutable; delete
and recreate to move a day.

`204` · `400` when the date falls inside a month already approved for any employee — the month is
flagged for review instead (FR-032, FR-071).

### `DELETE api/worktime-settings/non-working-days/{id:guid}`

`204`. Where an approved absence exists on that date, the absence is flagged for review so its
consumed day can be returned rather than the entitlement being adjusted silently (spec edge case).

`400` when the date falls inside a month already approved for any employee.

---

## Deferred — not implemented in this build

`POST api/worktime-settings/non-working-days/import` and its preview counterpart carry FR-066 to
FR-072. **Do not build them.** They require an outbound HTTP integration, the codebase's first, and
therefore a constitution amendment before any code. `HolidayRegionCode` is stored now (FR-065) so
the increment has the field it needs.

---

## Commands and queries

| Route | Handler |
|---|---|
| `GET policy` | `GetWorkTimePolicyQuery → WorkTimePolicyResponse?` |
| `GET policy/defaults` | `GetPolicyDefaultsQuery → WorkTimePolicyResponse?` |
| `POST policy` | `CreateWorkTimePolicyCommand → Guid` |
| `GET terms/{userId}` | `GetEmploymentTermsQuery → IReadOnlyList<EmploymentTermsResponse>` |
| `GET terms/mine` | `GetMyEmploymentTermsQuery → IReadOnlyList<EmploymentTermsResponse>` |
| `POST terms/{userId}` | `CreateEmploymentTermsCommand → Guid` |
| `GET terms/{userId}/target` | `GetTargetHoursQuery → TargetHoursResponse?` |
| `GET non-working-days` | `GetNonWorkingDaysQuery → IReadOnlyList<NonWorkingDayResponse>` |
| `POST non-working-days` | `CreateNonWorkingDayCommand → Guid` |
| `PUT non-working-days/{id}` | `UpdateNonWorkingDayCommand` |
| `DELETE non-working-days/{id}` | `DeleteNonWorkingDayCommand` |

Validators required for every command above except `DeleteNonWorkingDayCommand`, which is id-only.
