import { collectFileIds, imageAltOf, MISSING_IMAGE_CLASS, toDisplayHtml } from './rich-text-images';

const ID_A = 'v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA';
const ID_B = 'v1.BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB';

describe('collectFileIds', () => {
  it('collectFileIds_WithNoImages_ReturnsNothing', () => {
    expect(collectFileIds('<p>text</p>')).toEqual([]);
    expect(collectFileIds(null)).toEqual([]);
  });

  it('collectFileIds_WithDuplicateIds_ReturnsEachOnceInOrder', () => {
    const html = `<p><img data-file-id="${ID_B}" alt="b"></p><p><img data-file-id="${ID_A}"><img data-file-id="${ID_B}"></p>`;

    expect(collectFileIds(html)).toEqual([ID_B, ID_A]);
  });
});

describe('toDisplayHtml', () => {
  it('toDisplayHtml_WithLink_SetsSrcAndRemovesTheFileId', () => {
    const html = toDisplayHtml(
      `<p><img data-file-id="${ID_A}" alt="pump"></p>`,
      new Map([[ID_A, 'https://api/blob']]),
      'missing',
    );

    expect(html).toContain('src="https://api/blob"');
    expect(html).toContain('alt="pump"');
    expect(html).not.toContain('data-file-id');
  });

  it('toDisplayHtml_WithoutLink_MarksAMissingImageThatKeepsTheAlt', () => {
    const html = toDisplayHtml(`<p><img data-file-id="${ID_A}" alt="pump"></p>`, new Map(), 'missing');

    expect(html).toContain(MISSING_IMAGE_CLASS);
    expect(html).toContain('alt="pump"');
    expect(html).not.toContain('src=');
  });

  it('toDisplayHtml_WithNoImages_ReturnsTheSameMarkup', () => {
    expect(toDisplayHtml('<p><strong>a</strong></p>', new Map(), 'missing')).toBe('<p><strong>a</strong></p>');
  });
});

describe('toDisplayHtml with a missing image that has no alt', () => {
  it('toDisplayHtml_WithoutLinkAndAlt_UsesTheFallbackText', () => {
    const html = toDisplayHtml(`<p><img data-file-id="${ID_A}"></p>`, new Map(), 'not available');

    expect(html).toContain('alt="not available"');
  });
});

describe('imageAltOf', () => {
  it('imageAltOf_WithLongName_DropsTheExtensionAndCutsTo255', () => {
    const alt = imageAltOf(`${'a'.repeat(300)}.png`);

    expect(alt).toBe('a'.repeat(255));
  });

  it('imageAltOf_WithShortName_ReturnsTheBaseName', () => {
    expect(imageAltOf('scheme.png')).toBe('scheme');
  });
});
