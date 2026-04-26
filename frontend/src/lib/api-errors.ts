import axios from 'axios'

export interface ValidationErrors {
  [field: string]: string[]
}

export function getValidationErrors(error: unknown): string[] {
  if (!axios.isAxiosError(error)) return []
  const data = error.response?.data
  if (error.response?.status !== 422 || !data?.errors) return []
  return Object.values(data.errors as ValidationErrors).flat()
}

export function getApiErrorMessage(error: unknown): string {
  if (!axios.isAxiosError(error)) return 'An unexpected error occurred.'
  const data = error.response?.data
  if (error.response?.status === 422 && data?.errors) {
    const messages = Object.values(data.errors as ValidationErrors).flat()
    return messages.join(' ')
  }
  return data?.title ?? data?.detail ?? 'An unexpected error occurred.'
}
