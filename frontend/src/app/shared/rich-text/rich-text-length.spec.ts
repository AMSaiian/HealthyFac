import { richTextLength } from './rich-text-length';

describe('richTextLength', () => {
  it('richTextLength_WithNull_ReturnsZero', () => {
    const length = richTextLength(null);

    expect(length).toBe(0);
  });

  it('richTextLength_WithHtml_CountsMarkup', () => {
    const length = richTextLength('<p><strong>ab</strong></p>');

    expect(length).toBe(26);
  });
});
