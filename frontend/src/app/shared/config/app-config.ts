import { InjectionToken } from '@angular/core';

export interface AppConfig {
  readonly apiBaseUrl: string;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

export const APP_CONFIG_PATH = '/config.json';

const API_BASE_URL_FIELD = 'apiBaseUrl';
const HTTPS_PROTOCOL = 'https:';
const LOOPBACK_HOSTS: readonly string[] = ['localhost', '127.0.0.1', '[::1]'];

export class AppConfigError extends Error {}

function describeApiBaseUrlProblem(value: unknown): string | null {
  if (typeof value !== 'string') {
    return 'must be a string';
  }
  const url = URL.parse(value);
  if (url === null || url.protocol !== HTTPS_PROTOCOL) {
    return 'must be an absolute https URL';
  }
  if (LOOPBACK_HOSTS.includes(url.hostname)) {
    return 'must not use localhost or a loopback address';
  }
  return value.endsWith('/') ? 'must not end with /' : null;
}

export function parseAppConfig(raw: unknown): AppConfig {
  const value =
    typeof raw === 'object' && raw !== null ? (raw as Record<string, unknown>)[API_BASE_URL_FIELD] : undefined;
  const problem = describeApiBaseUrlProblem(value);
  if (problem !== null) {
    throw new AppConfigError(`Invalid ${API_BASE_URL_FIELD}: ${problem}.`);
  }
  return { apiBaseUrl: value as string };
}
