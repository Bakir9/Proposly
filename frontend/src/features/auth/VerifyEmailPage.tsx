import { useEffect, useState } from 'react'
import { useSearchParams, useNavigate, Link } from 'react-router-dom'
import { verifyEmail } from '@/api/auth'
import { useAuth } from './AuthContext'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function VerifyEmailPage() {
  const [searchParams] = useSearchParams()
  const { login: setAuth } = useAuth()
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const token = searchParams.get('token')
    if (!token) {
      setError('Invalid verification link.')
      return
    }
    verifyEmail(token)
      .then(data => {
        setAuth(data)
        navigate('/')
      })
      .catch(() => setError('This verification link is invalid or has expired.'))
  }, [])

  return (
    <div className="min-h-screen flex items-center justify-center bg-muted/40">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-2xl">
            {error ? 'Verification failed' : 'Verifying your email…'}
          </CardTitle>
          {error && (
            <CardDescription className="text-destructive">{error}</CardDescription>
          )}
        </CardHeader>
        {error && (
          <CardContent>
            <p className="text-sm text-muted-foreground">
              Please{' '}
              <Link to="/register" className="text-primary hover:underline">register again</Link>
              {' '}or contact support.
            </p>
          </CardContent>
        )}
      </Card>
    </div>
  )
}
