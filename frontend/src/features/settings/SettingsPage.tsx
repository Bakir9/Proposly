import { useTheme } from '@/context/ThemeContext'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Moon, Sun } from 'lucide-react'

export function SettingsPage() {
  const { theme, toggleTheme } = useTheme()
  const isDark = theme === 'dark'

  return (
    <div className="p-6 space-y-6 max-w-2xl">
      <h1 className="text-2xl font-semibold">Settings</h1>

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
    </div>
  )
}
