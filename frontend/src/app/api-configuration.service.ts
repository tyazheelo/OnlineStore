import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { map, Observable, tap } from 'rxjs';

export interface ApiConfiguration {
  productApiUrl: string;
  identityApiUrl: string;
  cartApiUrl: string;
  orderApiUrl: string;
  supportApiUrl: string;
}

@Injectable({ providedIn: 'root' })
export class ApiConfigurationService {
  private readonly http = inject(HttpClient);
  private configuration: ApiConfiguration | null = null;

  load(): Observable<void> {
    return this.http.get<unknown>('/shop-config.json').pipe(
      map((value) => this.validate(value)),
      tap((configuration) => {
        this.configuration = configuration;
      }),
      map(() => undefined),
    );
  }

  get productApiUrl(): string {
    return this.requireConfiguration().productApiUrl;
  }

  get identityApiUrl(): string {
    return this.requireConfiguration().identityApiUrl;
  }

  get cartApiUrl(): string {
    return this.requireConfiguration().cartApiUrl;
  }

  get orderApiUrl(): string {
    return this.requireConfiguration().orderApiUrl;
  }

  get supportApiUrl(): string {
    return this.requireConfiguration().supportApiUrl;
  }

  private requireConfiguration(): ApiConfiguration {
    if (!this.configuration) {
      throw new Error('API configuration has not been loaded.');
    }

    return this.configuration;
  }

  private validate(value: unknown): ApiConfiguration {
    if (!this.isApiConfiguration(value)) {
      throw new Error('The shop API configuration must be a JSON object.');
    }

    const keys: (keyof ApiConfiguration)[] = [
      'productApiUrl',
      'identityApiUrl',
      'cartApiUrl',
      'orderApiUrl',
      'supportApiUrl',
    ];

    for (const key of keys) {
      try {
        const url = new URL(value[key]);
        if (url.protocol !== 'http:' && url.protocol !== 'https:') {
          throw new Error();
        }
      } catch {
        throw new Error(
          `The shop API configuration value "${key}" must be an absolute HTTP or HTTPS URL.`,
        );
      }
    }

    return value;
  }

  private isApiConfiguration(value: unknown): value is ApiConfiguration {
    if (!value || typeof value !== 'object') return false;

    const candidate = value as Record<string, unknown>;
    return ['productApiUrl', 'identityApiUrl', 'cartApiUrl', 'orderApiUrl', 'supportApiUrl'].every(
      (key) => typeof candidate[key] === 'string' && candidate[key].trim() !== '',
    );
  }
}
