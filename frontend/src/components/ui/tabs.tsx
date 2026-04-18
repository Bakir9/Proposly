import { type ReactNode, useState } from 'react'
import { cn } from '@/lib/utils'

interface Tab { id: string; label: string }
interface TabsProps { tabs: Tab[]; children: (activeTab: string) => ReactNode; defaultTab?: string }

export function Tabs({ tabs, children, defaultTab }: TabsProps) {
  const [active, setActive] = useState(defaultTab ?? tabs[0]?.id ?? '')
  return (
    <div>
      <div className="flex gap-1 border-b mb-4">
        {tabs.map(tab => (
          <button
            key={tab.id}
            onClick={() => setActive(tab.id)}
            className={cn('px-4 py-2 text-sm font-medium -mb-px border-b-2 transition-colors',
              active === tab.id ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground')}
          >
            {tab.label}
          </button>
        ))}
      </div>
      {children(active)}
    </div>
  )
}
