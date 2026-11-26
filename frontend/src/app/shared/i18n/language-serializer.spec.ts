import { Language } from './i18n.constants';
import { languageSerializer } from './language-serializer';

describe('languageSerializer', { tags: ['Platform'] }, () => {
  it('read_WithUk_ReturnsUk', () => {
    const result = languageSerializer.read('uk');

    expect(result).toEqual({ language: Language.Ukrainian });
  });

  it.each(['de', null])('read_WithUnknownLanguageOrNull_ReturnsEnglish (%s)', (raw) => {
    const result = languageSerializer.read(raw);

    expect(result).toEqual({ language: Language.English });
  });
});
