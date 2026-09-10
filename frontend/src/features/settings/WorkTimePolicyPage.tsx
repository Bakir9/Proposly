import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import axios from 'axios'
import { toast } from 'sonner'
import {
  getWorkTimePolicy, getPolicyDefaults, createWorkTimePolicy,
  type WorkTimePolicy, type CreateWorkTimePolicyRequest,
} from '@/api/worktime-settings'
import { getApiErrorMessage } from '@/lib/api-errors'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { ShieldCheck, Info } from 'lucide-react'

const JURISDICTIONS = [
  { value: 'AT', label: 'Austria' },
  { value: 'DE', label: 'Germany' },
]

function today() {
  return new Date().toISOString().slice(0, 10)
}

type Form = Omit<CreateWorkTimePolicyRequest, 'breakRules'> & {
  breakRules: { aboveHours: number; minBreakMinutes: number }[]
}

function toForm(policy: WorkTimePolicy, validFrom: string): Form {
  return {
    validFrom,
    jurisdiction: policy.jurisdiction,
    holidayRegionCode: policy.holidayRegionCode,
    maxHoursPerDay: policy.maxHoursPerDay,
    maxHoursPerWeek: policy.maxHoursPerWeek,
    averagingWindowWeeks: policy.averagingWindowWeeks,
    maxAverageHoursPerWeek: policy.maxAverageHoursPerWeek,
    minDailyRestHours: policy.minDailyRestHours,
    minWeeklyRestHours: policy.minWeeklyRestHours,
    surplusCapHours: policy.surplusCapHours,
    deficitFloorHours: policy.deficitFloorHours,
    breakRules: policy.breakRules.map(r => ({ ...r })),
  }
}

export function WorkTimePolicyPage() {
  const queryClient = useQueryClient()

  // Held only once the user has edited or loaded defaults; otherwise the form is derived from the
  // saved policy, which avoids syncing state in an effect.
  const [draft, setDraft] = useState<Form | null>(null)

  const { data: policy, isLoading, error } = useQuery({
    queryKey: ['worktime', 'policy'],
    queryFn: getWorkTimePolicy,
    retry: false,
  })

  // A 404 is the documented "no rule set configured" signal, not a failure.
  const notConfigured = axios.isAxiosError(error) && error.response?.status === 404

  const form = draft ?? (policy ? toForm(policy, today()) : null)

  const loadDefaults = useMutation({
    mutationFn: (jurisdiction: string) => getPolicyDefaults(jurisdiction),
    onSuccess: (defaults) => {
      setDraft(toForm(defaults, today()))
      toast.success(`Loaded ${defaults.jurisdiction} defaults. Review them before saving.`)
    },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  const save = useMutation({
    mutationFn: (data: CreateWorkTimePolicyRequest) => createWorkTimePolicy(data),
    onSuccess: () => {
      toast.success('New policy version saved. It applies from its start date onward.')
      queryClient.invalidateQueries({ queryKey: ['worktime'] })
      queryClient.invalidateQueries({ queryKey: ['timesheet'] })
      queryClient.invalidateQueries({ queryKey: ['timesheets'] })
    },
    onError: (e) => toast.error(getApiErrorMessage(e)),
  })

  // Seeds the draft from whatever is currently shown, so the first edit does not lose the rest.
  const set = <K extends keyof Form>(key: K, value: Form[K]) =>
    setDraft(current => {
      const base = current ?? form
      return base ? { ...base, [key]: value } : base
    })

  if (isLoading) return <div className="p-6 text-sm text-muted-foreground">Loading…</div>

  return (
    <div className="p-6 space-y-6 max-w-3xl">
      <div>
        <h1 className="text-2xl font-bold tracking-tight flex items-center gap-2">
          <ShieldCheck className="h-6 w-6" />
          Working time rules
        </h1>
        <p className="text-sm text-muted-foreground mt-1">
          The limits recorded working time is checked against, and the bounds on carried overtime.
        </p>
      </div>

      {notConfigured && !form && (
        <div className="rounded-lg border border-amber-500/30 bg-amber-500/5 p-4 space-y-3">
          <p className="text-sm font-medium text-amber-500">No rule set is active</p>
          <p className="text-sm text-muted-foreground">
            Until you configure one, recorded working time is not checked against any limit.
            Start from the defaults for your country and adjust them to your agreement.
          </p>
          <div className="flex gap-2">
            {JURISDICTIONS.map(j => (
              <Button
                key={j.value}
                variant="outline"
                size="sm"
                disabled={loadDefaults.isPending}
                onClick={() => loadDefaults.mutate(j.value)}
              >
                Load {j.label} defaults
              </Button>
            ))}
          </div>
        </div>
      )}

      {policy && (
        <div className="rounded-lg border p-4 text-sm space-y-1">
          <p className="font-medium">
            Currently in force from {policy.validFrom}
            {policy.validTo ? ` to ${policy.validTo}` : ''} · {policy.jurisdiction}
          </p>
          {policy.history.length > 1 && (
            <p className="text-muted-foreground text-xs">
              {policy.history.length} versions on record. Saving creates a new one; earlier
              versions are kept so reported months keep the rules they were judged against.
            </p>
          )}
        </div>
      )}

      {form && (
        <div className="space-y-6">
          <div className="rounded-md border border-blue-500/30 bg-blue-500/5 p-3 flex gap-2 text-xs text-muted-foreground">
            <Info className="h-4 w-4 shrink-0 text-blue-400" />
            <span>
              These defaults are a starting point, not legal advice. Collective and company
              agreements are often stricter, and legislation changes — confirm the values against
              your own agreement.
            </span>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label>Applies from</Label>
              <Input
                type="date" value={form.validFrom}
                onChange={e => set('validFrom', e.target.value)}
              />
              <p className="text-xs text-muted-foreground">
                Must be later than the current version's start date.
              </p>
            </div>

            <div className="space-y-1.5">
              <Label>Jurisdiction</Label>
              <Select
                value={form.jurisdiction}
                onChange={e => set('jurisdiction', e.target.value)}
              >
                {JURISDICTIONS.map(j => (
                  <option key={j.value} value={j.value}>{j.label}</option>
                ))}
              </Select>
            </div>

            <NumberField
              label="Max hours per day" value={form.maxHoursPerDay}
              onChange={v => set('maxHoursPerDay', v)}
            />
            <NumberField
              label="Max hours per week" value={form.maxHoursPerWeek}
              onChange={v => set('maxHoursPerWeek', v)}
            />
            <NumberField
              label="Averaging window (weeks)" value={form.averagingWindowWeeks} step={1}
              onChange={v => set('averagingWindowWeeks', v)}
            />
            <NumberField
              label="Max average hours per week" value={form.maxAverageHoursPerWeek}
              onChange={v => set('maxAverageHoursPerWeek', v)}
            />
            <NumberField
              label="Min rest between days (h)" value={form.minDailyRestHours}
              onChange={v => set('minDailyRestHours', v)}
            />
            <NumberField
              label="Min weekly rest (h)" value={form.minWeeklyRestHours}
              onChange={v => set('minWeeklyRestHours', v)}
            />
          </div>

          <div>
            <h2 className="text-sm font-semibold mb-2">Break minimums</h2>
            <p className="text-xs text-muted-foreground mb-3">
              Above the given hours of working time, at least this much break is required. The
              highest matching tier applies.
            </p>
            <div className="space-y-2">
              {form.breakRules.map((rule, i) => (
                <div key={i} className="flex items-end gap-2">
                  <NumberField
                    label="Above (h)" value={rule.aboveHours}
                    onChange={v => set('breakRules', form.breakRules.map((r, j) =>
                      j === i ? { ...r, aboveHours: v } : r))}
                  />
                  <NumberField
                    label="Min break (min)" value={rule.minBreakMinutes} step={1}
                    onChange={v => set('breakRules', form.breakRules.map((r, j) =>
                      j === i ? { ...r, minBreakMinutes: v } : r))}
                  />
                  <Button
                    variant="ghost" size="sm"
                    onClick={() => set('breakRules', form.breakRules.filter((_, j) => j !== i))}
                  >
                    Remove
                  </Button>
                </div>
              ))}
              <Button
                variant="outline" size="sm"
                onClick={() => set('breakRules', [...form.breakRules, { aboveHours: 0, minBreakMinutes: 30 }])}
              >
                Add tier
              </Button>
            </div>
          </div>

          <div>
            <h2 className="text-sm font-semibold mb-2">Flexitime balance</h2>
            <p className="text-xs text-muted-foreground mb-3">
              Bounds on the balance carried between months. Leave blank for unbounded. Surplus
              above the cap is forfeited, and always shown explicitly on the month-end report.
            </p>
            <div className="grid grid-cols-2 gap-4">
              <NullableNumberField
                label="Surplus cap (h)" value={form.surplusCapHours ?? null}
                onChange={v => set('surplusCapHours', v)}
              />
              <NullableNumberField
                label="Deficit floor (h, negative)" value={form.deficitFloorHours ?? null}
                onChange={v => set('deficitFloorHours', v)}
              />
            </div>
          </div>

          <div className="flex justify-end">
            <Button disabled={save.isPending} onClick={() => save.mutate(form)}>
              Save as new version
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

function NumberField({
  label, value, onChange, step = 0.5,
}: { label: string; value: number; onChange: (v: number) => void; step?: number }) {
  return (
    <div className="space-y-1.5">
      <Label>{label}</Label>
      <Input
        type="number" step={step} value={value}
        onChange={e => onChange(Number(e.target.value))}
      />
    </div>
  )
}

function NullableNumberField({
  label, value, onChange,
}: { label: string; value: number | null; onChange: (v: number | null) => void }) {
  return (
    <div className="space-y-1.5">
      <Label>{label}</Label>
      <Input
        type="number" step={0.5} value={value ?? ''} placeholder="unbounded"
        onChange={e => onChange(e.target.value === '' ? null : Number(e.target.value))}
      />
    </div>
  )
}
