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

describe('hasFormChanges', () => {
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

  it('hasFormChanges_WithOnlyTheTemporalValueChanged_ReturnsTrue', () => {
    const result = hasFormChanges(
      dateSchema,
      { name: 'a', day: Temporal.PlainDate.from('2026-03-11') },
      { name: 'a', day: Temporal.PlainDate.from('2026-03-12') },
    );

    expect(result).toBe(true);
  });

  it('hasFormChanges_WithOnlyTheNameChangedBesideTemporalValue_ReturnsTrue', () => {
    const result = hasFormChanges(
      dateSchema,
      { name: 'a', day: Temporal.PlainDate.from('2026-03-11') },
      { name: 'b', day: Temporal.PlainDate.from('2026-03-11') },
    );

    expect(result).toBe(true);
  });
});
