import { DEFAULT_LANGUAGE, Language } from './i18n.constants';

function isLanguage(raw: string | null): raw is Language {
  return Object.values<string>(Language).includes(raw ?? '');
}

export const languageSerializer = {
  read: (raw: string | null): { language: Language } => ({ language: isLanguage(raw) ? raw : DEFAULT_LANGUAGE }),
  write: (state: { language: Language }): string => state.language,
};
