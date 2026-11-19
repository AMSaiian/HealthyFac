const CONTENT_STYLESHEET = 'src/rich-text-content.css';
const NODE_FS = 'node:fs';
const COMMENT = /\/\*[\s\S]*?\*\//g;
const EDITOR_SELECTOR_START = 'hf-rich-text-editor';
const VIEW_SELECTOR_PART = '.hf-rich-text';

/** Lists the selector lists of the rules that name only one of the two targets: the editor or the view. */
function rulesNamingOneTarget(css: string): string[] {
  return css
    .replace(COMMENT, '')
    .split('}')
    .map((rule) => rule.split('{')[0].trim())
    .filter((selectorList) => selectorList !== '')
    .filter((selectorList) => {
      const selectors = selectorList.split(',').map((selector) => selector.trim());
      const hasEditor = selectors.some((selector) => selector.startsWith(EDITOR_SELECTOR_START));
      const hasView = selectors.some((selector) => selector.includes(VIEW_SELECTOR_PART));
      return !hasEditor || !hasView;
    });
}

/**
 * Reads the stylesheet from the disk. An import of a `.css` file with `?raw` gives an empty text in the Angular test
 * builder, because the builder handles every stylesheet itself. The tests run in Node with the project folder as the
 * working directory.
 */
async function readContentStylesheet(): Promise<string> {
  const fs = (await import(/* @vite-ignore */ NODE_FS)) as { readFileSync(path: string, encoding: string): string };
  return fs.readFileSync(CONTENT_STYLESHEET, 'utf8');
}

describe('rich text content style', () => {
  it('contentRules_EveryRule_NamesTheEditorAndTheView', async () => {
    const css = await readContentStylesheet();

    const offenders = rulesNamingOneTarget(css);

    expect(css.length).toBeGreaterThan(0);
    expect(offenders).toEqual([]);
  });

  it('rulesNamingOneTarget_WithAnEditorOnlyRule_ReportsIt', () => {
    const css = 'hf-rich-text-editor .ql-editor p { margin: 0; }';

    const offenders = rulesNamingOneTarget(css);

    expect(offenders).toEqual(['hf-rich-text-editor .ql-editor p']);
  });

  it('rulesNamingOneTarget_WithBothTargets_ReportsNothing', () => {
    const css = '/* note */ hf-rich-text-editor .ql-editor p,\n.hf-rich-text p { margin: 0; }';

    const offenders = rulesNamingOneTarget(css);

    expect(offenders).toEqual([]);
  });
});
