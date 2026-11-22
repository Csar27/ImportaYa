import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { AppConfigService } from './app-config.service';
import {
  Account,
  CreateAccountRequest,
  CreateImportResult,
  CurrentUser,
  ImportedUser,
  ImportJob,
  ImportLogEntry,
  LoginRequest,
  LoginResponse,
  LogTipo,
  PagedResult,
  RegisterRequest,
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly config = inject(AppConfigService);

  private get baseUrl(): string {
    return this.config.apiUrl;
  }

  login(body: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/auth/login`, body);
  }

  me(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${this.baseUrl}/auth/me`);
  }

  register(request: RegisterRequest): Observable<Account> {
    return this.http.post<Account>(`${this.baseUrl}/auth/register`, request);
  }

  upload(file: File): Observable<CreateImportResult> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<CreateImportResult>(`${this.baseUrl}/imports`, form);
  }

  listJobs(page = 1, pageSize = 20): Observable<PagedResult<ImportJob>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<ImportJob>>(`${this.baseUrl}/imports`, { params });
  }

  getJob(id: string): Observable<ImportJob> {
    return this.http.get<ImportJob>(`${this.baseUrl}/imports/${id}`);
  }

  getLog(
    jobId: string,
    tipo: LogTipo | null,
    page = 1,
    pageSize = 10,
  ): Observable<PagedResult<ImportLogEntry>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (tipo) {
      params = params.set('tipo', tipo);
    }
    return this.http.get<PagedResult<ImportLogEntry>>(`${this.baseUrl}/imports/${jobId}/log`, {
      params,
    });
  }

  downloadLog(jobId: string, tipo: LogTipo | null, format: 'csv' | 'xlsx'): Observable<Blob> {
    let params = new HttpParams().set('formato', format);
    if (tipo) {
      params = params.set('tipo', tipo);
    }
    return this.http.get(`${this.baseUrl}/imports/${jobId}/log`, {
      params,
      responseType: 'blob',
    });
  }

  downloadTemplate(format: 'csv' | 'xlsx'): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/imports/plantilla`, {
      params: new HttpParams().set('formato', format),
      responseType: 'blob',
    });
  }

  listUsers(search: string, page = 1, pageSize = 20): Observable<PagedResult<ImportedUser>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search.trim()) {
      params = params.set('q', search.trim());
    }
    return this.http.get<PagedResult<ImportedUser>>(`${this.baseUrl}/users`, { params });
  }

  listAccounts(): Observable<Account[]> {
    return this.http.get<Account[]>(`${this.baseUrl}/accounts`);
  }

  createAccount(request: CreateAccountRequest): Observable<Account> {
    return this.http.post<Account>(`${this.baseUrl}/accounts`, request);
  }

  /** Descarga un Blob con el nombre indicado usando una URL temporal. */
  static saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    setTimeout(() => URL.revokeObjectURL(url), 0);
  }
}
