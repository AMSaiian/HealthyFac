import { Language } from './i18n.constants';

export const LANGUAGE_FLAGS: Readonly<Record<Language, string>> = {
  [Language.English]: 'gb',
  [Language.Ukrainian]: 'ua',
};

const FLAG_DIRECTORY = 'flags';

export function languageFlagUrl(language: Language): string {
  return `${FLAG_DIRECTORY}/${LANGUAGE_FLAGS[language]}.svg`;
}
