import { custom, minLength, object, string, trim } from 'zod/mini';

import { Temporal } from '@shared/time/temporal';

import { hasFormChanges } from './form-changes';

const schema = object({
  name: string().check(trim(), minLength(1)),
  note: string(),
});

const dateSchema = object({
  name: string(),
  day: custom<Temporal.PlainDate | null>((value) => value instanceof Temporal.PlainDate),
});

describe('hasFormChanges', { tags: ['Platform'] }, () => {
  it('hasFormChanges_WithWhitespaceOnlyChange_ReturnsFalse', () => {
    const result = hasFormChanges(schema, { name: 'Lathe', note: 'a' }, { name: '  Lathe  ', note: 'a' });

    expect(result).toBe(false);
  });

  it.each(
    [
      { name: 'WithChangedField', current: { name: 'Lathe', note: 'b' } },
      { name: 'WithInvalidValue', current: { name: '   ', note: 'a' } },
    ].map((row) => [row.name, row] as [string, typeof row]),
  )('hasFormChanges_%s_ReturnsTrue', (_name, { current }) => {
    expect(hasFormChanges(schema, { name: 'Lathe', note: 'a' }, current)).toBe(true);
  });

  it('hasFormChanges_WithEqualTemporalValues_ReturnsFalse', () => {
    const result = hasFormChanges(
      dateSchema,
      { name: 'a', day: Temporal.PlainDate.from('2026-03-11') },
      { name: 'a', day: Temporal.PlainDate.from('2026-03-11') },
    );

    expect(result).toBe(false);
  });

  it.each([
    ['TheTemporalValue', { name: 'a', day: '2026-03-12' }],
    ['TheNameBesideTemporalValue', { name: 'b', day: '2026-03-11' }],
  ])('hasFormChanges_WithOnly%sChanged_ReturnsTrue', (_name, changed) => {
    const result = hasFormChanges(
      dateSchema,
      { name: 'a', day: Temporal.PlainDate.from('2026-03-11') },
      { name: changed.name, day: Temporal.PlainDate.from(changed.day) },
    );

    expect(result).toBe(true);
  });
});
