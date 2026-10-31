import { useTheme } from '@/context/ThemeContext'
import { cn } from '@/lib/utils'
import type { Presence } from '@/api/chat'
import { avatarColors, initials } from './chat-utils'

interface ChatAvatarProps {
  name: string
  hue: number
  /** People are circles, groups and projects rounded squares. */
  round?: boolean
  size?: number
  fontSize?: number
  /** Shows a green/amber dot bottom-right (nothing for offline). Used by the chat dock. */
  presence?: Presence
  /** Offline people in the dock render at 60 % opacity. */
  dimmed?: boolean
}

export function ChatAvatar({ name, hue, round = true, size = 38, fontSize = 12, presence, dimmed }: ChatAvatarProps) {
  const { theme } = useTheme()
  const dot = presence === 'online' || presence === 'away'
  return (
    <div className="relative shrink-0" style={{ width: size, height: size }}>
      <div
        className={cn('w-full h-full flex items-center justify-center font-semibold', dimmed && 'opacity-60')}
        style={{
          fontSize,
          borderRadius: round ? '50%' : Math.round(size * 0.24),
          ...avatarColors(hue, theme === 'dark'),
        }}
      >
        {initials(name)}
      </div>
      {dot && (
        <span
          aria-label={presence === 'online' ? 'Online' : 'Away'}
          className={cn(
            'absolute -right-px -bottom-px rounded-full border-2 border-card',
            presence === 'online' ? 'bg-green-500' : 'bg-amber-500',
          )}
          style={{ width: size >= 40 ? 13 : 12, height: size >= 40 ? 13 : 12 }}
        />
      )}
    </div>
  )
}
