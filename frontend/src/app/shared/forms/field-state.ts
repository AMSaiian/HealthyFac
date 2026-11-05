export interface FieldFlags {
  readonly isInvalid: boolean;
  readonly isTouched: boolean;
}

/**
 * Decides whether a form field shows its invalid state (the red border and the messages). A field shows it only
 * after the user touched it, so a new form shows no red field.
 */
export function isFieldInvalid(field: FieldFlags): boolean {
  return field.isInvalid && field.isTouched;
}
