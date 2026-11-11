import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';

const I18N_SOURCE = readFileSync(new URL('./i18n.ts', import.meta.url), 'utf-8');
const LANGUAGE_BLOCK = /^ {4}(\w+): \{\n([\s\S]*?)\n {4}\},?$/gm;
const ENTRY = /^ {6}(\w+):\s+('(?:[^'\\]|\\.)*'),$/gm;

function readOwnTranslations(): Record<string, Record<string, string>> {
  const result: Record<string, Record<string, string>> = {};
  for (const [, language, body] of I18N_SOURCE.matchAll(LANGUAGE_BLOCK)) {
    result[language] = Object.fromEntries(Array.from(body.matchAll(ENTRY), ([, key, value]) => [key, value]));
  }

  return result;
}

describe('own translations in i18n.ts', () => {
  it('languages_InSource_AreEnglishAndUkrainian', () => {
    const languages = Object.keys(readOwnTranslations()).sort();

    expect(languages).toEqual(['en', 'uk']);
  });

  it('keys_InUkrainian_MatchEnglish', () => {
    const translations = readOwnTranslations();

    const ukrainianKeys = Object.keys(translations['uk']).sort();

    expect(ukrainianKeys).toEqual(Object.keys(translations['en']).sort());
    expect(ukrainianKeys.length).toBeGreaterThan(0);
  });

  it('values_InEveryLanguage_AreNotBlank', () => {
    const translations = readOwnTranslations();

    const blankValues = Object.values(translations)
      .flatMap((texts) => Object.values(texts))
      .filter((value) => value.replace(/['"`]/g, '').trim() === '');

    expect(blankValues).toEqual([]);
  });

  it('values_InAnyLanguage_EndWithoutAPeriod', () => {
    const withPeriod = Object.entries(readOwnTranslations()).flatMap(([language, entries]) =>
      Object.entries(entries)
        .filter(([, value]) => /\.'$/.test(value) && !/\.\.'$/.test(value))
        .map(([key]) => `${language}.${key}`),
    );

    expect(withPeriod).toEqual([]);
  });
});
