import { isFieldInvalid } from './field-state';

describe('isFieldInvalid', () => {
  it('isFieldInvalid_BeforeTouchAndSubmit_ReturnsFalse', () => {
    const result = isFieldInvalid({ isInvalid: true, isTouched: false });

    expect(result).toBe(false);
  });

  it('isFieldInvalid_AfterTouch_ReturnsTrue', () => {
    const result = isFieldInvalid({ isInvalid: true, isTouched: true });

    expect(result).toBe(true);
  });

  it('isFieldInvalid_WhenValid_ReturnsFalse', () => {
    const afterTouch = isFieldInvalid({ isInvalid: false, isTouched: true });

    expect(afterTouch).toBe(false);
  });
});
