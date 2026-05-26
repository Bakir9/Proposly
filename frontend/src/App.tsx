import { Routes, Route, Navigate } from 'react-router-dom'
import { Toaster } from 'sonner'
import { useTheme } from './context/ThemeContext'
import { AppShell } from './components/AppShell'
import { ProtectedRoute } from './features/auth/ProtectedRoute'
import { ErrorBoundary } from './features/errors/ErrorBoundary'
import { NotFoundPage } from './features/errors/NotFoundPage'
import { LoginPage } from './features/auth/LoginPage'
import { RegisterPage } from './features/auth/RegisterPage'
import { ForgotPasswordPage } from './features/auth/ForgotPasswordPage'
import { ResetPasswordPage } from './features/auth/ResetPasswordPage'
import { AcceptInvitePage } from './features/auth/AcceptInvitePage'
import { DashboardPage } from './features/dashboard/DashboardPage'
import { OffersPage } from './features/offers/OffersPage'
import { OfferDetailPage } from './features/offers/OfferDetailPage'
import { CreateOfferPage } from './features/offers/CreateOfferPage'
import { ProjectsPage } from './features/projects/ProjectsPage'
import { ProjectDetailPage } from './features/projects/ProjectDetailPage'
import { UsersPage } from './features/users/UsersPage'
import { ClientsPage } from './features/clients/ClientsPage'
import { ClientDetailPage } from './features/clients/ClientDetailPage'
import { SettingsPage } from './features/settings/SettingsPage'
import { ProfilePage } from './features/profile/ProfilePage'
import { NotificationsPage } from './features/notifications/NotificationsPage'
import { QuarterlyFinancialReportPage } from './features/reports/QuarterlyFinancialReportPage'
import { CalendarPage } from './features/calendar/CalendarPage'
import { AdminCompaniesPage } from './features/admin/AdminCompaniesPage'
import { useAuth } from './features/auth/AuthContext'

function AdminRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, user } = useAuth()
  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (user?.role !== 'SuperAdmin') return <Navigate to="/dashboard" replace />
  return <>{children}</>
}

export default function App() {
  const { theme } = useTheme()
  return (
    <>
    <Toaster theme={theme} position="bottom-right" richColors closeButton />
    <ErrorBoundary>
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />
      <Route path="/accept-invite" element={<AcceptInvitePage />} />
      <Route element={<ProtectedRoute><AppShell /></ProtectedRoute>}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/offers" element={<OffersPage />} />
        <Route path="/offers/new" element={<CreateOfferPage />} />
        <Route path="/offers/:id" element={<OfferDetailPage />} />
        <Route path="/projects" element={<ProjectsPage />} />
        <Route path="/projects/:id" element={<ProjectDetailPage />} />
        <Route path="/clients" element={<ClientsPage />} />
        <Route path="/clients/:id" element={<ClientDetailPage />} />
        <Route path="/users" element={<UsersPage />} />
        <Route path="/notifications" element={<NotificationsPage />} />
        <Route path="/reports" element={<QuarterlyFinancialReportPage />} />
        <Route path="/calendar" element={<CalendarPage />} />
        <Route path="/settings" element={<SettingsPage />} />
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="/admin/companies" element={<AdminRoute><AdminCompaniesPage /></AdminRoute>} />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
    </ErrorBoundary>
    </>
  )
}
