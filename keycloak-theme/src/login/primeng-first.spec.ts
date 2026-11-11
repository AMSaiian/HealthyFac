import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

const NATIVE_CONTROL = /<(select|textarea|input)\b[^>]*>/g;
const PRIMENG_DIRECTIVE = /\b(pInputText|pInputPassword|pTextarea|pInputNumber|pCheckbox|pRadioButton)\b/;
const HIDDEN_OR_SUBMIT = /type="(hidden|file)"/;
const SOURCE_ROOT = join(import.meta.dirname, '..');

function templatesUnder(directory: string): string[] {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) {
      return templatesUnder(path);
    }
    return entry.name.endsWith('.html') ? [path] : [];
  });
}

/** Lists the native `select`, `textarea`, and `input` tags that carry no PrimeNG directive. */
function nativeControlsWithoutPrimeNg(source: string): string[] {
  return [...source.matchAll(NATIVE_CONTROL)]
    .map((match) => match[0])
    .filter((tag) => !PRIMENG_DIRECTIVE.test(tag) && !HIDDEN_OR_SUBMIT.test(tag));
}

describe('PrimeNG first in the login theme', () => {
  it('primengFirst_EveryFormControl_UsesAPrimeNgDirective', () => {
    const offenders = templatesUnder(SOURCE_ROOT).flatMap((path) =>
      nativeControlsWithoutPrimeNg(readFileSync(path, 'utf-8')).map((tag) => `${path}: ${tag.replace(/\s+/g, ' ')}`),
    );

    expect(offenders).toEqual([]);
  });

  it('nativeControlsWithoutPrimeNg_WithABareCheckbox_ReportsIt', () => {
    const offenders = nativeControlsWithoutPrimeNg('<input type="checkbox" name="rememberMe" />');

    expect(offenders).toHaveLength(1);
  });
});
