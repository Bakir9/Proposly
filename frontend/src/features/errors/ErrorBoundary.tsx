import { Component, type ReactNode } from 'react'
import { Button } from '@/components/ui/button'

interface Props { children: ReactNode }
interface State { hasError: boolean; message?: string }

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, message: error.message }
  }

  render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen flex flex-col items-center justify-center gap-4 bg-muted/40 text-center p-6">
          <p className="text-8xl font-bold text-muted-foreground/30">500</p>
          <h1 className="text-2xl font-semibold">Something went wrong</h1>
          <p className="text-muted-foreground max-w-sm">
            An unexpected error occurred. Try refreshing the page.
          </p>
          <Button onClick={() => window.location.replace('/')}>Go to dashboard</Button>
        </div>
      )
    }
    return this.props.children
  }
}
