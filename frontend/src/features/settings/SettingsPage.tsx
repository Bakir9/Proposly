import { useState, useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { useTheme } from '@/context/ThemeContext'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Moon, Sun } from 'lucide-react'
import { getCompanySettings, updateCompanySettings } from '@/api/settings'

const EU_COUNTRIES = [
  { code: 'AT', name: 'Austria' }, { code: 'BE', name: 'Belgium' }, { code: 'BG', name: 'Bulgaria' },
  { code: 'CY', name: 'Cyprus' }, { code: 'CZ', name: 'Czech Republic' }, { code: 'DE', name: 'Germany' },
  { code: 'DK', name: 'Denmark' }, { code: 'EE', name: 'Estonia' }, { code: 'ES', name: 'Spain' },
  { code: 'FI', name: 'Finland' }, { code: 'FR', name: 'France' }, { code: 'GR', name: 'Greece' },
  { code: 'HR', name: 'Croatia' }, { code: 'HU', name: 'Hungary' }, { code: 'IE', name: 'Ireland' },
  { code: 'IT', name: 'Italy' }, { code: 'LT', name: 'Lithuania' }, { code: 'LU', name: 'Luxembourg' },
  { code: 'LV', name: 'Latvia' }, { code: 'MT', name: 'Malta' }, { code: 'NL', name: 'Netherlands' },
  { code: 'PL', name: 'Poland' }, { code: 'PT', name: 'Portugal' }, { code: 'RO', name: 'Romania' },
  { code: 'SE', name: 'Sweden' }, { code: 'SI', name: 'Slovenia' }, { code: 'SK', name: 'Slovakia' },
]

const MONTH_NAMES = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

export function SettingsPage() {
  const { theme, toggleTheme } = useTheme()
  const isDark = theme === 'dark'
  const queryClient = useQueryClient()

  const { data: settings } = useQuery({
    queryKey: ['company-settings'],
    queryFn: getCompanySettings,
  })

  const [fiscalMonth, setFiscalMonth] = useState(1)
  const [companyCountry, setCompanyCountry] = useState<string>('')
  const [isVatRegistered, setIsVatRegistered] = useState(false)
  const [companyVatNumber, setCompanyVatNumber] = useState('')
  const [defaultVatRate, setDefaultVatRate] = useState('')
  const [isVatExempt, setIsVatExempt] = useState(false)
  const [vatExemptReason, setVatExemptReason] = useState('')

  useEffect(() => {
    if (settings) {
      setFiscalMonth(settings.fiscalYearStartMonth)
      setCompanyCountry(settings.companyCountry ?? '')
      setIsVatRegistered(settings.isVatRegistered)
      setCompanyVatNumber(settings.companyVatNumber ?? '')
      setDefaultVatRate(settings.defaultVatRate > 0 ? String(settings.defaultVatRate) : '')
      setIsVatExempt(settings.isVatExempt)
      setVatExemptReason(settings.vatExemptReason ?? '')
    }
  }, [settings])

  const { mutate: saveSettings, isPending } = useMutation({
    mutationFn: () => updateCompanySettings({
      fiscalYearStartMonth: fiscalMonth,
      companyCountry: companyCountry || null,
      isVatRegistered,
      companyVatNumber: companyVatNumber || null,
      defaultVatRate: parseFloat(defaultVatRate) || 0,
      isVatExempt,
      vatExemptReason: vatExemptReason || null,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['company-settings'] })
      queryClient.invalidateQueries({ queryKey: ['quarterly-report'] })
      toast.success('Settings saved.')
    },
    onError: () => toast.error('Failed to save settings.'),
  })

  const hasChanges = settings
    ? settings.fiscalYearStartMonth !== fiscalMonth
      || (settings.companyCountry ?? '') !== companyCountry
      || settings.isVatRegistered !== isVatRegistered
      || (settings.companyVatNumber ?? '') !== companyVatNumber
      || String(settings.defaultVatRate) !== (defaultVatRate || '0')
      || settings.isVatExempt !== isVatExempt
      || (settings.vatExemptReason ?? '') !== vatExemptReason
    : false

  return (
    <div className="p-6 space-y-6 max-w-2xl">
      <h1 className="text-2xl font-semibold">Settings</h1>

      {/* Appearance */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Appearance</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium">Theme</p>
              <p className="text-sm text-muted-foreground mt-0.5">
                {isDark ? 'Dark mode is on' : 'Light mode is on'}
              </p>
            </div>
            <button
              onClick={toggleTheme}
              className="relative inline-flex h-10 w-20 items-center rounded-full border transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              style={{ backgroundColor: isDark ? 'hsl(var(--primary))' : 'hsl(var(--muted))' }}
              aria-label="Toggle theme"
            >
              <span
                className="inline-flex h-8 w-8 items-center justify-center rounded-full bg-background shadow-sm transition-transform duration-200"
                style={{ transform: isDark ? 'translateX(40px)' : 'translateX(4px)' }}
              >
                {isDark ? (
                  <Moon className="h-4 w-4 text-primary" />
                ) : (
                  <Sun className="h-4 w-4 text-muted-foreground" />
                )}
              </span>
            </button>
          </div>
        </CardContent>
      </Card>

      {/* Fiscal Year */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Fiscal Year</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-medium">Fiscal year starts in</label>
            <p className="text-sm text-muted-foreground">
              All quarterly reports are calculated from this month. Q1 begins on the 1st of the selected month.
            </p>
            <select
              value={fiscalMonth}
              onChange={e => setFiscalMonth(Number(e.target.value))}
              className="mt-1 h-9 w-48 rounded-md border bg-background px-3 text-sm"
            >
              {MONTH_NAMES.map((name, i) => (
                <option key={i + 1} value={i + 1}>{name}</option>
              ))}
            </select>
          </div>

          {fiscalMonth !== 1 && (
            <div className="rounded-md bg-muted px-4 py-3 text-xs text-muted-foreground">
              <p className="font-medium text-foreground mb-1">Quarters for FY starting in {MONTH_NAMES[fiscalMonth - 1]}</p>
              {[1, 2, 3, 4].map(q => {
                const offset = (q - 1) * 3
                const m1 = MONTH_NAMES[(fiscalMonth - 1 + offset) % 12]
                const m3 = MONTH_NAMES[(fiscalMonth - 1 + offset + 2) % 12]
                return <p key={q}>Q{q}: {m1} – {m3}</p>
              })}
            </div>
          )}

          <Button
            size="sm"
            onClick={() => saveSettings()}
            disabled={!hasChanges || isPending}
          >
            {isPending ? 'Saving…' : 'Save'}
          </Button>
        </CardContent>
      </Card>

      {/* VAT Settings */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">VAT Settings</CardTitle>
        </CardHeader>
        <CardContent className="space-y-5">
          {/* Company country */}
          <div className="space-y-1.5">
            <Label>Company Country</Label>
            <p className="text-sm text-muted-foreground">Used to determine domestic vs. international VAT rules.</p>
            <select
              value={companyCountry}
              onChange={e => setCompanyCountry(e.target.value)}
              className="mt-1 h-9 w-56 rounded-md border bg-background px-3 text-sm"
            >
              <option value="">— Not set —</option>
              {EU_COUNTRIES.map(c => (
                <option key={c.code} value={c.code}>{c.name} ({c.code})</option>
              ))}
            </select>
          </div>

          {/* VAT registration */}
          <div className="space-y-3">
            <Label>VAT Registration</Label>
            <div className="flex gap-4">
              <label className="flex items-center gap-2 cursor-pointer text-sm">
                <input
                  type="radio"
                  checked={!isVatRegistered}
                  onChange={() => setIsVatRegistered(false)}
                  className="accent-primary"
                />
                Not VAT registered
              </label>
              <label className="flex items-center gap-2 cursor-pointer text-sm">
                <input
                  type="radio"
                  checked={isVatRegistered}
                  onChange={() => setIsVatRegistered(true)}
                  className="accent-primary"
                />
                VAT registered
              </label>
            </div>

            {isVatRegistered && (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-1">
                <div className="space-y-1.5">
                  <Label htmlFor="vat-number">VAT Number</Label>
                  <Input
                    id="vat-number"
                    value={companyVatNumber}
                    onChange={e => setCompanyVatNumber(e.target.value)}
                    placeholder="e.g. ATU12345678"
                    className="w-56"
                  />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="vat-rate">Default VAT Rate (%)</Label>
                  <Input
                    id="vat-rate"
                    type="number"
                    min="0"
                    max="100"
                    step="0.01"
                    value={defaultVatRate}
                    onChange={e => setDefaultVatRate(e.target.value)}
                    placeholder="e.g. 20"
                    className="w-32"
                  />
                </div>
              </div>
            )}
          </div>

          {/* VAT exempt */}
          <div className="space-y-2">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={isVatExempt}
                onChange={e => setIsVatExempt(e.target.checked)}
                className="rounded accent-primary"
              />
              <span className="text-sm font-medium">Company is VAT exempt</span>
            </label>
            {isVatExempt && (
              <div className="space-y-1.5 pl-6">
                <Label htmlFor="vat-exempt-reason">Exemption reason (shown on offers)</Label>
                <Input
                  id="vat-exempt-reason"
                  value={vatExemptReason}
                  onChange={e => setVatExemptReason(e.target.value)}
                  placeholder="e.g. Kein Umsatzsteuer gemäß § 6 Abs. 1 Z 27 UStG"
                  className="w-full"
                />
              </div>
            )}
          </div>

          {/* Preview */}
          {isVatRegistered && companyCountry && (
            <div className="rounded-md bg-muted px-4 py-3 text-xs text-muted-foreground space-y-1">
              <p className="font-medium text-foreground mb-1">VAT Rules Preview</p>
              {isVatExempt ? (
                <p>All offers: VAT exempt</p>
              ) : (
                <>
                  <p>→ Clients in <strong>{companyCountry}</strong>: VAT {defaultVatRate || 0}% (domestic)</p>
                  <p>→ EU clients with VAT number: 0% Reverse Charge</p>
                  <p>→ EU clients without VAT number: their country's rate</p>
                  <p>→ Non-EU clients: 0% (Export)</p>
                </>
              )}
            </div>
          )}

          <Button
            size="sm"
            onClick={() => saveSettings()}
            disabled={!hasChanges || isPending}
          >
            {isPending ? 'Saving…' : 'Save VAT Settings'}
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}
