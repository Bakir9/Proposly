import { cn } from '@/lib/utils'

/** Pulsing placeholder block shown while a section's data is still loading. */
export function Skeleton({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <div aria-hidden className={cn('animate-pulse rounded-md bg-muted', className)} {...props} />
}
