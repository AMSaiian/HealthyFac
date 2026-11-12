import { minLength, object, string, trim } from 'zod/mini';

import { hasFormChanges } from './form-changes';

const schema = object({
  name: string().check(trim(), minLength(1)),
  note: string(),
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
});
