import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';

import { ApiService } from '../../core/api.service';
import { extractErrorMessage } from '../../core/error';
import { Account } from '../../core/models';

@Component({
  selector: 'app-accounts',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './accounts.html',
  styleUrl: './accounts.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Accounts {
  private readonly api = inject(ApiService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly accounts = signal<Account[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);
  readonly columns = ['displayName', 'email', 'role', 'isActive', 'createdAt'];

  readonly form = this.formBuilder.nonNullable.group({
    displayName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    role: ['Usuario' as 'Admin' | 'Usuario', [Validators.required]],
  });

  constructor() {
    this.load();
  }

  create(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.notice.set(null);

    this.api.createAccount(this.form.getRawValue()).subscribe({
      next: (account) => {
        this.notice.set(`Cuenta ${account.email} creada con rol ${account.role}.`);
        this.form.reset({ role: 'Usuario' });
        this.loading.set(false);
        this.load();
      },
      error: (error: unknown) => {
        this.error.set(extractErrorMessage(error, 'No se pudo crear la cuenta.'));
        this.loading.set(false);
      },
    });
  }

  private load(): void {
    this.api.listAccounts()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => this.accounts.set(items),
        error: () => this.error.set('No se pudo cargar la lista de cuentas.'),
      });
  }
}
