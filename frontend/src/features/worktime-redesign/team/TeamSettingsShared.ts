import type { Worker } from './types'

/** Patches the currently selected worker — the single write path every tab uses. */
export type UpdateWorker = (patch: Partial<Worker> | ((w: Worker) => Partial<Worker>)) => void
