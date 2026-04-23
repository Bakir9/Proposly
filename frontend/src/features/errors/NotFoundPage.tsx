import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'

export function NotFoundPage() {
  const navigate = useNavigate()
  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-4 bg-muted/40 text-center p-6">
      <p className="text-8xl font-bold text-muted-foreground/30">404</p>
      <h1 className="text-2xl font-semibold">Page not found</h1>
      <p className="text-muted-foreground max-w-sm">
        The page you're looking for doesn't exist or you don't have access to it.
      </p>
      <Button onClick={() => navigate('/')}>Go to dashboard</Button>
    </div>
  )
}
