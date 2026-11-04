import { AppConfigError } from '@shared/config/app-config';

const GENERIC_START_ERROR = 'HealthyFac cannot start. Reload the page or contact support.';
const CONFIG_START_ERROR_PREFIX = 'HealthyFac cannot start. The configuration is not valid: ';

export function startErrorMessage(error: unknown, isDevelopment: boolean): string {
  return isDevelopment && error instanceof AppConfigError
    ? `${CONFIG_START_ERROR_PREFIX}${error.message}`
    : GENERIC_START_ERROR;
}
