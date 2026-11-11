export interface Chip {
  readonly label: string;
  readonly styleClass: string;
}

export interface ChipSummary {
  readonly shown: readonly Chip[];
  readonly hiddenCount: number;
  readonly tooltip: string;
}

export interface ChipMeasures {
  readonly availableWidth: number;
  readonly chipWidths: readonly number[];
  readonly gap: number;
  readonly moreWidth: number;
}

/** Builds the chips of the values: the style class is the prefix plus the value. */
export function toChips<T extends string>(
  values: readonly T[],
  classPrefix: string,
  label: (value: T) => string,
): Chip[] {
  return values.map((value) => ({ label: label(value), styleClass: `${classPrefix}${value}` }));
}

/**
 * Counts the chips that fit in one row. All chips show when they fit without the `+N` chip. Otherwise the count is the
 * most chips that fit next to the `+N` chip, and 0 when not even one does.
 */
export function fitChipCount({ availableWidth, chipWidths, gap, moreWidth }: ChipMeasures): number {
  const widthOf = (count: number): number =>
    chipWidths.slice(0, count).reduce((sum, width) => sum + width, 0) + Math.max(0, count - 1) * gap;
  if (widthOf(chipWidths.length) <= availableWidth) {
    return chipWidths.length;
  }
  let count = chipWidths.length - 1;
  while (count > 0 && widthOf(count) + gap + moreWidth > availableWidth) {
    count--;
  }
  return count;
}

/** Splits the chips into the shown ones and the count of the rest. The tooltip lists every label when some are hidden. */
export function summarizeChips(chips: readonly Chip[], shownCount: number): ChipSummary {
  const hiddenCount = Math.max(0, chips.length - shownCount);
  return {
    shown: chips.slice(0, shownCount),
    hiddenCount,
    tooltip: hiddenCount === 0 ? '' : chips.map((chip) => chip.label).join(', '),
  };
}
