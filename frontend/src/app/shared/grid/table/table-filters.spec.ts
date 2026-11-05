import { Temporal } from '@shared/time/temporal';

import { asList, asRange, asText, readTableFilters, toTableFilters } from './table-filters';

interface TestFilter {
  readonly name: string;
  readonly tags: readonly string[];
}

type TestColumn = 'name' | 'tags';

const WRITERS: Record<TestColumn, (filter: TestFilter) => unknown> = {
  name: (filter) => filter.name || null,
  tags: (filter) => (filter.tags.length > 0 ? [...filter.tags] : null),
};

const READERS: Record<TestColumn, (value: unknown) => Partial<TestFilter>> = {
  name: (value) => ({ name: asText(value) }),
  tags: (value) => ({ tags: asList<string>(value) }),
};

const COLUMNS: readonly TestColumn[] = ['name', 'tags'];

describe('toTableFilters', () => {
  it('toTableFilters_WithAppliedFilter_WritesOneConstraintPerColumn', () => {
    const filters = toTableFilters(COLUMNS, WRITERS, { name: 'la', tags: [] });

    expect(filters).toEqual({
      name: [{ value: 'la', matchMode: 'equals', operator: 'and' }],
      tags: [{ value: null, matchMode: 'equals', operator: 'and' }],
    });
  });
});

describe('readTableFilters', () => {
  it('readTableFilters_WithPendingValues_ReadsTrimmedPatchOfEveryColumn', () => {
    const patch = readTableFilters(COLUMNS, READERS, { name: [{ value: '  ab  ' }], tags: { value: ['x'] } });

    expect(patch).toEqual({ name: 'ab', tags: ['x'] });
  });

  it('readTableFilters_WithMissingFilters_ReadsEmptyValues', () => {
    const patch = readTableFilters(COLUMNS, READERS, undefined);

    expect(patch).toEqual({ name: '', tags: [] });
  });
});

describe('asRange', () => {
  it('asRange_WithNonRange_ReturnsEmptyRange', () => {
    const range = asRange('x');

    expect(range).toEqual({ from: null, to: null });
  });

  it('asRange_WithRange_ReturnsSameRange', () => {
    const day = Temporal.PlainDate.from('2026-03-29');

    const range = asRange({ from: day, to: null });

    expect(range.from).toBe(day);
  });
});
