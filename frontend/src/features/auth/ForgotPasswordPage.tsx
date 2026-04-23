import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { forgotPassword } from '@/api/auth'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')

  const mutation = useMutation({
    mutationFn: () => forgotPassword(email),
  })

  if (mutation.isSuccess) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-muted/40">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle className="text-2xl">Check your email</CardTitle>
            <CardDescription>
              If an account exists for <strong>{email}</strong>, you'll receive a password reset link shortly.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <Link to="/login" className="text-sm text-primary hover:underline">Back to sign in</Link>
          </CardContent>
        </Card>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-muted/40">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-2xl">Forgot password</CardTitle>
          <CardDescription>Enter your email and we'll send you a reset link.</CardDescription>
        </CardHeader>
        <CardContent>
          <form
            onSubmit={e => { e.preventDefault(); mutation.mutate() }}
            className="space-y-4"
          >
            <div className="space-y-1">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="you@company.com"
                value={email}
                onChange={e => setEmail(e.target.value)}
                required
              />
            </div>
            {mutation.isError && (
              <p className="text-sm text-destructive">Something went wrong. Please try again.</p>
            )}
            <Button type="submit" className="w-full" disabled={mutation.isPending}>
              {mutation.isPending ? 'Sending…' : 'Send reset link'}
            </Button>
          </form>
          <p className="mt-4 text-center text-sm text-muted-foreground">
            <Link to="/login" className="text-primary hover:underline">Back to sign in</Link>
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
