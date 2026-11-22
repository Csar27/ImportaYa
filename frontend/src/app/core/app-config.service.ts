import { Injectable } from '@angular/core';

export interface AppConfig {
  apiUrl: string;
}

const DEFAULT_CONFIG: AppConfig = {
  apiUrl: 'http://localhost:5080/api',
};

/**
 * Configuración de runtime. Se carga desde `config.json` antes de arrancar la app,
 * así cambiar el puerto de la API no obliga a recompilar el frontend.
 */
@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private config: AppConfig = DEFAULT_CONFIG;

  async load(): Promise<void> {
    try {
      const response = await fetch('config.json', { cache: 'no-store' });
      if (!response.ok) {
        return;
      }
      const loaded = (await response.json()) as Partial<AppConfig>;
      if (loaded.apiUrl?.trim()) {
        this.config = { apiUrl: loaded.apiUrl.trim() };
      }
    } catch {
      // Si no hay config.json se mantiene el valor por defecto.
    }
  }

  get apiUrl(): string {
    return this.config.apiUrl;
  }
}
