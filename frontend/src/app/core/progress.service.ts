import { Injectable, inject, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Subject } from 'rxjs';

import { AppConfigService } from './app-config.service';
import { AuthService } from './auth.service';
import { ImportProgressEvent } from './models';

/** Cliente SignalR: un único hub por aplicación, suscripto a los trabajos que sigue el usuario. */
@Injectable({ providedIn: 'root' })
export class ImportProgressService {
  private readonly config = inject(AppConfigService);
  private readonly auth = inject(AuthService);

  private connection: HubConnection | null = null;
  private starting: Promise<void> | null = null;
  private readonly joinedJobs = new Set<string>();
  private readonly progressSubject = new Subject<ImportProgressEvent>();

  readonly progress$ = this.progressSubject.asObservable();
  readonly connected = signal(false);

  async joinJob(jobId: string): Promise<void> {
    await this.start();

    if (!this.joinedJobs.has(jobId)) {
      await this.connection!.invoke('JoinJob', jobId);
      this.joinedJobs.add(jobId);
    }
  }

  async leaveJob(jobId: string): Promise<void> {
    this.joinedJobs.delete(jobId);
    if (this.connection?.state === HubConnectionState.Connected) {
      await this.connection.invoke('LeaveJob', jobId);
    }
  }

  private hubUrl(): string {
    const base = this.config.apiUrl.replace(/\/api\/?$/, '');
    return `${base}/hubs/imports`;
  }

  private async start(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    if (this.connection?.state === HubConnectionState.Connecting && this.starting) {
      return this.starting;
    }

    const connection = this.connection ?? this.buildConnection();
    this.connection = connection;

    this.starting = connection
      .start()
      .then(() => this.connected.set(true))
      .catch((error: unknown) => {
        this.starting = null;
        throw error;
      });

    return this.starting;
  }

  private buildConnection(): HubConnection {
    const connection = new HubConnectionBuilder()
      .withUrl(this.hubUrl(), {
        accessTokenFactory: () => this.auth.token() ?? '',
      })
      .configureLogging(LogLevel.Warning)
      .withAutomaticReconnect()
      .build();

    connection.on('progress', (event: ImportProgressEvent) => this.progressSubject.next(event));
    connection.onreconnected(() => {
      this.connected.set(true);
      for (const jobId of this.joinedJobs) {
        void connection.invoke('JoinJob', jobId).catch(() => undefined);
      }
    });
    connection.onclose(() => this.connected.set(false));

    return connection;
  }
}
