import { useState, useRef, useEffect, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { Search, FolderKanban, FileText, Building2 } from 'lucide-react'
import { search, type SearchResultItem } from '@/api/search'
import { NotificationBell } from './NotificationBell'
import { cn } from '@/lib/utils'

const TYPE_ICON: Record<string, React.ElementType> = {
  Project: FolderKanban,
  Offer:   FileText,
  Client:  Building2,
}

const TYPE_LABEL_CLASS: Record<string, string> = {
  Project: 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300',
  Offer:   'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
  Client:  'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300',
}

export function TopBar() {
  const [query, setQuery]           = useState('')
  const [results, setResults]       = useState<SearchResultItem[]>([])
  const [open, setOpen]             = useState(false)
  const [loading, setLoading]       = useState(false)
  const [highlighted, setHighlighted] = useState(-1)
  const timerRef  = useRef<ReturnType<typeof setTimeout> | null>(null)
  const wrapperRef = useRef<HTMLDivElement>(null)
  const navigate  = useNavigate()

  const runSearch = useCallback(async (term: string) => {
    if (term.trim().length < 2) { setResults([]); setOpen(false); return }
    setLoading(true)
    try {
      const data = await search(term)
      setResults(data)
      setOpen(true)
      setHighlighted(-1)
    } finally {
      setLoading(false)
    }
  }, [])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value
    setQuery(val)
    if (timerRef.current) clearTimeout(timerRef.current)
    timerRef.current = setTimeout(() => runSearch(val), 300)
  }

  const handleSelect = (item: SearchResultItem) => {
    setQuery('')
    setResults([])
    setOpen(false)
    navigate(item.link)
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!open) return
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setHighlighted(h => Math.min(h + 1, results.length - 1))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setHighlighted(h => Math.max(h - 1, 0))
    } else if (e.key === 'Enter' && highlighted >= 0) {
      handleSelect(results[highlighted])
    } else if (e.key === 'Escape') {
      setOpen(false)
    }
  }

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (wrapperRef.current && !wrapperRef.current.contains(e.target as Node))
        setOpen(false)
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [])

  return (
    <div className="h-14 border-b bg-card px-6 flex items-center gap-4 shrink-0">
      <div ref={wrapperRef} className="relative flex-1 max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
        <input
          type="text"
          value={query}
          onChange={handleChange}
          onKeyDown={handleKeyDown}
          onFocus={() => results.length > 0 && setOpen(true)}
          placeholder="Search projects, offers, clients…"
          className="w-full pl-9 pr-4 py-1.5 text-sm rounded-md border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
        />

        {open && (
          <div className="absolute top-full mt-1 w-full min-w-[320px] bg-card border rounded-lg shadow-lg z-50 overflow-hidden">
            {loading && (
              <p className="text-sm text-muted-foreground px-4 py-3">Searching…</p>
            )}
            {!loading && results.length === 0 && (
              <p className="text-sm text-muted-foreground px-4 py-3">No results found.</p>
            )}
            {!loading && results.map((item, i) => {
              const Icon = TYPE_ICON[item.type] ?? FolderKanban
              return (
                <button
                  key={item.id}
                  onClick={() => handleSelect(item)}
                  className={cn(
                    'w-full flex items-center gap-3 px-4 py-2.5 text-sm text-left border-b last:border-0 transition-colors',
                    i === highlighted ? 'bg-accent' : 'hover:bg-accent'
                  )}
                >
                  <Icon className="h-4 w-4 text-muted-foreground shrink-0" />
                  <div className="flex-1 min-w-0">
                    <p className="font-medium truncate">{item.title}</p>
                    <p className="text-xs text-muted-foreground truncate">{item.subtitle}</p>
                  </div>
                  <span className={cn('text-[10px] font-semibold px-1.5 py-0.5 rounded shrink-0', TYPE_LABEL_CLASS[item.type])}>
                    {item.type}
                  </span>
                </button>
              )
            })}
          </div>
        )}
      </div>

      <div className="ml-auto">
        <NotificationBell />
      </div>
    </div>
  )
}
