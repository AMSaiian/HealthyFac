import { markChecklistItems, toggleChecklistItem } from './rich-text-checklist';

const LIST =
  '<p>Steps</p><ol><li data-list="unchecked">Open</li><li data-list="checked">Clean</li><li data-list="unchecked">Close</li></ol>';

describe('toggleChecklistItem', () => {
  it('toggleChecklistItem_WithUncheckedItem_ChecksOnlyThatItem', () => {
    const result = toggleChecklistItem(LIST, 0);

    expect(result).toBe(
      '<p>Steps</p><ol><li data-list="checked">Open</li><li data-list="checked">Clean</li><li data-list="unchecked">Close</li></ol>',
    );
  });

  it('toggleChecklistItem_WithCheckedItem_UnchecksOnlyThatItem', () => {
    const result = toggleChecklistItem(LIST, 1);

    expect(result).toBe(
      '<p>Steps</p><ol><li data-list="unchecked">Open</li><li data-list="unchecked">Clean</li><li data-list="unchecked">Close</li></ol>',
    );
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

describe('markChecklistItems', () => {
  it('markChecklistItems_WithBothStates_AddsAClassForEach', () => {
    const result = markChecklistItems(
      '<ol><li data-list="checked">A</li><li data-list="unchecked">B</li><li>C</li></ol>',
    );

    expect(result).toBe(
      '<ol><li data-list="checked" class="hf-check-checked">A</li><li data-list="unchecked" class="hf-check-unchecked">B</li><li>C</li></ol>',
    );
  });
});
