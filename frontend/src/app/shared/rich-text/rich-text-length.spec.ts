import { visibleText, visibleTextLength } from './rich-text-length';

describe('visibleText', () => {
  it('visibleText_WithParagraphsAndEntities_ReturnsJoinedDecodedText', () => {
    const text = visibleText('<p>Tom &amp; Jerry</p><p>a &lt; b</p>');

    expect(text).toBe('Tom & Jerry a < b');
  });
});

describe('visibleTextLength', () => {
  it('visibleTextLength_WithNull_ReturnsZero', () => {
    const length = visibleTextLength(null);

    expect(length).toBe(0);
  });

  it.each([
    ['<p>a</p>', 1],
    ['<p><br></p>', 0],
    ['<p>&amp;</p>', 1],
    ['<ol><li data-list="bullet"><span class="ql-ui" contenteditable="false"></span>ab</li></ol>', 2],
    ['<p>e\u0301</p>', 1],
    ['<p>e&#x301;</p>', 1],
    ['<p>\u0958</p>', 2],
    ['<p><strong>ab</strong> <em>c</em></p>', 4],
  ])('visibleTextLength_With%j_Returns%i', (html, expected) => {
    const length = visibleTextLength(html);

    expect(length).toBe(expected);
  });
});
