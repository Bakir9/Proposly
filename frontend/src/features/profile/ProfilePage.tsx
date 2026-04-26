import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { getMe, updateProfile, changePassword } from '@/api/users'
import { useAuth } from '@/features/auth/AuthContext'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Check } from 'lucide-react'
import { getApiErrorMessage } from '@/lib/api-errors'

const AVATAR_COLORS = [
  'bg-blue-500',
  'bg-violet-500',
  'bg-rose-500',
  'bg-orange-500',
  'bg-green-500',
  'bg-teal-500',
  'bg-pink-500',
  'bg-amber-500',
  'bg-indigo-500',
  'bg-cyan-500',
  'bg-lime-600',
  'bg-red-500',
]

const AVATAR_STORAGE_KEY = 'proposly_avatar_color'

export function getAvatarColor(): string {
  const stored = localStorage.getItem(AVATAR_STORAGE_KEY)
  const idx = stored !== null ? parseInt(stored, 10) : 0
  return AVATAR_COLORS[idx] ?? AVATAR_COLORS[0]
}

function setAvatarColor(idx: number) {
  localStorage.setItem(AVATAR_STORAGE_KEY, String(idx))
}

export function AvatarCircle({ initials, className = '' }: { initials: string; className?: string }) {
  const color = getAvatarColor()
  return (
    <div className={`${color} ${className} rounded-full flex items-center justify-center text-white font-semibold select-none`}>
      {initials}
    </div>
  )
}

function getInitials(firstName: string, lastName: string) {
  return `${firstName.charAt(0)}${lastName.charAt(0)}`.toUpperCase()
}

export function ProfilePage() {
  const { user, login } = useAuth()
  const queryClient = useQueryClient()

  const { data: profile, isLoading } = useQuery({ queryKey: ['me'], queryFn: getMe })

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [email, setEmail] = useState('')
  const [editing, setEditing] = useState(false)

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [passwordError, setPasswordError] = useState('')
  const [passwordSuccess, setPasswordSuccess] = useState(false)
  const [avatarIdx, setAvatarIdx] = useState<number>(() => {
    const stored = localStorage.getItem(AVATAR_STORAGE_KEY)
    return stored !== null ? parseInt(stored, 10) : 0
  })

  const mutUpdate = useMutation({
    mutationFn: () => updateProfile({ firstName: firstName.trim(), lastName: lastName.trim(), email: email.trim() }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['me'] })
      if (user) {
        login({
          ...user,
          fullName: `${firstName.trim()} ${lastName.trim()}`,
        })
      }
      setEditing(false)
    },
  })

  const mutChangePassword = useMutation({
    mutationFn: () => changePassword({ currentPassword, newPassword }),
    onSuccess: () => {
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setPasswordError('')
      setPasswordSuccess(true)
      setTimeout(() => setPasswordSuccess(false), 3000)
    },
    onError: (err: unknown) => setPasswordError(getApiErrorMessage(err)),
  })

  const handleChangePassword = () => {
    setPasswordError('')
    if (newPassword.length < 6) { setPasswordError('New password must be at least 6 characters.'); return }
    if (newPassword !== confirmPassword) { setPasswordError('Passwords do not match.'); return }
    mutChangePassword.mutate()
  }

  const startEdit = () => {
    setFirstName(profile?.firstName ?? '')
    setLastName(profile?.lastName ?? '')
    setEmail(profile?.email ?? '')
    setEditing(true)
  }

  const pickAvatar = (idx: number) => {
    setAvatarIdx(idx)
    setAvatarColor(idx)
    queryClient.invalidateQueries({ queryKey: ['me'] })
    window.dispatchEvent(new Event('avatarChanged'))
  }

  if (isLoading) return <div className="p-6"><p className="text-muted-foreground">Loading…</p></div>
  if (!profile) return null

  const initials = getInitials(profile.firstName, profile.lastName)
  const currentColor = AVATAR_COLORS[avatarIdx] ?? AVATAR_COLORS[0]

  return (
    <div className="p-6 max-w-4xl space-y-6">
      <h1 className="text-2xl font-semibold">My Profile</h1>

      {/* Avatar */}
      <Card>
        <CardHeader><CardTitle className="text-base">Avatar</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center gap-4">
            <div className={`${currentColor} w-16 h-16 rounded-full flex items-center justify-center text-white text-2xl font-bold select-none`}>
              {initials}
            </div>
            <div>
              <p className="text-sm font-medium">{profile.fullName}</p>
              <p className="text-xs text-muted-foreground">{profile.email}</p>
            </div>
          </div>
          <p className="text-sm text-muted-foreground">Choose a background color for your avatar:</p>
          <div className="flex flex-wrap gap-2">
            {AVATAR_COLORS.map((color, idx) => (
              <button
                key={idx}
                onClick={() => pickAvatar(idx)}
                className={`${color} w-8 h-8 rounded-full flex items-center justify-center transition-transform hover:scale-110 focus:outline-none focus:ring-2 focus:ring-ring`}
                title={`Color ${idx + 1}`}
              >
                {avatarIdx === idx && <Check className="h-4 w-4 text-white" />}
              </button>
            ))}
          </div>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 items-start">
        {/* Profile info */}
        <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="text-base">Profile Information</CardTitle>
            {!editing && <Button size="sm" variant="outline" onClick={startEdit}>Edit</Button>}
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          {editing ? (
            <>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label>First Name</Label>
                  <Input value={firstName} onChange={e => setFirstName(e.target.value)} />
                </div>
                <div className="space-y-1">
                  <Label>Last Name</Label>
                  <Input value={lastName} onChange={e => setLastName(e.target.value)} />
                </div>
              </div>
              <div className="space-y-1">
                <Label>Email</Label>
                <Input type="email" value={email} onChange={e => setEmail(e.target.value)} />
              </div>
              <div className="flex gap-2 justify-end">
                <Button variant="outline" onClick={() => setEditing(false)}>Cancel</Button>
                <Button
                  onClick={() => mutUpdate.mutate()}
                  disabled={mutUpdate.isPending || !firstName.trim() || !lastName.trim() || !email.trim()}
                >
                  {mutUpdate.isPending ? 'Saving…' : 'Save'}
                </Button>
              </div>
            </>
          ) : (
            <div className="grid grid-cols-2 gap-3 text-sm">
              <div>
                <p className="text-muted-foreground">First Name</p>
                <p className="font-medium">{profile.firstName}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Last Name</p>
                <p className="font-medium">{profile.lastName}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Email</p>
                <p className="font-medium">{profile.email}</p>
              </div>
              <div>
                <p className="text-muted-foreground">Role</p>
                <Badge variant="secondary">{profile.role}</Badge>
              </div>
              <div>
                <p className="text-muted-foreground">Member since</p>
                <p className="font-medium">{new Date(profile.createdAt).toLocaleDateString()}</p>
              </div>
            </div>
          )}
        </CardContent>
        </Card>

        {/* Change password */}
        <Card>
          <CardHeader><CardTitle className="text-base">Change Password</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-1">
              <Label>Current Password</Label>
              <Input type="password" value={currentPassword} onChange={e => { setCurrentPassword(e.target.value); setPasswordError('') }} />
            </div>
            <div className="space-y-1">
              <Label>New Password</Label>
              <Input type="password" value={newPassword} onChange={e => { setNewPassword(e.target.value); setPasswordError('') }} />
            </div>
            <div className="space-y-1">
              <Label>Confirm New Password</Label>
              <Input type="password" value={confirmPassword} onChange={e => { setConfirmPassword(e.target.value); setPasswordError('') }} />
            </div>
            {passwordError && <p className="text-sm text-destructive">{passwordError}</p>}
            {passwordSuccess && <p className="text-sm text-green-600">Password changed successfully.</p>}
            <div className="flex justify-end">
              <Button
                onClick={handleChangePassword}
                disabled={mutChangePassword.isPending || !currentPassword || !newPassword || !confirmPassword}
              >
                {mutChangePassword.isPending ? 'Saving…' : 'Change Password'}
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
