import { markChecklistItems, toggleChecklistItem } from './rich-text-checklist';

const LIST =
  '<p>Steps</p><ol><li data-list="unchecked">Open</li><li data-list="checked">Clean</li><li data-list="unchecked">Close</li></ol>';

describe('toggleChecklistItem', { tags: ['Platform'] }, () => {
  it.each([
    [
      'UncheckedItem_ChecksOnlyThatItem',
      0,
      '<p>Steps</p><ol><li data-list="checked">Open</li><li data-list="checked">Clean</li><li data-list="unchecked">Close</li></ol>',
    ],
    [
      'CheckedItem_UnchecksOnlyThatItem',
      1,
      '<p>Steps</p><ol><li data-list="unchecked">Open</li><li data-list="unchecked">Clean</li><li data-list="unchecked">Close</li></ol>',
    ],
  ])('toggleChecklistItem_With%s', (_name, index, expected) => {
    const result = toggleChecklistItem(LIST, index);

    expect(result).toBe(expected);
  });

  it('toggleChecklistItem_CalledTwice_RoundTripsTheHtml', () => {
    const result = toggleChecklistItem(toggleChecklistItem(LIST, 2), 2);

    expect(result).toBe(LIST);
  });

  it('toggleChecklistItem_WithIndexOutOfRange_ReturnsTheSameHtml', () => {
    const result = toggleChecklistItem(LIST, 3);

    expect(result).toBe(LIST);
  });

  it('toggleChecklistItem_WithOrdinaryListItems_IgnoresThem', () => {
    const html = '<ol><li>One</li></ol><ol><li data-list="unchecked">Two</li></ol>';

    const result = toggleChecklistItem(html, 0);

    expect(result).toBe('<ol><li>One</li></ol><ol><li data-list="checked">Two</li></ol>');
  });
});

describe('markChecklistItems', { tags: ['Platform'] }, () => {
  it('markChecklistItems_WithBothStates_AddsAClassForEach', () => {
    const result = markChecklistItems(
      '<ol><li data-list="checked">A</li><li data-list="unchecked">B</li><li>C</li></ol>',
    );

    expect(result).toBe(
      '<ol><li data-list="checked" class="hf-check-checked">A</li><li data-list="unchecked" class="hf-check-unchecked">B</li><li>C</li></ol>',
    );
  });
});
