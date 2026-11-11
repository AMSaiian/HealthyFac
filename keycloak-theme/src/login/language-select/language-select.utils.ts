import type { MenuItem } from 'primeng/api';

export interface LanguageOption {
  readonly languageTag: string;
  readonly label: string;
  readonly href: string;
}

/** Upper-cases the first letter, because Java locale data writes some language names in lower case ("українська"). */
export function toLanguageLabel(label: string, languageTag: string): string {
  return label.charAt(0).toLocaleUpperCase(languageTag) + label.slice(1);
}

/**
 * The items of the language menu: the name of each language, its flag in `state`, and a mark on the current one. The
 * flag code of a language without a flag is `null`.
 */
export function languageMenuItems(
  languages: readonly LanguageOption[],
  currentTag: string,
  flagUrlOf: (languageTag: string) => string | null,
  choose: (href: string) => void,
): MenuItem[] {
  return languages.map((language) => ({
    id: language.languageTag,
    label: toLanguageLabel(language.label, language.languageTag),
    state: { flagUrl: flagUrlOf(language.languageTag), isCurrent: language.languageTag === currentTag },
    command: (): void => choose(language.href),
  }));
}
