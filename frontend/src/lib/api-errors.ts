import axios from 'axios'

export interface ValidationErrors {
  [field: string]: string[]
}

/** Shape of the API's problem responses: { status, code, title, detail?, errors? }. */
export interface ApiError {
  status: number
  /** Stable machine-readable code, e.g. "project.not_found", "plan.user_limit_reached",
   * "validation_failed", "forbidden", "bad_request", "internal_error". */
  code?: string
  title?: string
  detail?: string
  errors?: ValidationErrors
}

/** The structured API error, or null when the failure wasn't an HTTP response (network etc.). */
export function getApiError(error: unknown): ApiError | null {
  if (!axios.isAxiosError(error) || !error.response) return null
  const data = error.response.data as Partial<ApiError> | undefined
  return {
    status: error.response.status,
    code: data?.code,
    title: data?.title,
    detail: data?.detail,
    errors: data?.errors,
  }
}

/** Machine-readable error code for branching (e.g. show an upgrade dialog on
 * "plan.user_limit_reached"); null when the response carries none. */
export function getApiErrorCode(error: unknown): string | null {
  return getApiError(error)?.code ?? null
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
