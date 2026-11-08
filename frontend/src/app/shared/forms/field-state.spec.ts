import { isFieldInvalid } from './field-state';

describe('isFieldInvalid', () => {
  it.each([
    ['before touch', { isInvalid: true, isTouched: false }, false],
    ['after touch', { isInvalid: true, isTouched: true }, true],
    ['when valid', { isInvalid: false, isTouched: true }, false],
  ])('isFieldInvalid_%s_ReturnsExpected', (_name, state, expected) => {
    const result = isFieldInvalid(state);

    expect(result).toBe(expected);
  });
});
