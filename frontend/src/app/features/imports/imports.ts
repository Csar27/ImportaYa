import { DatePipe, DecimalPipe, UpperCasePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subscription, switchMap, takeWhile, timer } from 'rxjs';

import { ApiService } from '../../core/api.service';
import { extractErrorMessage } from '../../core/error';
import { ImportJob, ImportLogEntry, ImportProgressEvent, LogTipo } from '../../core/models';
import { ImportProgressService } from '../../core/progress.service';

const ALLOWED_EXTENSIONS = ['csv', 'xlsx'];

@Component({
  selector: 'app-imports',
  imports: [
    DatePipe,
    DecimalPipe,
    UpperCasePipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
  ],
  templateUrl: './imports.html',
  styleUrl: './imports.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Imports {
  private readonly api = inject(ApiService);
  private readonly progress = inject(ImportProgressService);
  private readonly destroyRef = inject(DestroyRef);
  private pollSubscription: Subscription | null = null;

  readonly file = signal<File | null>(null);
  readonly dragging = signal(false);
  readonly uploading = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);

  readonly jobs = signal<ImportJob[]>([]);
  readonly currentJob = signal<ImportJob | null>(null);
  readonly logs = signal<ImportLogEntry[]>([]);
  readonly logTotal = signal(0);
  readonly logPageIndex = signal(0);
  readonly logPageSize = signal(10);
  readonly logLoading = signal(false);
  readonly logFilter = signal<LogTipo | null>(null);

  readonly isActive = computed(() => {
    const status = this.currentJob()?.status;
    return status === 'Queued' || status === 'Processing';
  });

  readonly jobColumns = [
    'fileName',
    'format',
    'status',
    'processedCount',
    'createdCount',
    'errorCount',
    'duplicateCount',
    'createdAt',
  ];
  readonly logColumns = ['fila', 'tipo', 'tipoDocumento', 'documento', 'motivo', 'fecha'];

  constructor() {
    this.loadHistory();
    this.progress.progress$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((event) => this.onProgress(event));
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectFile(input.files?.[0] ?? null);
    input.value = '';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
    this.selectFile(event.dataTransfer?.files?.[0] ?? null);
  }

  selectFile(file: File | null): void {
    this.error.set(null);
    this.notice.set(null);

    if (!file) {
      return;
    }

    const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
    if (!ALLOWED_EXTENSIONS.includes(extension)) {
      this.error.set('Formato no soportado. Adjunte un archivo .csv o .xlsx.');
      this.file.set(null);
      return;
    }

    if (file.size === 0) {
      this.error.set('El archivo está vacío.');
      this.file.set(null);
      return;
    }

    if (file.size > 10 * 1024 * 1024) {
      this.error.set('El archivo supera el límite de 10 MB.');
      this.file.set(null);
      return;
    }

    this.file.set(file);
  }

  upload(): void {
    const selected = this.file();
    if (!selected || this.uploading()) {
      return;
    }

    this.uploading.set(true);
    this.error.set(null);
    this.notice.set(null);

    this.api.upload(selected).subscribe({
      next: (result) => {
        this.uploading.set(false);
        this.file.set(null);
        this.notice.set(`Importación ${result.jobId} recibida. Procesando en segundo plano…`);
        this.loadHistory();
        void this.trackJob(result.jobId);
      },
      error: (error: unknown) => {
        this.uploading.set(false);
        this.error.set(extractErrorMessage(error, 'No se pudo subir el archivo.'));
      },
    });
  }

  clearFile(): void {
    this.file.set(null);
    this.error.set(null);
  }

  selectJob(job: ImportJob): void {
    this.currentJob.set(job);
    this.logPageIndex.set(0);
    this.logFilter.set(null);
    this.loadLogs();

    if (job.status === 'Queued' || job.status === 'Processing') {
      void this.progress.joinJob(job.id).catch(() => this.pollJob(job.id));
    }
  }

  changeFilter(value: string): void {
    this.logFilter.set(value === 'Error' || value === 'Duplicado' ? value : null);
    this.logPageIndex.set(0);
    this.loadLogs();
  }

  onPage(event: PageEvent): void {
    this.logPageIndex.set(event.pageIndex);
    this.logPageSize.set(event.pageSize);
    this.loadLogs();
  }

  downloadTemplate(format: 'csv' | 'xlsx'): void {
    this.api.downloadTemplate(format).subscribe({
      next: (blob) => ApiService.saveBlob(blob, `plantilla-usuarios.${format}`),
      error: () => this.error.set('No se pudo descargar la plantilla.'),
    });
  }

  downloadLog(format: 'csv' | 'xlsx'): void {
    const jobId = this.currentJob()?.id;
    if (!jobId) {
      return;
    }

    this.api.downloadLog(jobId, this.logFilter(), format).subscribe({
      next: (blob) => ApiService.saveBlob(blob, `log-${jobId}.${format}`),
      error: () => this.error.set('No se pudo descargar el log.'),
    });
  }

  private loadHistory(): void {
    this.api.listJobs(1, 20)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => this.jobs.set(result.items),
        error: () => this.error.set('No se pudo cargar el historial.'),
      });
  }

  private async trackJob(jobId: string): Promise<void> {
    try {
      await this.progress.joinJob(jobId);
      this.api.getJob(jobId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (job) => this.currentJob.set(job),
          error: () => this.pollJob(jobId),
        });
    } catch {
      this.error.set('SignalR no disponible; se consulta el progreso cada 800 ms.');
      this.pollJob(jobId);
    }
  }

  private onProgress(event: ImportProgressEvent): void {
    let job = this.currentJob();
    if (!job || job.id !== event.jobId) {
      const fromHistory = this.jobs().find((item) => item.id === event.jobId);
      if (!fromHistory) {
        return;
      }
      job = fromHistory;
      this.currentJob.set(job);
    }

    const terminal = event.status === 'Completed' || event.status === 'Failed';
    this.currentJob.set({
      ...job,
      status: event.status,
      processedCount: event.processed,
      createdCount: event.created,
      errorCount: event.errors,
      duplicateCount: event.duplicates,
      startedAt: job.startedAt ?? new Date().toISOString(),
      completedAt: terminal ? job.completedAt ?? new Date().toISOString() : job.completedAt,
    });

    if (terminal) {
      void this.progress.leaveJob(event.jobId);
      this.loadHistory();
      this.logPageIndex.set(0);
      this.loadLogs();
      this.notice.set(
        event.status === 'Completed'
          ? `Importación finalizada: ${event.created} usuarios creados.`
          : `Importación fallida: ${event.errors} filas con error.`,
      );
    }
  }

  private pollJob(jobId: string): void {
    this.pollSubscription?.unsubscribe();

    this.pollSubscription = timer(200, 800)
      .pipe(
        switchMap(() => this.api.getJob(jobId)),
        takeWhile((job) => job.status === 'Queued' || job.status === 'Processing', true),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (job) => {
          this.currentJob.set(job);
          this.loadHistory();
          if (job.status !== 'Queued' && job.status !== 'Processing') {
            void this.progress.leaveJob(jobId);
            this.notice.set(
              job.status === 'Completed'
                ? `Importación finalizada: ${job.createdCount} usuarios creados.`
                : `Importación fallida: ${job.errorMessage ?? 'error desconocido'}`,
            );
            this.logPageIndex.set(0);
            this.loadLogs();
          }
        },
        error: (error: unknown) => {
          this.error.set(extractErrorMessage(error, 'No se pudo consultar el progreso.'));
        },
      });
  }

  private loadLogs(): void {
    const jobId = this.currentJob()?.id;
    if (!jobId) {
      return;
    }

    this.logLoading.set(true);
    this.api.getLog(jobId, this.logFilter(), this.logPageIndex() + 1, this.logPageSize())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.logs.set(result.items);
          this.logTotal.set(result.total);
          this.logLoading.set(false);
        },
        error: () => {
          this.logLoading.set(false);
          this.error.set('No se pudo cargar el log.');
        },
      });
  }
}
