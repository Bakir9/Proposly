import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getProjects, createProject } from '@/api/projects'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { Dialog } from '@/components/ui/dialog'
import { FolderKanban, Plus } from 'lucide-react'

const statusVariant: Record<string, 'default' | 'secondary' | 'success' | 'destructive' | 'warning' | 'outline'> = {
  Planning: 'secondary',
  Active: 'default',
  OnHold: 'warning',
  Completed: 'success',
  Cancelled: 'destructive',
}

export function ProjectsPage() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: projects, isLoading, isError } = useQuery({
    queryKey: ['projects'],
    queryFn: getProjects,
  })

  const [dialogOpen, setDialogOpen] = useState(false)
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [clientName, setClientName] = useState('')
  const [budgetAmount, setBudgetAmount] = useState('')
  const [currency, setCurrency] = useState('EUR')
  const [startDate, setStartDate] = useState('')
  const [deadline, setDeadline] = useState('')

  const mutCreate = useMutation({
    mutationFn: () => createProject({
      name,
      description: description || undefined,
      clientName,
      budgetAmount: parseFloat(budgetAmount),
      currency,
      startDate,
      deadline: deadline || undefined,
    }),
    onSuccess: (newId) => {
      queryClient.invalidateQueries({ queryKey: ['projects'] })
      setDialogOpen(false)
      setName('')
      setDescription('')
      setClientName('')
      setBudgetAmount('')
      setCurrency('EUR')
      setStartDate('')
      setDeadline('')
      navigate(`/projects/${newId}`)
    },
  })

  return (
    <div className="p-6 space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold flex items-center gap-2"><FolderKanban className="h-6 w-6" /> Projects</h1>
        <Button onClick={() => setDialogOpen(true)}><Plus className="h-4 w-4 mr-1" /> New Project</Button>
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
          <Card
            key={project.id}
            className="cursor-pointer hover:bg-muted/50 transition-colors"
            onClick={() => navigate(`/projects/${project.id}`)}
          >
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

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} title="New Project">
        <div className="space-y-4">
          <div className="space-y-1">
            <Label>Project Name</Label>
            <Input value={name} onChange={e => setName(e.target.value)} placeholder="Project name" />
          </div>
          <div className="space-y-1">
            <Label>Client Name</Label>
            <Input value={clientName} onChange={e => setClientName(e.target.value)} placeholder="Client name" />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Budget</Label>
              <Input type="number" step="0.01" value={budgetAmount} onChange={e => setBudgetAmount(e.target.value)} placeholder="0.00" />
            </div>
            <div className="space-y-1">
              <Label>Currency</Label>
              <Select value={currency} onChange={e => setCurrency(e.target.value)}>
                <option value="EUR">EUR</option>
                <option value="USD">USD</option>
                <option value="GBP">GBP</option>
                <option value="CHF">CHF</option>
              </Select>
            </div>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Start Date</Label>
              <Input type="date" value={startDate} onChange={e => setStartDate(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label>Deadline (optional)</Label>
              <Input type="date" value={deadline} onChange={e => setDeadline(e.target.value)} />
            </div>
          </div>
          <div className="space-y-1">
            <Label>Description (optional)</Label>
            <Textarea value={description} onChange={e => setDescription(e.target.value)} placeholder="Project description…" />
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setDialogOpen(false)}>Cancel</Button>
            <Button
              onClick={() => mutCreate.mutate()}
              disabled={mutCreate.isPending || !name.trim() || !clientName.trim() || !budgetAmount || !startDate}
            >
              {mutCreate.isPending ? 'Creating…' : 'Create Project'}
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}
