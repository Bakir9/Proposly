import { useState } from 'react'
import { useSearchParams, useNavigate, Link } from 'react-router-dom'
import { useMutation } from '@tanstack/react-query'
import { acceptInvite } from '@/api/users'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function AcceptInvitePage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get('token') ?? ''

  const [newPassword, setNewPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [validationError, setValidationError] = useState('')

  const mutation = useMutation({
    mutationFn: () => acceptInvite(token, newPassword),
    onSuccess: () => navigate('/login', { state: { inviteAccepted: true } }),
  })

  if (!token) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-muted/40">
        <Card className="w-full max-w-md">
          <CardHeader>
            <CardTitle className="text-2xl">Invalid invite link</CardTitle>
            <CardDescription>This invite link is missing a token. Ask your administrator to resend the invite.</CardDescription>
          </CardHeader>
          <CardContent>
            <Link to="/login" className="text-sm text-primary hover:underline">Back to sign in</Link>
          </CardContent>
        </Card>
      </div>
    )
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (newPassword.length < 6) {
      setValidationError('Password must be at least 6 characters.')
      return
    }
    if (newPassword !== confirm) {
      setValidationError('Passwords do not match.')
      return
    }
    setValidationError('')
    mutation.mutate()
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-muted/40">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-2xl">Activate your account</CardTitle>
          <CardDescription>Set a password to complete your account setup.</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1">
              <Label htmlFor="newPassword">Password</Label>
              <Input
                id="newPassword"
                type="password"
                value={newPassword}
                onChange={e => setNewPassword(e.target.value)}
                required
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="confirm">Confirm password</Label>
              <Input
                id="confirm"
                type="password"
                value={confirm}
                onChange={e => setConfirm(e.target.value)}
                required
              />
            </div>
            {validationError && <p className="text-sm text-destructive">{validationError}</p>}
            {mutation.isError && (
              <p className="text-sm text-destructive">This invite link is invalid or has expired. Ask your administrator to resend it.</p>
            )}
            <Button type="submit" className="w-full" disabled={mutation.isPending}>
              {mutation.isPending ? 'Activating…' : 'Activate account'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
