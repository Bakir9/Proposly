import type { AbsenceKind, NoteTag, Worker, WorkerDraft } from './types'

/** Patches the draft (saved later via the Save button) — the write path for editable fields. */
export type UpdateWorker = (patch: Partial<WorkerDraft> | ((w: Worker) => Partial<WorkerDraft>)) => void

/** Server-backed actions that apply immediately (files, absences, notes). */
export interface WorkerActions {
  uploadPhoto: (file: File) => void
  removePhoto: () => void
  uploadCv: (file: File) => void
  removeCv: () => void
  downloadCv: (fileName: string) => void
  uploadDocuments: (files: File[], category: string) => void
  deleteDocument: (documentId: string) => void
  setDocumentCategory: (documentId: string, category: string) => void
  downloadDocument: (documentId: string, fileName: string) => void
  addAbsence: (kind: AbsenceKind, from: string, to: string) => void
  approveAbsence: (absenceId: string) => void
  removeAbsence: (absenceId: string) => void
  addNote: (tag: NoteTag, text: string) => void
  pinNote: (noteId: string, pinned: boolean) => void
  deleteNote: (noteId: string) => void
}
