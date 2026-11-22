import { inject, Injectable, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

import { ApiService } from './api.service';
import { CurrentUser, LoginRequest, LoginResponse } from './models';

const TOKEN_KEY = 'importaya.token';
const USER_KEY = 'importaya.user';
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  private readonly tokenSignal = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  private readonly userSignal = signal<CurrentUser | null>(AuthService.readStoredUser());

  readonly token = this.tokenSignal.asReadonly();
  readonly user = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!this.tokenSignal());
  readonly isAdmin = computed(() => this.userSignal()?.role === 'Admin');

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.api.login(request).pipe(
      tap((response) => {
        const user = AuthService.decodeToken(response.token);
        localStorage.setItem(TOKEN_KEY, response.token);
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        this.tokenSignal.set(response.token);
        this.userSignal.set(user);
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.tokenSignal.set(null);
    this.userSignal.set(null);
    void this.router.navigate(['/login']);
  }

  /** Refresca los claims del usuario autenticado (útil si cambian sus roles). */
  refreshMe(): void {
    this.api.me().subscribe((user) => {
      localStorage.setItem(USER_KEY, JSON.stringify(user));
      this.userSignal.set(user);
    });
  }

  private static readStoredUser(): CurrentUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as CurrentUser;
    } catch {
      localStorage.removeItem(USER_KEY);
      return null;
    }
  }

  private static decodeToken(token: string): CurrentUser {
    const payload = token.split('.')[1];
    if (!payload) {
      throw new Error('El token recibido no es un JWT válido.');
    }

    const base64 = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=');
    const binary = atob(padded);
    const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0));
    const claims = JSON.parse(new TextDecoder().decode(bytes)) as Record<string, string>;

    return {
      id: claims['sub'] ?? '',
      email: claims['email'] ?? claims['unique_name'] ?? '',
      displayName: claims['displayName'] ?? claims['email'] ?? '',
      role: claims[ROLE_CLAIM] ?? claims['role'] ?? '',
    };
  }
}
