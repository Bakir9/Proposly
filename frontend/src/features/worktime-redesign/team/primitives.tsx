import type { ReactNode } from 'react'

/* Small building blocks shared by the Team page tabs — matching the handoff's visual language. */

export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <div className={`border border-[color:var(--wtr-border)] rounded-[14px] bg-[color:var(--wtr-card)] overflow-hidden ${className}`}>
      {children}
    </div>
  )
}

export function CardHeader({
  title,
  sub,
  action,
}: {
  title: string
  sub?: string
  action?: ReactNode
}) {
  return (
    <div className="px-5 py-4 flex items-center justify-between gap-3 border-b border-[color:var(--wtr-border)] flex-wrap">
      <div className="flex flex-col gap-1">
        <h2 className="m-0 text-[15px] font-semibold">{title}</h2>
        {sub && <p className="m-0 text-[13px] text-[color:var(--wtr-text-sec)]">{sub}</p>}
      </div>
      {action}
    </div>
  )
}

export function Field({
  label,
  hint,
  children,
}: {
  label: string
  hint?: string
  children: ReactNode
}) {
  return (
    <label className="flex flex-col gap-1.5">
      <span className="text-[12.5px] font-medium text-[color:var(--wtr-text-sec)]">
        {label}
        {hint && <span className="text-[color:var(--wtr-label)]"> · {hint}</span>}
      </span>
      {children}
    </label>
  )
}

export const INPUT = 'wtr-input h-[38px] w-full px-3 text-sm'
export const INPUT_MONO = `${INPUT} wtr-mono`

/** Segmented control (type of work, model, overtime, note tags). */
export function Segmented<T extends string>({
  options,
  value,
  onChange,
  className = '',
}: {
  options: readonly T[]
  value: T
  onChange: (value: T) => void
  className?: string
}) {
  return (
    <div className={`flex gap-1 bg-[color:var(--wtr-head)] border border-[color:var(--wtr-border2)] rounded-[10px] p-1 flex-wrap ${className}`}>
      {options.map(o => (
        <button
          key={o}
          onClick={() => onChange(o)}
          className="h-8 px-3.5 rounded-[7px] border-0 text-[13.5px] font-medium cursor-pointer whitespace-nowrap"
          style={{
            background: o === value ? 'var(--wtr-active-bg)' : 'transparent',
            color: o === value ? 'var(--wtr-active-fg)' : 'var(--wtr-text-sec)',
          }}
        >
          {o}
        </button>
      ))}
    </div>
  )
}

export function Toggle({ on, onClick }: { on: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      aria-pressed={on}
      className="w-[42px] h-6 rounded-full border-0 p-[3px] cursor-pointer flex-none"
      style={{ background: on ? 'var(--wtr-accent)' : 'var(--wtr-border2)' }}
    >
      <span
        className="block w-[18px] h-[18px] rounded-full transition-transform duration-150"
        style={{ background: on ? 'var(--wtr-on-accent)' : 'var(--wtr-label)', transform: `translateX(${on ? 18 : 0}px)` }}
      />
    </button>
  )
}

export function AddButton({ label = 'Add', onClick }: { label?: string; onClick: () => void }) {
  return (
    <button onClick={onClick} className="wtr-btn h-8 px-[13px] flex items-center gap-[7px] text-[12.5px] font-medium whitespace-nowrap">
      <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round">
        <path d="M12 5v14M5 12h14" />
      </svg>
      {label}
    </button>
  )
}

export function RemoveButton({ onClick, className = '' }: { onClick: () => void; className?: string }) {
  return (
    <button onClick={onClick} className={`wtr-ghost h-[30px] px-2.5 text-[12.5px] whitespace-nowrap ${className}`}>
      Remove
    </button>
  )
}

export function EmptyRow({ children }: { children: ReactNode }) {
  return <div className="px-5 py-6 text-center text-[13px] text-[color:var(--wtr-text-sec)]">{children}</div>
}

export const SECTION_LABEL = 'text-[11px] font-semibold tracking-[0.9px] uppercase text-[color:var(--wtr-label)]'

export function FileIcon({ size = 18, muted = false }: { size?: number; muted?: boolean }) {
  return (
    <span
      className="flex-none rounded-[9px] grid place-items-center"
      style={{
        width: size + 22,
        height: size + 22,
        background: muted ? 'var(--wtr-row-border)' : 'var(--wtr-tint-cyan-bg)',
        color: muted ? 'var(--wtr-text-sec)' : 'var(--wtr-tint-cyan-fg)',
      }}
    >
      <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinejoin="round">
        <path d="M6 3h8l4 4v14H6z" />
        <path d="M14 3v4h4" />
      </svg>
    </span>
  )
}
