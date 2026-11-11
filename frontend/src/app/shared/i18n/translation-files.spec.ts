import en from '@public/i18n/en.json';
import uk from '@public/i18n/uk.json';

const PRIMENG_ROOT = 'PRIMENG';
const UPPER_SNAKE_CASE = /^[A-Z][A-Z0-9_]*$/;

function flattenValues(value: unknown, prefix = ''): [string, string][] {
  if (typeof value === 'string') {
    return [[prefix, value]];
  }
  if (typeof value !== 'object' || value === null) {
    return [];
  }
  return Object.entries(value).flatMap(([key, child]) => flattenValues(child, prefix ? `${prefix}.${key}` : key));
}

function flattenKeys(value: unknown, prefix = ''): string[] {
  if (typeof value !== 'object' || value === null) {
    return [prefix];
  }
  return Object.entries(value).flatMap(([key, child]) => flattenKeys(child, prefix ? `${prefix}.${key}` : key));
}

describe('translation files', () => {
  it('translationFiles_AllLanguages_HaveSameKeys', () => {
    const enKeys = flattenKeys(en).sort();
    const ukKeys = flattenKeys(uk).sort();

    expect(ukKeys).toEqual(enKeys);
  });

  it.each([
    ['en', en],
    ['uk', uk],
  ])('translationFiles_AllKeys_UseUpperSnakeCase (%s)', (_language, file) => {
    const badKeys = flattenKeys(file).filter(
      (key) => !key.startsWith(`${PRIMENG_ROOT}.`) && !key.split('.').every((part) => UPPER_SNAKE_CASE.test(part)),
    );

    expect(badKeys).toEqual([]);
  });

  it.each([
    ['en', en],
    ['uk', uk],
  ])('translationFiles_AllValues_EndWithoutAPeriod (%s)', (_language, file) => {
    const withPeriod = flattenValues(file)
      .filter(([key]) => !key.startsWith(`${PRIMENG_ROOT}.`))
      .filter(([, text]) => text.endsWith('.') && !text.endsWith('..'))
      .map(([key]) => key);

    expect(withPeriod).toEqual([]);
  });
});
