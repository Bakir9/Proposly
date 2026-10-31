/**
 * The redesigned workspace IA: 5 groups, each with sub-views that map onto the
 * existing tab implementations (`tab`), so no functionality is lost.
 */
export const PROJECT_GROUPS = [
  {
    id: 'overview',
    label: 'Overview',
    caption: 'Health & setup',
    subs: [
      { id: 'summary', label: 'Overview', tab: 'overview' },
      { id: 'team', label: 'Team', tab: 'team' },
    ],
  },
  {
    id: 'plan',
    label: 'Plan',
    caption: 'Board · List · Timeline · Milestones',
    subs: [
      { id: 'board', label: 'Board', tab: 'tasks' },
      { id: 'list', label: 'List', tab: 'tasklist' },
      { id: 'timeline', label: 'Timeline', tab: 'gantt' },
      { id: 'milestones', label: 'Milestones', tab: 'milestones' },
    ],
  },
  {
    id: 'track',
    label: 'Track',
    caption: 'Time · Expenses',
    subs: [
      { id: 'time', label: 'Time', tab: 'timelog' },
      { id: 'expenses', label: 'Expenses', tab: 'expenses' },
    ],
  },
  {
    id: 'insights',
    label: 'Insights',
    caption: 'Burndown · Velocity',
    subs: [
      { id: 'burndown', label: 'Burndown', tab: 'burndown' },
      { id: 'velocity', label: 'Velocity & capacity', tab: 'velocity' },
    ],
  },
  {
    id: 'collaborate',
    label: 'Collaborate',
    caption: 'Discussion · Notes · Files',
    subs: [
      { id: 'discussion', label: 'Discussion', tab: 'discussion' },
      { id: 'notes', label: 'Notes', tab: 'notes' },
      { id: 'files', label: 'Files', tab: 'files' },
    ],
  },
] as const

export type ProjectGroupId = (typeof PROJECT_GROUPS)[number]['id']

/** group/sub from the URL → the legacy tab id that renders the content. */
export function resolveTab(group?: string, sub?: string): { group: ProjectGroupId; sub: string; tab: string } {
  const g = PROJECT_GROUPS.find(x => x.id === group) ?? PROJECT_GROUPS[0]
  const s = g.subs.find(x => x.id === sub) ?? g.subs[0]
  return { group: g.id, sub: s.id, tab: s.tab }
}

/** legacy tab id → route path under the project, for internal links. */
export function routeForTab(tab: string): string {
  for (const g of PROJECT_GROUPS)
    for (const s of g.subs) if (s.tab === tab) return `${g.id}/${s.id}`
  return 'overview/summary'
}
