import {
  chipGapOf,
  fitChipCount,
  labelChipsOf,
  summarizeChips,
  toChips,
} from '@shared/chips/chip-summary/chip-summary.utils';

const GAP = 4;
const MORE = 30;

describe('labelChipsOf', () => {
  it('labelChipsOf_WithAKnownValue_ShowsTheOptionLabel', () => {
    const chips = labelChipsOf(['repair'], [{ value: 'repair', label: 'Repair' }]);

    expect(chips.map((chip) => chip.label)).toEqual(['Repair']);
  });

  it('labelChipsOf_WithValuesInClickOrder_ReturnsThemInOptionOrder', () => {
    const chips = labelChipsOf(
      ['c', 'a'],
      [
        { value: 'a', label: 'Alpha' },
        { value: 'b', label: 'Beta' },
        { value: 'c', label: 'Gamma' },
      ],
    );

    expect(chips.map((chip) => chip.label)).toEqual(['Alpha', 'Gamma']);
  });

  it('labelChipsOf_WithASeverityOption_CarriesTheSeverityToTheChip', () => {
    const chips = labelChipsOf(['queued'], [{ value: 'queued', label: 'Queued', severity: 'info' }]);

    expect(chips[0].severity).toBe('info');
  });

  it('labelChipsOf_WithAValueWithoutOption_ShowsTheValue', () => {
    const chips = labelChipsOf(['unknown'], [{ value: 'repair', label: 'Repair' }]);

    expect(chips.map((chip) => chip.label)).toEqual(['unknown']);
  });
});

describe('chipGapOf', () => {
  it('chipGapOf_WithTextAppearance_IsZeroBecauseTheSeparatorsAreInTheWidths', () => {
    expect(chipGapOf('text', GAP)).toBe(0);
  });

  it('chipGapOf_WithTagAppearance_IsTheColumnGap', () => {
    expect(chipGapOf('tag', GAP)).toBe(GAP);
  });
});

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

describe('toChips', () => {
  it('toChips_WithValuesInClickOrder_ReturnsThemInOptionOrder', () => {
    const chips = toChips(['c', 'a'], 'x-', (value) => value.toUpperCase(), ['a', 'b', 'c']);

    expect(chips).toEqual([
      { label: 'A', styleClass: 'x-a' },
      { label: 'C', styleClass: 'x-c' },
    ]);
  });
});

describe('summarizeChips', () => {
  it('summarizeChips_WithSomeChipsHidden_CountsTheRestAndListsAllInTheTooltip', () => {
    const chips = toChips(['a', 'b', 'c'], 'x-', (value) => value.toUpperCase(), ['a', 'b', 'c']);

    const summary = summarizeChips(chips, 1);

    expect(summary.shown).toEqual([{ label: 'A', styleClass: 'x-a' }]);
    expect(summary.hiddenCount).toBe(2);
    expect(summary.tooltip).toBe('A, B, C');
  });

  it('summarizeChips_WithAllChipsShown_HidesNothingAndHasNoTooltip', () => {
    const summary = summarizeChips(
      toChips(['a', 'b'], 'x-', (value) => value, ['a', 'b']),
      2,
    );

    expect(summary.hiddenCount).toBe(0);
    expect(summary.tooltip).toBe('');
  });
});
