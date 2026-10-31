import { useCallback, useEffect, useState } from 'react'

export interface DockEntry {
  userId: string
  minimized: boolean
}

const KEY = 'proposly-chat-dock'

function load(): { open: DockEntry[]; panelOpen: boolean } {
  try {
    const v = JSON.parse(sessionStorage.getItem(KEY) ?? '')
    if (Array.isArray(v.open)) return { open: v.open, panelOpen: !!v.panelOpen }
  } catch {
    /* nothing stored */
  }
  return { open: [], panelOpen: false }
}

/**
 * Which chat windows are open/minimized and whether the people panel is open.
 * Most recently opened first. Survives navigation and reloads in this tab.
 */
export function useDockState(maxWindows = 3) {
  const [state, setState] = useState(load)
  const [vw, setVw] = useState(() => window.innerWidth)

  useEffect(() => {
    const onResize = () => setVw(window.innerWidth)
    window.addEventListener('resize', onResize)
    return () => window.removeEventListener('resize', onResize)
  }, [])
  useEffect(() => {
    try {
      sessionStorage.setItem(KEY, JSON.stringify(state))
    } catch {
      /* private mode */
    }
  }, [state])

  // Windows are 328 px + 12 gap; keep 200 px of page visible on the left.
  const rightOffset = state.panelOpen ? 380 : 96
  const fits = Math.max(1, Math.floor((vw - rightOffset - 200) / 340))
  const expandedIds = state.open
    .filter(o => !o.minimized)
    .slice(0, Math.min(maxWindows, fits))
    .map(o => o.userId)
  const headIds = state.open.map(o => o.userId).filter(id => !expandedIds.includes(id))

  const openUser = useCallback(
    (userId: string) =>
      setState(s => ({ ...s, open: [{ userId, minimized: false }, ...s.open.filter(o => o.userId !== userId)] })),
    [],
  )
  const minimize = useCallback(
    (userId: string) => setState(s => ({ ...s, open: s.open.map(o => (o.userId === userId ? { ...o, minimized: true } : o)) })),
    [],
  )
  const close = useCallback((userId: string) => setState(s => ({ ...s, open: s.open.filter(o => o.userId !== userId) })), [])
  const togglePanel = useCallback(() => setState(s => ({ ...s, panelOpen: !s.panelOpen })), [])

  return { panelOpen: state.panelOpen, expandedIds, headIds, rightOffset, openUser, minimize, close, togglePanel }
}
