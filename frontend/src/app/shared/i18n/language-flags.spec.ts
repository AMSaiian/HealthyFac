import { Language } from './i18n.constants';
import { LANGUAGE_FLAGS, languageFlagUrl } from './language-flags';

describe('LANGUAGE_FLAGS', () => {
  it.each(Object.values(Language))('has_ForEveryLanguage_ANonEmptyFlagCode (%s)', (language) => {
    const flag = LANGUAGE_FLAGS[language];

    expect(flag).toMatch(/^[a-z]{2}$/);
  });
});

describe('languageFlagUrl', () => {
  it('languageFlagUrl_WithUkrainian_ReturnsTheAssetPath', () => {
    const result = languageFlagUrl(Language.Ukrainian);

    expect(result).toBe('flags/ua.svg');
  });
});
