import { AppConfigError } from '@shared/config/app-config';

import { startErrorMessage } from './start-error-message';

const PROBLEM = 'Invalid authUrl: must be an absolute https URL.';

describe('startErrorMessage', () => {
  it('startErrorMessage_InDevelopmentWithConfigError_NamesTheProblem', () => {
    const result = startErrorMessage(new AppConfigError(PROBLEM), true);

    expect(result).toBe(`HealthyFac cannot start. The configuration is not valid: ${PROBLEM}`);
  });

  it('startErrorMessage_InProductionWithConfigError_ReturnsGenericText', () => {
    const result = startErrorMessage(new AppConfigError(PROBLEM), false);

    expect(result).toBe('HealthyFac cannot start. Reload the page or contact support.');
    expect(result).not.toContain(PROBLEM);
  });

  it.each([true, false])('startErrorMessage_WithOtherError_ReturnsGenericTextInBothModes (%s)', (isDevelopment) => {
    const result = startErrorMessage(new Error('network down'), isDevelopment);

    expect(result).toBe('HealthyFac cannot start. Reload the page or contact support.');
  });
});
