# Specification Quality Checklist: Work Time Management

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-09-08
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- Items marked incomplete require spec updates before `/speckit-clarify` or `/speckit-plan`

### Validation iteration 1 — 2026-09-08

**Result**: 15 of 16 items pass. Two [NEEDS CLARIFICATION] markers deliberately retained for the
user: the working-time capture method, and the year-end treatment of the overtime balance. Both had
multiple defensible answers with materially different scope and no safe industry default.

**Fixes applied during this iteration**:

- Rewrote three success criteria that were phrased as system internals into user-facing,
  measurable outcomes.
- Removed references to specific existing type and class names from the requirements, keeping them
  in the Assumptions section as business-level statements about reusing existing capability.
- Added the "Visibility and access" requirement group (FR-036 to FR-040) after the first pass
  covered per-employee visibility only inside individual acceptance scenarios.
- Added edge cases for mid-month joiners and leavers, cross-month absences, retroactive sick leave
  conflicting with recorded work, and holiday-calendar corrections after reporting.

### Validation iteration 2 — 2026-09-08

**Result**: 16 of 16 items pass. Specification complete.

Both clarifications were answered by the user and folded in:

1. **Time capture — manual entry, permanently.** Manual entry of start time, end time, and break
   duration per day; no live clock. The user confirmed manual entry must always remain available,
   so it is specified as the permanent capture method rather than a first increment. FR-001
   extended; FR-041 and FR-042 added for retrospective entry and permitted gaps in an open month;
   Story 1 narrative and a new acceptance scenario added.

2. **Balance — continuous carry-forward, bounded per DACH practice.** No annual reset. The user
   asked for standard DACH treatment, so the balance is bounded in *both* directions rather than
   surplus only, reflecting that an Austrian or German flexitime agreement must state the
   transferable surplus and deficit. FR-043 to FR-048 added covering configurable cap and floor,
   explicit forfeiture, deficit-breach visibility, an approach-to-cap warning, and non-retroactive
   application of a changed cap. Four acceptance scenarios, one entity, one success criterion, and
   four edge cases added.

**Additional changes in this iteration**:

- FR-029 now enumerates what the exported document must state, including forfeited surplus and
  both opening and closing balance.
- Added `Flexitime Agreement Settings` to Key Entities.
- Added SC-012 (no unstated forfeiture) and SC-013 (a full month enterable in one sitting, which
  matters now that capture is manual rather than clocked).
- Recorded an explicit non-goal: statutory working-time limits — daily and weekly maxima, minimum
  break durations, and minimum rest between shifts — are **not** validated or warned on in this
  version. The module records what employees report and does not police compliance.
- Renamed `Outstanding Clarifications` to `Resolved Decisions` with both answers and the
  requirements that carry them.
- Renumbered acceptance scenarios in Stories 1 and 4 to stay contiguous after insertions.

**Verified mechanically**: 0 clarification markers, 48 functional requirements numbered FR-001 to
FR-048 with no gaps, 13 success criteria, contiguous scenario numbering in all five stories, no
trailing whitespace.

### Validation iteration 3 — 2026-09-08

**Result**: 16 of 16 items pass. Scope extended at the user's direction; specification complete.

**Change**: statutory working-time limits, breaks and rest periods were moved **into** scope,
reversing the non-goal recorded in iteration 2. This is a material scope increase — it turns the
module from a record-keeping tool into a compliance tool — and it is now the second-largest story
in the spec.

Added as **User Story 6, Working time compliance guardrails (P2)** with 14 acceptance scenarios,
sharing the P2 band with absence because it depends only on Story 1 and is the natural increment
straight after it.

Added **FR-049 to FR-064**, covering: a per-company rule set (daily and weekly maxima, averaging
window and cap, break minimums tiered by day length, daily and weekly rest) seeded from the
company's country and adjustable; detection and flagging of each breach type; exclusion of approved
absence days from limit calculations; breach visibility on the timesheet, the month-end report, and
a company-wide overview; mandatory approver acknowledgement before approving a month with
breaches; retention of acknowledged breaches on a locked month; forward-only application of rule
changes; and an explicit "no rule set active" state where the country is unknown.

**The load-bearing design decision — FR-051: flag, never block.** A breach must not prevent a day
being saved. Refusing an 11-hour day that was actually worked would make the statutory record false
and push employees toward under-reporting, defeating the purpose of working-time recording. Only
internally inconsistent data is refused (FR-002). SC-015 tests this directly by recording a day
beyond every maximum and confirming it saves.

**Also added**: `Working Time Rule Set` and `Compliance Breach` entities; SC-014 to SC-017;
7 edge cases (cross-month rest breaches, midnight-crossing shifts, mid-month rule changes,
retroactive breaches on locked months, partial averaging windows, and consistently-at-the-maximum
patterns not being treated as suspicion); and three assumptions recording the shipped Austrian and
German defaults, the extensibility of the rule set to other jurisdictions, and an explicit statement
that the defaults are a starting point rather than legal advice, since collective and sector
agreements are frequently stricter.

**Verified mechanically**: 0 clarification markers, 64 functional requirements FR-001 to FR-064 with
no gaps, 17 success criteria, 6 user stories, contiguous scenario numbering in all six stories, no
trailing whitespace.

### Validation iteration 4 — 2026-09-08

**Result**: 16 of 16 items pass. Specification complete.

**Change**: holiday handling extended, within Story 3 rather than as a new story.

Manual holiday maintenance and the exclusion of public holidays from vacation counting were
**already specified** in iteration 1 (FR-013, FR-024, FR-025, Story 3 scenarios 4 and 5), so no
change was needed there. What this iteration adds is optional import, region granularity, the
public-holiday vs. company-closure-day distinction, and calendar visibility.

Added **FR-065 to FR-074**: a per-company holiday region set independently of the postal address,
because public holidays differ by subdivision in Austria and Germany and the existing company
country field has no subdivision; import of a year's holidays for that region; a preview-and-confirm
step before any import takes effect; provenance recorded per entry with imported entries remaining
editable; protection of manual edits from being overwritten by a later import; idempotent re-import;
no retroactive change to periods inside an approved month; and read-only display of non-working days
and the viewer's own absences in the existing calendar.

**Two design decisions worth noting**:

1. **Import is enrichment, never a dependency (FR-070, SC-020).** The manual list is the authority.
   No absence calculation, target-hour calculation, or month-end close may fail or block because an
   external service is unreachable — otherwise a third party can stop payroll.
2. **A company closure day is not a public holiday (FR-073).** A public holiday costs the employee
   nothing, whereas a company shutdown or bridge day is normally taken from vacation entitlement
   under Austrian and German practice. Recording both as one kind would silently hand employees
   free days that should have been vacation, so they are separate kinds with different entitlement
   effects — closure days consuming entitlement by default, overridable per entry.

**Also**: the `Public Holiday` entity was generalised to `Non-Working Day` (carrying kind,
entitlement effect, and provenance) and `Holiday Region` added; 7 acceptance scenarios added to
Story 3; SC-018 to SC-020 added; 5 edge cases added, including an approved absence on a date that
later becomes a holiday, and a closure day overlapping approved vacation being counted once rather
than twice.

**Verified mechanically**: 0 clarification markers, 74 functional requirements FR-001 to FR-074 with
no gaps or duplicates, 20 success criteria, 6 user stories, contiguous scenario numbering in all six
stories, no stale references to the renamed entity, no trailing whitespace.

**Ready for** `/speckit.plan`.
