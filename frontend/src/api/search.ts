import { api } from './client'

export interface SearchResultItem {
  id: string
  type: 'Project' | 'Offer' | 'Client'
  title: string
  subtitle: string
  link: string
}

export const search = (q: string) =>
  api.get<SearchResultItem[]>('/search', { params: { q } }).then(r => r.data)
