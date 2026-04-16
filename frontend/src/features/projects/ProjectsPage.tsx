import { useQuery } from '@tanstack/react-query'
import { getProjects } from '@/api/projects'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const statusVariant: Record<string, 'default' | 'secondary' | 'success' | 'destructive' | 'warning' | 'outline'> = {
  Planning: 'secondary',
  Active: 'default',
  OnHold: 'warning',
  Completed: 'success',
  Cancelled: 'destructive',
}

export function ProjectsPage() {
  const { data: projects, isLoading, isError } = useQuery({
    queryKey: ['projects'],
    queryFn: getProjects,
  })

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Projects</h1>
      </div>

      {isLoading && <p className="text-muted-foreground">Loading…</p>}
      {isError && <p className="text-destructive">Failed to load projects.</p>}

      {projects && projects.length === 0 && (
        <Card>
          <CardContent className="py-10 text-center text-muted-foreground">
            No projects yet.
          </CardContent>
        </Card>
      )}

      <div className="grid gap-3">
        {projects?.map(project => (
          <Card key={project.id}>
            <CardHeader className="pb-2">
              <div className="flex items-start justify-between">
                <div>
                  <CardTitle className="text-base">{project.name}</CardTitle>
                  <p className="text-sm text-muted-foreground mt-0.5">{project.clientName}</p>
                </div>
                <Badge variant={statusVariant[project.status] ?? 'outline'}>{project.status}</Badge>
              </div>
            </CardHeader>
            <CardContent>
              <div className="flex items-center gap-6 text-sm text-muted-foreground">
                <span className="font-medium text-foreground">
                  {project.budgetAmount.toLocaleString('de-AT', { style: 'currency', currency: project.currency })}
                </span>
                <span>{project.memberCount} member{project.memberCount !== 1 ? 's' : ''}</span>
                <span>From {new Date(project.startDate).toLocaleDateString()}</span>
                {project.deadline && <span>Due {new Date(project.deadline).toLocaleDateString()}</span>}
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}
