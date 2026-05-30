import { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getProjectById,
  updateTask,
  updateTaskStatus,
  addTaskComment,
  editTaskComment,
  deleteTaskComment,
  addTaskDependency,
  removeTaskDependency,
} from '@/api/projects'
import { useAuth } from '@/features/auth/AuthContext'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { ArrowLeft, SquareCheck, Maximize2, Minimize2, Send, Pencil, Trash2, X } from 'lucide-react'
import { RichTextEditor } from '@/components/ui/rich-text-editor'
import { toast } from 'sonner'

const TASK_STATUS_BORDER_COLOR: Record<string, string> = {
  Todo: '#94a3b8',
  InProgress: '#3b82f6',
  Done: '#22c55e',
}

function timeAgo(dateStr: string) {
  const diff = Date.now() - new Date(dateStr).getTime()
  const mins = Math.floor(diff / 60_000)
  if (mins < 1) return 'just now'
  if (mins < 60) return `${mins}m ago`
  const hours = Math.floor(mins / 60)
  if (hours < 24) return `${hours}h ago`
  const days = Math.floor(hours / 24)
  if (days < 7) return `${days}d ago`
  return new Date(dateStr).toLocaleDateString('de-AT', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

export function TaskDetailPage() {
  const { id: projectId, taskId } = useParams<{ id: string; taskId: string }>()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { user: authUser } = useAuth()

  const { data: project, isLoading, isError } = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => getProjectById(projectId!),
  })

  const task = project?.tasks.find(t => t.id === taskId)

  const [title, setTitle] = useState('')
  const [desc, setDesc] = useState('')
  const [hours, setHours] = useState('')
  const [actualHours, setActualHours] = useState('')
  const [startDate, setStartDate] = useState('')
  const [dueDate, setDueDate] = useState('')
  const [milestoneId, setMilestoneId] = useState('')
  const [assignedMemberId, setAssignedMemberId] = useState('')
  const [status, setStatus] = useState('')
  const [descExpanded, setDescExpanded] = useState(false)

  const [commentBody, setCommentBody] = useState('')
  const [editingCommentId, setEditingCommentId] = useState<string | null>(null)
  const [editingCommentBody, setEditingCommentBody] = useState('')

  const [addingDependency, setAddingDependency] = useState(false)
  const [pendingBlockerId, setPendingBlockerId] = useState('')

  useEffect(() => {
    if (!task) return
    setTitle(task.title)
    setDesc(task.description ?? '')
    setHours(task.estimatedHours?.toString() ?? '')
    setActualHours(task.actualHours?.toString() ?? '')
    setStartDate(task.startDate ? task.startDate.slice(0, 10) : '')
    setDueDate(task.dueDate ? task.dueDate.slice(0, 10) : '')
    setMilestoneId(task.milestoneId ?? '')
    setAssignedMemberId(task.assignedMemberId ?? '')
    setStatus(task.status)
  }, [task?.id]) // eslint-disable-line react-hooks/exhaustive-deps

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['project', projectId] })

  const mutSave = useMutation({
    mutationFn: async () => {
      await updateTask(projectId!, taskId!, {
        title,
        description: desc || undefined,
        estimatedHours: hours ? parseFloat(hours) : undefined,
        actualHours: actualHours && parseFloat(actualHours) > 0 ? parseFloat(actualHours) : undefined,
        startDate: startDate || undefined,
        dueDate: dueDate || undefined,
        milestoneId: milestoneId || undefined,
        assignedMemberId: assignedMemberId || undefined,
      })
      if (task && status !== task.status) {
        const ah = actualHours ? parseFloat(actualHours) : undefined
        await updateTaskStatus(projectId!, taskId!, status, ah)
      }
    },
    onSuccess: () => {
      queryClient.setQueryData<import('@/api/projects').ProjectDetail>(['project', projectId], old => {
        if (!old) return old
        const member = old.members.find(m => m.id === assignedMemberId)
        return {
          ...old,
          tasks: old.tasks.map(t => t.id !== taskId ? t : {
            ...t,
            title,
            description: desc || null,
            estimatedHours: hours ? parseFloat(hours) : null,
            actualHours: actualHours && parseFloat(actualHours) > 0 ? parseFloat(actualHours) : null,
            startDate: startDate || null,
            dueDate: dueDate || null,
            milestoneId: milestoneId || null,
            assignedMemberId: assignedMemberId || null,
            assignedMemberName: member?.name ?? null,
            status: status as import('@/api/projects').TaskStatus,
          }),
        }
      })
      toast.success('Changes saved')
      invalidate()
    },
  })

  const mutAddComment = useMutation({
    mutationFn: () => addTaskComment(projectId!, taskId!, commentBody),
    onSuccess: () => { invalidate(); setCommentBody('') },
  })

  const mutEditComment = useMutation({
    mutationFn: ({ commentId }: { commentId: string }) =>
      editTaskComment(projectId!, taskId!, commentId, editingCommentBody),
    onSuccess: () => { invalidate(); setEditingCommentId(null); setEditingCommentBody('') },
  })

  const mutDeleteComment = useMutation({
    mutationFn: ({ commentId }: { commentId: string }) =>
      deleteTaskComment(projectId!, taskId!, commentId),
    onSuccess: invalidate,
  })

  const mutAddDependency = useMutation({
    mutationFn: ({ blockingTaskId }: { blockingTaskId: string }) =>
      addTaskDependency(projectId!, taskId!, blockingTaskId),
    onSuccess: () => { invalidate(); setAddingDependency(false); setPendingBlockerId('') },
  })

  const mutRemoveDependency = useMutation({
    mutationFn: ({ blockingTaskId }: { blockingTaskId: string }) =>
      removeTaskDependency(projectId!, taskId!, blockingTaskId),
    onSuccess: invalidate,
  })

  if (isLoading) return <div className="p-6"><p className="text-muted-foreground">Loading…</p></div>
  if (isError || !project) return <div className="p-6"><p className="text-destructive">Failed to load project.</p></div>
  if (!task) return <div className="p-6"><p className="text-destructive">Task not found.</p></div>

  const liveTask = project.tasks.find(t => t.id === taskId)!

  return (
    <div className="min-h-screen flex flex-col">
      {/* Page header */}
      <div
        className="border-b border-l-4 bg-muted/30"
        style={{ borderLeftColor: TASK_STATUS_BORDER_COLOR[status] ?? '#3b82f6' }}
      >
        <div className="max-w-6xl mx-auto px-6 py-3 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3 min-w-0">
            <button
              onClick={() => navigate(`/projects/${projectId}`)}
              className="flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors shrink-0"
            >
              <ArrowLeft className="h-4 w-4" />
              {project.name}
            </button>
            <span className="text-muted-foreground/40">/</span>
            <span className="inline-flex items-center gap-1.5 text-xs font-medium text-blue-600 bg-blue-100 dark:bg-blue-950 dark:text-blue-400 px-2 py-1 rounded shrink-0">
              <SquareCheck className="h-3.5 w-3.5" /> Task
            </span>
            <span className="text-sm text-muted-foreground truncate hidden sm:block">{task.title}</span>
          </div>
          <Button
            size="sm"
            onClick={() => mutSave.mutate()}
            disabled={mutSave.isPending || !title.trim() || (status === 'Done' && (!actualHours || parseFloat(actualHours) <= 0))}
          >
            {mutSave.isPending ? 'Saving…' : 'Save'}
          </Button>
        </div>
      </div>

      {/* Body */}
      <div className="flex flex-1 max-w-6xl mx-auto w-full px-6 py-6 gap-6 min-h-0">
        {/* Left: main content */}
        <div className={`flex-1 flex flex-col min-w-0 gap-6 ${descExpanded ? 'overflow-hidden' : ''}`}>
          {/* Title */}
          <input
            value={title}
            onChange={e => setTitle(e.target.value)}
            placeholder="Task title"
            className="w-full text-2xl font-semibold bg-transparent border-0 border-b border-transparent hover:border-border focus:border-primary focus:outline-none py-1 transition-colors placeholder:text-muted-foreground/50"
          />

          {/* Description */}
          <div className={`flex flex-col gap-2 ${descExpanded ? 'flex-1 min-h-0' : ''}`}>
            <div className="flex items-center justify-between shrink-0">
              <p className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">Description</p>
              <button
                onClick={() => setDescExpanded(v => !v)}
                title={descExpanded ? 'Collapse description' : 'Expand description'}
                className="text-muted-foreground hover:text-foreground p-1 rounded hover:bg-muted transition-colors"
              >
                {descExpanded ? <Minimize2 className="h-3.5 w-3.5" /> : <Maximize2 className="h-3.5 w-3.5" />}
              </button>
            </div>
            <RichTextEditor
              value={desc}
              onChange={setDesc}
              placeholder="Add a description…"
              className={descExpanded ? 'flex-1 min-h-0' : ''}
              editorClassName={descExpanded ? 'min-h-0' : 'min-h-[200px]'}
            />
          </div>

          {/* Discussion */}
          {!descExpanded && (
            <div className="space-y-4">
              <div className="flex items-center gap-2 border-b pb-2">
                <p className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">Discussion</p>
                {liveTask.comments.length > 0 && (
                  <span className="text-xs text-muted-foreground">({liveTask.comments.length})</span>
                )}
              </div>

              <div className="flex gap-3 items-start">
                <span className="inline-flex items-center justify-center w-7 h-7 rounded-full bg-primary/15 text-primary font-semibold text-xs shrink-0 mt-0.5">
                  {authUser?.fullName?.charAt(0).toUpperCase() ?? '?'}
                </span>
                <div className="flex-1 space-y-2">
                  <Textarea
                    value={commentBody}
                    onChange={e => setCommentBody(e.target.value)}
                    placeholder="Add a comment… (Ctrl+Enter to post)"
                    rows={2}
                    className="resize-none"
                    onKeyDown={e => {
                      if (e.key === 'Enter' && (e.ctrlKey || e.metaKey) && commentBody.trim()) {
                        e.preventDefault()
                        mutAddComment.mutate()
                      }
                    }}
                  />
                  {commentBody.trim() && (
                    <div className="flex justify-end">
                      <Button size="sm" onClick={() => mutAddComment.mutate()} disabled={mutAddComment.isPending}>
                        <Send className="h-3.5 w-3.5 mr-1.5" />
                        {mutAddComment.isPending ? 'Posting…' : 'Comment'}
                      </Button>
                    </div>
                  )}
                </div>
              </div>

              <div className="space-y-5">
                {liveTask.comments.length === 0 && (
                  <p className="text-sm text-muted-foreground">No comments yet. Be the first to add one.</p>
                )}
                {liveTask.comments.map(comment => (
                  <div key={comment.id} className="flex gap-3 group">
                    <span className="inline-flex items-center justify-center w-7 h-7 rounded-full bg-muted font-semibold text-xs shrink-0 mt-0.5">
                      {comment.authorName.charAt(0).toUpperCase()}
                    </span>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium">{comment.authorName}</span>
                        <span className="text-xs text-muted-foreground">
                          {timeAgo(comment.createdAt)}{comment.updatedAt ? ' · edited' : ''}
                        </span>
                        <div className="ml-auto flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                          {comment.authorId === authUser?.userId && editingCommentId !== comment.id && (
                            <button
                              className="text-muted-foreground hover:text-foreground p-1 rounded hover:bg-muted"
                              onClick={() => { setEditingCommentId(comment.id); setEditingCommentBody(comment.body) }}
                            >
                              <Pencil className="h-3 w-3" />
                            </button>
                          )}
                          {(comment.authorId === authUser?.userId || authUser?.role === 'Owner' || authUser?.role === 'Admin') && editingCommentId !== comment.id && (
                            <button
                              className="text-muted-foreground hover:text-destructive p-1 rounded hover:bg-muted"
                              onClick={() => mutDeleteComment.mutate({ commentId: comment.id })}
                              disabled={mutDeleteComment.isPending}
                            >
                              <Trash2 className="h-3 w-3" />
                            </button>
                          )}
                        </div>
                      </div>
                      {editingCommentId === comment.id ? (
                        <div className="mt-1.5 space-y-2">
                          <Textarea
                            value={editingCommentBody}
                            onChange={e => setEditingCommentBody(e.target.value)}
                            rows={2}
                            className="resize-none"
                          />
                          <div className="flex gap-2">
                            <Button size="sm" onClick={() => mutEditComment.mutate({ commentId: comment.id })} disabled={mutEditComment.isPending || !editingCommentBody.trim()}>
                              {mutEditComment.isPending ? 'Saving…' : 'Save'}
                            </Button>
                            <Button size="sm" variant="outline" onClick={() => { setEditingCommentId(null); setEditingCommentBody('') }}>Cancel</Button>
                          </div>
                        </div>
                      ) : (
                        <p className="text-sm mt-1 whitespace-pre-wrap break-words text-foreground/90">{comment.body}</p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Right: details sidebar */}
        <div className="w-64 shrink-0 border-l pl-6 space-y-5">
          <p className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">Details</p>

          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground font-normal">State</Label>
            <Select value={status} onChange={e => setStatus(e.target.value)}>
              <option value="Todo">To Do</option>
              <option value="InProgress">In Progress</option>
              <option value="Done">Done</option>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground font-normal">Assigned To</Label>
            <Select value={assignedMemberId} onChange={e => setAssignedMemberId(e.target.value)}>
              <option value="">Unassigned</option>
              {project.members.map(m => <option key={m.id} value={m.id}>{m.name}</option>)}
            </Select>
          </div>

          {project.milestones.length > 0 && (
            <div className="space-y-1.5">
              <Label className="text-xs text-muted-foreground font-normal">Milestone</Label>
              <Select value={milestoneId} onChange={e => setMilestoneId(e.target.value)}>
                <option value="">None</option>
                {project.milestones.map(m => <option key={m.id} value={m.id}>{m.title}</option>)}
              </Select>
            </div>
          )}

          <div className="w-full h-px bg-border" />

          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground font-normal">Estimated Hours</Label>
            <Input type="number" step="0.5" min="0" value={hours} onChange={e => setHours(e.target.value)} placeholder="—" />
          </div>

          <div className="space-y-1.5">
            <Label className={`text-xs font-normal ${status === 'Done' && (!actualHours || parseFloat(actualHours) <= 0) ? 'text-red-500' : 'text-muted-foreground'}`}>
              Actual Hours{status === 'Done' ? ' *' : ''}
            </Label>
            <Input
              type="number"
              step="0.5"
              min="0"
              value={actualHours}
              onChange={e => setActualHours(e.target.value)}
              placeholder="—"
              className={status === 'Done' && (!actualHours || parseFloat(actualHours) <= 0) ? 'border-red-500 focus-visible:ring-red-500' : ''}
            />
            {status === 'Done' && (!actualHours || parseFloat(actualHours) <= 0) && (
              <p className="text-xs text-red-500">Required to mark as done</p>
            )}
          </div>

          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground font-normal">Start Date</Label>
            <Input type="date" value={startDate} onChange={e => setStartDate(e.target.value)} />
          </div>

          <div className="space-y-1.5">
            <Label className="text-xs text-muted-foreground font-normal">Due Date</Label>
            <Input type="date" value={dueDate} onChange={e => setDueDate(e.target.value)} />
          </div>

          <div className="w-full h-px bg-border" />

          {/* Dependencies */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label className="text-xs text-muted-foreground font-normal">Blocked By</Label>
              {!addingDependency && (
                <button
                  className="text-xs text-muted-foreground hover:text-foreground underline"
                  onClick={() => setAddingDependency(true)}
                >
                  + Add
                </button>
              )}
            </div>

            {(liveTask.blockedByTaskIds ?? []).map(blockerId => {
              const blocker = project.tasks.find(t => t.id === blockerId)
              if (!blocker) return null
              return (
                <div key={blockerId} className="flex items-center justify-between gap-1 text-xs bg-muted/50 rounded px-2 py-1">
                  <span className="flex items-center gap-1 truncate">
                    <span className={`w-1.5 h-1.5 rounded-full shrink-0 ${blocker.status === 'Done' ? 'bg-green-500' : 'bg-amber-400'}`} />
                    <span className="truncate">{blocker.title}</span>
                  </span>
                  <button
                    className="text-muted-foreground hover:text-destructive shrink-0"
                    onClick={() => mutRemoveDependency.mutate({ blockingTaskId: blockerId })}
                    disabled={mutRemoveDependency.isPending}
                  >
                    <X className="h-3 w-3" />
                  </button>
                </div>
              )
            })}

            {addingDependency && (
              <div className="space-y-1.5">
                <Select
                  value={pendingBlockerId}
                  onChange={e => setPendingBlockerId(e.target.value)}
                  className="text-xs"
                >
                  <option value="">Select a task…</option>
                  {project.tasks
                    .filter(t =>
                      t.id !== taskId &&
                      !(liveTask.blockedByTaskIds ?? []).includes(t.id)
                    )
                    .map(t => (
                      <option key={t.id} value={t.id}>{t.title}</option>
                    ))
                  }
                </Select>
                <div className="flex gap-1">
                  <Button
                    size="sm"
                    className="flex-1 text-xs h-7"
                    onClick={() => pendingBlockerId && mutAddDependency.mutate({ blockingTaskId: pendingBlockerId })}
                    disabled={!pendingBlockerId || mutAddDependency.isPending}
                  >
                    {mutAddDependency.isPending ? '…' : 'Add'}
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    className="text-xs h-7"
                    onClick={() => { setAddingDependency(false); setPendingBlockerId('') }}
                  >
                    Cancel
                  </Button>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
