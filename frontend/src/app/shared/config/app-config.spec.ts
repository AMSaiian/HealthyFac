import { AppConfigError, parseAppConfig } from './app-config';

const VALID_URL = 'https://api.healthyfac.test';

describe('parseAppConfig', () => {
  it('parseAppConfig_WithValidHttpsUrl_ReturnsConfig', () => {
    const result = parseAppConfig({ apiBaseUrl: VALID_URL });

    expect(result).toEqual({ apiBaseUrl: VALID_URL });
  });

  it.each([{}, { apiBaseUrl: 5 }, null, 'text'])(
    'parseAppConfig_WithMissingOrNonStringUrl_ThrowsNamingField (%j)',
    (raw) => {
      const act = (): unknown => parseAppConfig(raw);

      expect(act).toThrow(AppConfigError);
      expect(act).toThrow(/apiBaseUrl/);
    },
  );

  it.each(['http://api.healthyfac.test', '/api', 'api.healthyfac.test'])(
    'parseAppConfig_WithHttpOrRelativeUrl_Throws (%s)',
    (apiBaseUrl) => {
      const act = (): unknown => parseAppConfig({ apiBaseUrl });

      expect(act).toThrow(/apiBaseUrl/);
    },
  );

  it.each(['https://localhost', 'https://localhost:5000', 'https://127.0.0.1', 'https://[::1]'])(
    'parseAppConfig_WithLocalhost_Throws (%s)',
    (apiBaseUrl) => {
      const act = (): unknown => parseAppConfig({ apiBaseUrl });

      expect(act).toThrow(/apiBaseUrl/);
    },
  );

  it('parseAppConfig_WithTrailingSlash_Throws', () => {
    const act = (): unknown => parseAppConfig({ apiBaseUrl: `${VALID_URL}/` });

    expect(act).toThrow(/apiBaseUrl/);
  });
});
