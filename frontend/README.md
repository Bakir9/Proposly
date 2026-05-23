# Proposly — Frontend

React + TypeScript SPA for the Proposly platform. See the [root README](../README.md) for full project documentation.

## Stack

- **React 19** + TypeScript, built with Vite
- **Tailwind CSS** + shadcn/ui components
- **TanStack Query** for server state
- **Recharts** for charts (burndown, velocity, capacity, reports)
- **Tiptap** for rich text editing (task descriptions, project notes)
- **Sonner** for toast notifications

## Dev

```bash
npm install
npm run dev       # http://localhost:5173 — proxies /api → http://localhost:5143
npm run build
npm run lint
```

## Structure

```
src/
├── api/              # Typed API functions (one file per domain)
├── components/       # Shared UI components (AppShell, ui/*)
├── context/          # ThemeContext, AuthContext
└── features/
    ├── auth/         # Login, register, invite, password reset
    ├── calendar/     # CalendarPage, month/week views, meeting modals
    ├── clients/      # ClientsPage, ClientDetailPage
    ├── dashboard/    # DashboardPage
    ├── errors/       # ErrorBoundary, NotFoundPage
    ├── notifications/# NotificationsPage, NotificationBell
    ├── offers/       # OffersPage, OfferDetailPage, CreateOfferPage
    ├── profile/      # ProfilePage
    ├── projects/     # ProjectsPage, ProjectDetailPage (9 tabs)
    ├── reports/      # QuarterlyFinancialReportPage
    ├── settings/     # SettingsPage
    └── users/        # UsersPage
```
