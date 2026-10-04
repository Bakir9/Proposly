import { useTheme } from '@/context/ThemeContext'
import { avatarColors, initials } from './chat-utils'

interface ChatAvatarProps {
  name: string
  hue: number
  /** People are circles, groups and projects rounded squares. */
  round?: boolean
  size?: number
  fontSize?: number
}

export function ChatAvatar({ name, hue, round = true, size = 38, fontSize = 12 }: ChatAvatarProps) {
  const { theme } = useTheme()
  return (
    <div
      className="flex items-center justify-center font-semibold shrink-0"
      style={{
        width: size,
        height: size,
        fontSize,
        borderRadius: round ? '50%' : Math.round(size * 0.24),
        ...avatarColors(hue, theme === 'dark'),
      }}
    >
      {initials(name)}
    </div>
  )
}
