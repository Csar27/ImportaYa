import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { debounceTime, distinctUntilChanged } from 'rxjs';

import { ApiService } from '../../core/api.service';
import { extractErrorMessage } from '../../core/error';
import { ImportedUser } from '../../core/models';

@Component({
  selector: 'app-users',
  imports: [
    DatePipe,
    DecimalPipe,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './users.html',
  styleUrl: './users.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Users {
  private readonly api = inject(ApiService);

  readonly search = new FormControl('', { nonNullable: true });
  readonly users = signal<ImportedUser[]>([]);
  readonly total = signal(0);
  readonly pageIndex = signal(0);
  readonly pageSize = signal(20);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly columns = ['tipoDocumento', 'documento', 'nombre', 'email', 'telefono', 'createdAt'];

  constructor() {
    this.load();
    this.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(inject(DestroyRef)))
      .subscribe(() => {
        this.pageIndex.set(0);
        this.load();
      });
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api
      .listUsers(this.search.value, this.pageIndex() + 1, this.pageSize())
      .subscribe({
        next: (result) => {
          this.users.set(result.items);
          this.total.set(result.total);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.error.set(extractErrorMessage(error, 'No se pudo cargar los usuarios.'));
          this.users.set([]);
          this.total.set(0);
          this.loading.set(false);
        },
      });
  }
}
