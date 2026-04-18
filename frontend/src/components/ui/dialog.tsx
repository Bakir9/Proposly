import { type ReactNode } from 'react'

interface DialogProps { open: boolean; onClose: () => void; title: string; children: ReactNode }

export function Dialog({ open, onClose, title, children }: DialogProps) {
  if (!open) return null
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="fixed inset-0 bg-black/50" onClick={onClose} />
      <div className="relative z-50 bg-background border rounded-lg shadow-lg p-6 w-full max-w-lg mx-4 max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold">{title}</h2>
          <button onClick={onClose} className="text-muted-foreground hover:text-foreground text-xl leading-none">&times;</button>
        </div>
        {children}
      </div>
    </div>
  )
}
