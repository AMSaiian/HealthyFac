import { describe, expect, it, vi } from 'vitest';

import { languageMenuItems, toLanguageLabel } from './language-select.utils';

describe('toLanguageLabel', () => {
  it('toLanguageLabel_WithLowerCaseUkrainianName_UpperCasesTheFirstLetter', () => {
    const label = toLanguageLabel('українська', 'uk');

    expect(label).toBe('Українська');
  });

  it('toLanguageLabel_WithCapitalizedName_KeepsIt', () => {
    const label = toLanguageLabel('English', 'en');

    expect(label).toBe('English');
  });
});

describe('languageMenuItems', () => {
  const languages = [
    { languageTag: 'en', label: 'English', href: '/en' },
    { languageTag: 'uk', label: 'українська', href: '/uk' },
  ];

  it('languageMenuItems_WithTwoLanguages_MarksTheCurrentOneAndCapitalizesTheNames', () => {
    const items = languageMenuItems(
      languages,
      'uk',
      (tag) => `/flags/${tag}.svg`,
      () => undefined,
    );

    expect(items.map((item) => item.label)).toEqual(['English', 'Українська']);
    expect(items.map((item) => (item.state as { isCurrent: boolean }).isCurrent)).toEqual([false, true]);
  });

  it('languageMenuItems_OnCommand_ChoosesTheHrefOfThatLanguage', () => {
    const choose = vi.fn();
    const items = languageMenuItems(languages, 'en', () => null, choose);

    items[1].command?.({ originalEvent: new Event('click'), item: items[1] });

    expect(choose).toHaveBeenCalledWith('/uk');
  });
});
