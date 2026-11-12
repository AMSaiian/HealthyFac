import type { TagSeverity } from 'primeng/tag';

export interface Chip {
  readonly label: string;
  readonly styleClass: string;
  /** The tag severity of a chip whose color comes from a severity, not from a style class. */
  readonly severity?: TagSeverity;
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

/**
 * Orders the values by their place in the options, so a summary lists them as the list shows them, not in click order.
 * A value without an option keeps its click order after the known values.
 */
export function orderByOptions<T extends string>(values: readonly T[], optionValues: readonly T[]): T[] {
  const placeOf = (value: T): number => {
    const place = optionValues.indexOf(value);
    return place === -1 ? optionValues.length : place;
  };
  return [...values].sort((first, second) => placeOf(first) - placeOf(second));
}

/**
 * Builds the chips of the values, in the order of the options: the style class is the prefix plus the value.
 */
export function toChips<T extends string>(
  values: readonly T[],
  classPrefix: string,
  label: (value: T) => string,
  optionValues: readonly T[],
): Chip[] {
  return orderByOptions(values, optionValues).map((value) => ({
    label: label(value),
    styleClass: `${classPrefix}${value}`,
  }));
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

/** The tag chips are colored boxes. The text chips are plain labels, separated by `, `. */
export type ChipAppearance = 'tag' | 'text';

/**
 * Builds the chips of the values, labelled and colored by their option, in the order of the options. A value without
 * an option shows as its own text.
 */
export function labelChipsOf(
  values: readonly string[],
  options: readonly { readonly value: string; readonly label: string; readonly severity?: TagSeverity }[],
): Chip[] {
  return orderByOptions(
    values,
    options.map((option) => option.value),
  ).map((value) => {
    const option = options.find((candidate) => candidate.value === value);
    return { label: option?.label ?? value, styleClass: '', severity: option?.severity };
  });
}

/** The gap of the fit: a text chip carries its separator in its width, so text chips have no gap between them. */
export function chipGapOf(appearance: ChipAppearance, columnGap: number): number {
  return appearance === 'text' ? 0 : columnGap;
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
