import { Routes, Route, Navigate } from 'react-router-dom'
import { AppShell } from './components/AppShell'
import { ProtectedRoute } from './features/auth/ProtectedRoute'
import { LoginPage } from './features/auth/LoginPage'
import { RegisterPage } from './features/auth/RegisterPage'
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

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
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
        <Route path="/settings" element={<SettingsPage />} />
        <Route path="/profile" element={<ProfilePage />} />
      </Route>
    </Routes>
  )
}
