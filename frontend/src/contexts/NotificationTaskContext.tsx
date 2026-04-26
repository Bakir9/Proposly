import { createContext, useContext, useState } from 'react'

interface NotificationTaskContext {
  pendingTaskId: string | null
  setPendingTaskId: (id: string | null) => void
}

const NotificationTaskContext = createContext<NotificationTaskContext>({
  pendingTaskId: null,
  setPendingTaskId: () => {},
})

export function NotificationTaskProvider({ children }: { children: React.ReactNode }) {
  const [pendingTaskId, setPendingTaskId] = useState<string | null>(null)
  return (
    <NotificationTaskContext.Provider value={{ pendingTaskId, setPendingTaskId }}>
      {children}
    </NotificationTaskContext.Provider>
  )
}

export const useNotificationTask = () => useContext(NotificationTaskContext)
