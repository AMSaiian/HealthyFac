import { readdirSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

const PAGES_ROOT = join(import.meta.dirname, 'pages');
const FORM_TAG = /<form(?=[\s>])[^>]*>/g;

function pageFolders(): string[] {
  return readdirSync(PAGES_ROOT, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => join(PAGES_ROOT, entry.name));
}

/** Lists the forms of a page that Angular would take over: the page imports `FormsModule` and the form has no `ngNoForm`. */
function formsAngularWouldCancel(folder: string): string[] {
  const files = readdirSync(folder);
  const ts = files.filter((name) => name.endsWith('.ts')).map((name) => readFileSync(join(folder, name), 'utf-8'));
  const html = files.filter((name) => name.endsWith('.html')).map((name) => readFileSync(join(folder, name), 'utf-8'));
  if (!ts.some((source) => source.includes('FormsModule'))) {
    return [];
  }
  return html
    .flatMap((source) => [...source.matchAll(FORM_TAG)].map((match) => match[0]))
    .filter((tag) => !tag.includes('ngNoForm'));
}

describe('native form submit', () => {
  it('forms_OfPagesWithFormsModule_CarryNgNoForm', () => {
    const offenders = pageFolders().flatMap((folder) => formsAngularWouldCancel(folder));

    expect(offenders).toEqual([]);
  });
});
