export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  email: string;
  displayName: string;
  role: string;
}

export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
  role: string;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export type ImportStatus = 'Queued' | 'Processing' | 'Completed' | 'Failed';

export interface ImportJob {
  id: string;
  fileName: string;
  format: string;
  status: ImportStatus;
  processedCount: number;
  createdCount: number;
  errorCount: number;
  duplicateCount: number;
  errorMessage: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

export interface CreateImportResult {
  jobId: string;
  status: string;
}

export interface ImportProgressEvent {
  jobId: string;
  processed: number;
  created: number;
  errors: number;
  duplicates: number;
  status: ImportStatus;
}

export type LogTipo = 'Error' | 'Duplicado';

export interface ImportLogEntry {
  id: number;
  jobId: string;
  fila: number;
  tipoDocumento: string;
  documento: string;
  tipo: LogTipo;
  motivo: string;
  fecha: string;
}

export interface ImportedUser {
  id: number;
  tipoDocumento: string;
  documento: string;
  nombre: string;
  email: string;
  telefono: string | null;
  createdAt: string;
}

export interface Account {
  id: string;
  email: string;
  displayName: string;
  role: 'Admin' | 'Usuario';
  isActive: boolean;
  createdAt: string;
}

export interface CreateAccountRequest {
  email: string;
  password: string;
  displayName: string;
  role: 'Admin' | 'Usuario';
}

export interface ProblemDetails {
  title?: string;
  detail?: string;
}
