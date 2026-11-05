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

  it('hasFormChanges_WithChangedField_ReturnsTrue', () => {
    const result = hasFormChanges(schema, { name: 'Lathe', note: 'a' }, { name: 'Lathe', note: 'b' });

    expect(result).toBe(true);
  });

  it('hasFormChanges_WithInvalidValue_ReturnsTrue', () => {
    const result = hasFormChanges(schema, { name: 'Lathe', note: 'a' }, { name: '   ', note: 'a' });

    expect(result).toBe(true);
  });
});
