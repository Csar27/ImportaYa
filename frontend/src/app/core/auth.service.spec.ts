import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';

import { AppConfigService } from './app-config.service';
import { AuthService } from './auth.service';

function fakeJwt(role: string): string {
  const payload = {
    sub: '11111111-1111-1111-1111-111111111111',
    email: 'admin@demo.com',
    displayName: 'Administrador',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': role,
  };
  const encoded = btoa(JSON.stringify(payload)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `header.${encoded}.signature`;
}

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AppConfigService, useValue: { apiUrl: 'http://localhost:5080/api' } },
      ],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('guarda el token y los claims tras iniciar sesión', () => {
    service.login({ email: 'admin@demo.com', password: 'Admin12345' }).subscribe();

    const request = httpMock.expectOne('http://localhost:5080/api/auth/login');
    expect(request.request.method).toBe('POST');
    request.flush({
      token: fakeJwt('Admin'),
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      email: 'admin@demo.com',
      displayName: 'Administrador',
      role: 'Admin',
    });

    expect(service.isAuthenticated()).toBe(true);
    expect(service.isAdmin()).toBe(true);
    expect(service.user()?.email).toBe('admin@demo.com');
    expect(localStorage.getItem('importaya.token')).toContain('header.');
  });

  it('limpia la sesión al cerrar sesión', () => {
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('importaya.token')).toBeNull();
  });
});
