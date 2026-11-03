import { AppConfigError, parseAppConfig } from './app-config';

const VALID_URL = 'https://api.healthyfac.test';
const VALID_AUTH_URL = 'https://auth.healthyfac.test';
const VALID_CONFIG = {
  apiBaseUrl: VALID_URL,
  authUrl: VALID_AUTH_URL,
  authRealm: 'healthyfac',
  authClientId: 'healthyfac-spa',
};
const URL_FIELDS = ['apiBaseUrl', 'authUrl'] as const;
const TEXT_FIELDS = ['authRealm', 'authClientId'] as const;

describe('parseAppConfig', () => {
  it('parseAppConfig_WithValidConfig_ReturnsConfig', () => {
    const result = parseAppConfig(VALID_CONFIG);

    expect(result).toEqual(VALID_CONFIG);
  });

  it.each([null, 'text', 5])('parseAppConfig_WithNonObject_ThrowsNamingField (%j)', (raw) => {
    const act = (): unknown => parseAppConfig(raw);

    expect(act).toThrow(AppConfigError);
    expect(act).toThrow(/apiBaseUrl/);
  });

  describe.each(URL_FIELDS)('%s', (field) => {
    it.each([undefined, 5])('parseAppConfig_WithMissingOrNonStringUrl_ThrowsNamingField (%j)', (bad) => {
      const act = (): unknown => parseAppConfig({ ...VALID_CONFIG, [field]: bad });

      expect(act).toThrow(AppConfigError);
      expect(act).toThrow(new RegExp(field));
    });

    it.each(['http://auth.healthyfac.test', '/api', 'auth.healthyfac.test'])(
      'parseAppConfig_WithHttpOrRelativeUrl_Throws (%s)',
      (bad) => {
        const act = (): unknown => parseAppConfig({ ...VALID_CONFIG, [field]: bad });

        expect(act).toThrow(new RegExp(field));
      },
    );

    it.each(['https://localhost', 'https://localhost:5000', 'https://127.0.0.1', 'https://[::1]'])(
      'parseAppConfig_WithLocalhost_Throws (%s)',
      (bad) => {
        const act = (): unknown => parseAppConfig({ ...VALID_CONFIG, [field]: bad });

        expect(act).toThrow(new RegExp(field));
      },
    );

    it('parseAppConfig_WithTrailingSlash_Throws', () => {
      const act = (): unknown => parseAppConfig({ ...VALID_CONFIG, [field]: `${VALID_URL}/` });

      expect(act).toThrow(new RegExp(field));
    });
  });

  describe.each(TEXT_FIELDS)('%s', (field) => {
    it.each(['', '  ', undefined])('parseAppConfig_WithBlankOrMissingValue_ThrowsNamingField (%j)', (bad) => {
      const act = (): unknown => parseAppConfig({ ...VALID_CONFIG, [field]: bad });

      expect(act).toThrow(AppConfigError);
      expect(act).toThrow(new RegExp(field));
    });
  });
});
