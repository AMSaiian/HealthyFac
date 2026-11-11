import { fitChipCount, summarizeChips, toChips } from '@shared/chips/chip-summary/chip-summary.utils';

const GAP = 4;
const MORE = 30;

describe('fitChipCount', () => {
  it('fitChipCount_WhenAllChipsFitWithoutTheMoreChip_ShowsAll', () => {
    const count = fitChipCount({ availableWidth: 100, chipWidths: [40, 40], gap: GAP, moreWidth: MORE });

    expect(count).toBe(2);
  });

  it('fitChipCount_WhenOneChipTooMany_ShowsTheMostThatFitNextToTheMoreChip', () => {
    const count = fitChipCount({ availableWidth: 100, chipWidths: [30, 30, 40], gap: GAP, moreWidth: MORE });

    expect(count).toBe(2);
  });

  it('fitChipCount_WhenNoChipFitsNextToTheMoreChip_ShowsNone', () => {
    const count = fitChipCount({ availableWidth: 50, chipWidths: [40, 40], gap: GAP, moreWidth: MORE });

    expect(count).toBe(0);
  });

  it('fitChipCount_WhenTheChipsFillTheWidthExactly_ShowsAllAndOnePixelLessShowsFewer', () => {
    const fits = fitChipCount({ availableWidth: 84, chipWidths: [40, 40], gap: GAP, moreWidth: MORE });
    const tooNarrow = fitChipCount({ availableWidth: 83, chipWidths: [40, 40], gap: GAP, moreWidth: MORE });

    expect(fits).toBe(2);
    expect(tooNarrow).toBe(1);
  });
});

describe('summarizeChips', () => {
  it('summarizeChips_WithSomeChipsHidden_CountsTheRestAndListsAllInTheTooltip', () => {
    const chips = toChips(['a', 'b', 'c'], 'x-', (value) => value.toUpperCase());

    const summary = summarizeChips(chips, 1);

    expect(summary.shown).toEqual([{ label: 'A', styleClass: 'x-a' }]);
    expect(summary.hiddenCount).toBe(2);
    expect(summary.tooltip).toBe('A, B, C');
  });

  it('summarizeChips_WithAllChipsShown_HidesNothingAndHasNoTooltip', () => {
    const summary = summarizeChips(
      toChips(['a', 'b'], 'x-', (value) => value),
      2,
    );

    expect(summary.hiddenCount).toBe(0);
    expect(summary.tooltip).toBe('');
  });
});
