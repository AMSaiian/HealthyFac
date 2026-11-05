import { InjectionToken } from '@angular/core';
import * as z from 'zod/mini';

const HTTPS_PROTOCOL = 'https:';
const LOOPBACK_HOSTS: readonly string[] = ['localhost', '127.0.0.1', '[::1]'];

const ConfigMessage = {
  UrlNotString: 'must be a string',
  UrlNotHttps: 'must be an absolute https URL',
  UrlLoopback: 'must not use localhost or a loopback address',
  UrlTrailingSlash: 'must not end with /',
  TextBlank: 'must be a non-blank string',
};

const publicHttpsUrl = z.string(ConfigMessage.UrlNotString).check(
  z.refine((value) => URL.parse(value)?.protocol === HTTPS_PROTOCOL, { error: ConfigMessage.UrlNotHttps, abort: true }),
  z.refine((value) => !LOOPBACK_HOSTS.includes(URL.parse(value)?.hostname ?? ''), {
    error: ConfigMessage.UrlLoopback,
    abort: true,
  }),
  z.refine((value) => !value.endsWith('/'), { error: ConfigMessage.UrlTrailingSlash }),
);

const nonBlankText = z
  .string(ConfigMessage.TextBlank)
  .check(z.refine((value) => value.trim() !== '', { error: ConfigMessage.TextBlank }));

const appConfigSchema = z.object({
  apiBaseUrl: publicHttpsUrl,
  authUrl: publicHttpsUrl,
  authRealm: nonBlankText,
  authClientId: nonBlankText,
  primeUiLicenseKey: nonBlankText,
});

export type AppConfig = Readonly<z.infer<typeof appConfigSchema>>;

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');

export const APP_CONFIG_PATH = '/config.json';

export class AppConfigError extends Error {}

export function parseAppConfig(raw: unknown): AppConfig {
  const source = typeof raw === 'object' && raw !== null ? raw : {};
  const result = appConfigSchema.safeParse(source);
  if (!result.success) {
    const [issue] = result.error.issues;
    throw new AppConfigError(`Invalid ${issue.path.map(String).join('.')}: ${issue.message}.`);
  }
  return result.data;
}
