/// <reference types="vite/client" />
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

const GENERIC_ROOTS = ['ACTIONS', 'FORMS', 'TOASTS'];
const TOOLTIPS_SEGMENT = 'TOOLTIPS';
/** Keys that share a word with a generic key but mean something else. */
const OTHER_MEANING_KEYS = ['STATUS_DEFINITIONS.SEVERITIES.ERROR', 'METRICS.FORM.DONE'];

const isGenericKey = (key: string): boolean => GENERIC_ROOTS.includes(key.split('.')[0]);

/**
 * The feature keys (outside `ACTIONS`, `FORMS`, `TOASTS`, and `PRIMENG`) whose value equals the value of a generic
 * key. The tooltips of `ACTIONS` do not count as generic values.
 */
function featureKeysWithGenericValue(entries: [string, string][]): string[] {
  const genericValues = new Set(
    entries.filter(([key]) => isGenericKey(key) && !key.split('.').includes(TOOLTIPS_SEGMENT)).map(([, text]) => text),
  );
  return entries
    .filter(([key]) => !isGenericKey(key) && !key.startsWith(`${PRIMENG_ROOT}.`))
    .filter(([key]) => !OTHER_MEANING_KEYS.includes(key))
    .filter(([, text]) => genericValues.has(text))
    .map(([key]) => key);
}

/** The tooltip keys (a key with a `TOOLTIPS` segment) whose value another tooltip key already holds. */
function tooltipKeysWithSameValue(entries: [string, string][]): string[] {
  const tooltips = entries.filter(([key]) => key.split('.').includes(TOOLTIPS_SEGMENT));
  return tooltips
    .filter(([key, text]) => tooltips.some(([otherKey, otherText]) => otherKey !== key && otherText === text))
    .map(([key]) => key);
}

const SOURCES = {
  ...import.meta.glob('../../**/*.ts', { query: '?raw', import: 'default', eager: true }),
  ...import.meta.glob('../../**/*.html', { query: '?raw', import: 'default', eager: true }),
} as Record<string, string>;
const SPEC_SUFFIX = '.spec.ts';
const KEY_LITERAL = /(['"`])([A-Z][A-Z0-9_]*(?:\.[A-Z0-9_]+)+)\1/g;
const TEMPLATE_KEY = /`[A-Z][A-Z0-9_]*\.[^`]*\$\{[^`]*`/g;

/** The files whose template key a value from the API builds. They are the only exemption of the template check. */
const API_VALUE_KEY_FILES: Readonly<Record<string, string>> = {
  '/files/ingest/summary/ingest-summary-host/ingest-summary-host.ts':
    'the value comes from the API; an unknown value shows its key',
  '/machines/metrics/metric-form/metric-form.utils.ts': 'the value comes from the API; an unknown value shows its key',
};

function sourceEntries(): [string, string][] {
  return Object.entries(SOURCES).filter(([path]) => !path.endsWith(SPEC_SUFFIX));
}

function sourceTexts(): string[] {
  return sourceEntries().map(([, text]) => text);
}

function isApiValueFile(path: string): boolean {
  return Object.keys(API_VALUE_KEY_FILES).some((suffix) => path.endsWith(suffix));
}

function missingKeysIn(sources: readonly string[], keys: ReadonlySet<string>): string[] {
  const literals = sources.flatMap((text) => [...text.matchAll(KEY_LITERAL)].map((match) => match[2]));
  return [...new Set(literals.filter((key) => !keys.has(key)))].sort();
}

function templateKeysIn(sources: readonly string[]): string[] {
  return sources.flatMap((text) => text.match(TEMPLATE_KEY) ?? []);
}

describe('translation files', () => {
  it('tooltipKeys_WithTheSameEnValue_AreReported', () => {
    const reported = tooltipKeysWithSameValue(flattenValues(en));

    expect(reported).toEqual([]);
  });

  it('tooltipKeysWithSameValue_WithTwoTooltipsOfOneText_ReportsBothKeys', () => {
    const entries: [string, string][] = [
      ['FILES.TOOLTIPS.UPLOAD', 'Upload files'],
      ['MACHINES.TOOLTIPS.UPLOAD', 'Upload files'],
      ['MACHINES.TOOLTIPS.OPEN', 'Open the machine'],
      ['MACHINES.TITLE', 'Upload files'],
    ];

    const reported = tooltipKeysWithSameValue(entries);

    expect(reported).toEqual(['FILES.TOOLTIPS.UPLOAD', 'MACHINES.TOOLTIPS.UPLOAD']);
  });

  it('featureKeys_WithTheValueOfAGenericKey_AreReported', () => {
    const reported = featureKeysWithGenericValue(flattenValues(en));

    expect(reported).toEqual([]);
  });

  it('featureKeysWithGenericValue_WithAFeatureKeyOfAGenericValue_ReportsTheKey', () => {
    const entries: [string, string][] = [
      ['ACTIONS.DELETE', 'Delete'],
      ['ACTIONS.TOOLTIPS.OPEN', 'Open the details'],
      ['MACHINES.DELETE', 'Delete'],
      ['MACHINES.TOOLTIPS.OPEN', 'Open the details'],
      ['METRICS.FORM.DONE', 'Delete'],
      ['PRIMENG.accept', 'Delete'],
      ['MACHINES.TITLE', 'Machines'],
    ];

    const reported = featureKeysWithGenericValue(entries);

    expect(reported).toEqual(['MACHINES.DELETE']);
  });

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

  it('everyKeyInTheCode_ExistsInTheTranslationFiles', () => {
    const enKeys = new Set(flattenKeys(en));

    const missing = missingKeysIn(sourceTexts(), enKeys);

    expect(missing).toEqual([]);
  });

  it('missingKeysIn_WithAKeyAbsentFromEn_ReportsIt', () => {
    const enKeys = new Set(['FILES.TITLE']);

    const missing = missingKeysIn(["const a = 'FILES.TITLE'; const b = 'MISSING.KEY';"], enKeys);

    expect(missing).toEqual(['MISSING.KEY']);
  });

  it('apiValueKeyFiles_EachListedFileExists', () => {
    const missing = Object.keys(API_VALUE_KEY_FILES).filter(
      (suffix) => !sourceEntries().some(([path]) => path.endsWith(suffix)),
    );

    expect(missing).toEqual([]);
  });

  it('isApiValueFile_WithAnotherFile_IsNotExempt', () => {
    expect(isApiValueFile('../../machines/metrics/metric-form/metric-form.utils.ts')).toBe(true);
    expect(isApiValueFile('../../machines/metrics/metric-form/metric-form.ts')).toBe(false);
  });

  it('noKeyIsBuiltFromATemplate', () => {
    const built = templateKeysIn(
      sourceEntries()
        .filter(([path]) => !isApiValueFile(path))
        .map(([, text]) => text),
    );

    expect(built).toEqual([]);
  });

  it('templateKeysIn_WithAVariableAfterAKeySegment_ReportsIt', () => {
    const built = templateKeysIn(['const key = `WORK_ORDERS.CONFIRM.${upper}_ACCEPT`;']);

    expect(built).toEqual(['`WORK_ORDERS.CONFIRM.${upper}_ACCEPT`']);
  });
});
