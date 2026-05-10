import { useQuery } from '@tanstack/react-query'
import { getVelocity, getCapacity } from '@/api/projects'
import {
  BarChart, Bar, Cell, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
} from 'recharts'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const MEMBER_COLORS = ['#3b82f6', '#22c55e', '#8b5cf6', '#f59e0b', '#ef4444', '#06b6d4', '#ec4899', '#f97316']

export function VelocityCapacityTab({ projectId }: { projectId: string }) {
  const { data: velocity, isLoading: loadingVelocity } = useQuery({
    queryKey: ['velocity', projectId],
    queryFn: () => getVelocity(projectId),
  })

  const { data: capacity, isLoading: loadingCapacity } = useQuery({
    queryKey: ['capacity', projectId],
    queryFn: () => getCapacity(projectId),
  })

  return (
    <div className="space-y-6">
      {/* Velocity */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Weekly Velocity</CardTitle>
          <p className="text-xs text-muted-foreground">Tasks completed and hours logged per week</p>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-4 mb-6">
            <div className="rounded-lg border p-4 text-center">
              <p className="text-2xl font-bold">{velocity?.totalTasksCompleted ?? '—'}</p>
              <p className="text-xs text-muted-foreground mt-1">Tasks Completed</p>
            </div>
            <div className="rounded-lg border p-4 text-center">
              <p className="text-2xl font-bold">
                {velocity ? `${velocity.totalHoursCompleted}h` : '—'}
              </p>
              <p className="text-xs text-muted-foreground mt-1">Total Hours</p>
            </div>
          </div>

          {loadingVelocity ? (
            <p className="text-sm text-muted-foreground text-center py-8">Loading...</p>
          ) : !velocity || velocity.weeks.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">
              No completed tasks yet — complete tasks to see velocity.
            </p>
          ) : (
            <ResponsiveContainer width="100%" height={280}>
              <BarChart data={velocity.weeks} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                <XAxis dataKey="weekLabel" tick={{ fontSize: 11 }} />
                <YAxis yAxisId="tasks" orientation="left" allowDecimals={false} tick={{ fontSize: 11 }} label={{ value: 'Tasks', angle: -90, position: 'insideLeft', offset: 10, style: { fontSize: 11 } }} />
                <YAxis yAxisId="hours" orientation="right" tick={{ fontSize: 11 }} label={{ value: 'Hours', angle: 90, position: 'insideRight', offset: 10, style: { fontSize: 11 } }} />
                <Tooltip formatter={(v, name) => [name === 'tasksCompleted' ? `${v} tasks` : `${v}h`, name === 'tasksCompleted' ? 'Tasks' : 'Hours']} />
                <Legend formatter={v => v === 'tasksCompleted' ? 'Tasks' : 'Hours'} />
                <Bar yAxisId="tasks" dataKey="tasksCompleted" name="tasksCompleted" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                <Bar yAxisId="hours" dataKey="hoursCompleted" name="hoursCompleted" fill="#22c55e" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </CardContent>
      </Card>

      {/* Workload */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Team Workload</CardTitle>
          <p className="text-xs text-muted-foreground">Hours logged per team member</p>
        </CardHeader>
        <CardContent>
          {loadingCapacity ? (
            <p className="text-sm text-muted-foreground text-center py-8">Loading...</p>
          ) : !capacity || capacity.members.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">
              No time logged yet — log time to see workload distribution.
            </p>
          ) : (
            <>
              <div className="rounded-lg border p-4 text-center mb-6">
                <p className="text-2xl font-bold">{capacity.totalHoursLogged}h</p>
                <p className="text-xs text-muted-foreground mt-1">Total Hours Logged</p>
              </div>
              <ResponsiveContainer width="100%" height={Math.max(180, capacity.members.length * 48)}>
                <BarChart
                  data={capacity.members}
                  layout="vertical"
                  margin={{ top: 5, right: 40, left: 10, bottom: 5 }}
                >
                  <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" horizontal={false} />
                  <XAxis type="number" tick={{ fontSize: 11 }} unit="h" />
                  <YAxis type="category" dataKey="memberName" tick={{ fontSize: 12 }} width={100} />
                  <Tooltip formatter={v => [`${v}h`, 'Hours Logged']} />
                  <Bar dataKey="hoursLogged" name="Hours Logged" radius={[0, 4, 4, 0]}>
                    {capacity.members.map((_, i) => (
                      <Cell key={i} fill={MEMBER_COLORS[i % MEMBER_COLORS.length]} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
