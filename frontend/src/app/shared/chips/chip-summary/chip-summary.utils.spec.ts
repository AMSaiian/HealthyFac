import {
  chipGapOf,
  fitChipCount,
  labelChipsOf,
  summarizeChips,
  toChips,
} from '@shared/chips/chip-summary/chip-summary.utils';

const GAP = 4;
const MORE = 30;

describe('labelChipsOf', { tags: ['Platform'] }, () => {
  it.each([
    ['AKnownValue_ShowsTheOptionLabel', 'repair', 'Repair'],
    ['AValueWithoutOption_ShowsTheValue', 'unknown', 'unknown'],
  ])('labelChipsOf_With%s', (_name, value, expected) => {
    const chips = labelChipsOf([value], [{ value: 'repair', label: 'Repair' }]);

    expect(chips.map((chip) => chip.label)).toEqual([expected]);
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
});

describe('chipGapOf', { tags: ['Platform'] }, () => {
  it.each([
    ['TextAppearance_IsZeroBecauseTheSeparatorsAreInTheWidths', 'text', 0],
    ['TagAppearance_IsTheColumnGap', 'tag', GAP],
  ] as const)('chipGapOf_With%s', (_name, appearance, expected) => {
    expect(chipGapOf(appearance, GAP)).toBe(expected);
  });
});

describe('fitChipCount', { tags: ['Platform'] }, () => {
  it.each([
    ['WhenAllChipsFitWithoutTheMoreChip_ShowsAll', 100, [40, 40], 2],
    ['WhenOneChipTooMany_ShowsTheMostThatFitNextToTheMoreChip', 100, [30, 30, 40], 2],
    ['WhenNoChipFitsNextToTheMoreChip_ShowsNone', 50, [40, 40], 0],
  ])('fitChipCount_%s', (_name, availableWidth, chipWidths, expected) => {
    const count = fitChipCount({ availableWidth, chipWidths, gap: GAP, moreWidth: MORE });

    expect(count).toBe(expected);
  });

  it('fitChipCount_WhenTheChipsFillTheWidthExactly_ShowsAllAndOnePixelLessShowsFewer', () => {
    const fits = fitChipCount({ availableWidth: 84, chipWidths: [40, 40], gap: GAP, moreWidth: MORE });
    const tooNarrow = fitChipCount({ availableWidth: 83, chipWidths: [40, 40], gap: GAP, moreWidth: MORE });

    expect(fits).toBe(2);
    expect(tooNarrow).toBe(1);
  });
});

describe('toChips', { tags: ['Platform'] }, () => {
  it('toChips_WithValuesInClickOrder_ReturnsThemInOptionOrder', () => {
    const chips = toChips(['c', 'a'], 'x-', (value) => value.toUpperCase(), ['a', 'b', 'c']);

    expect(chips).toEqual([
      { label: 'A', styleClass: 'x-a' },
      { label: 'C', styleClass: 'x-c' },
    ]);
  });
});

describe('summarizeChips', { tags: ['Platform'] }, () => {
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
