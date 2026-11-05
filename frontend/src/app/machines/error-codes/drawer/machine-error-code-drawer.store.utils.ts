import type { MachineErrorCode } from '@machines/error-codes/machine-error-code';

import type { MachineErrorCodeFormValue } from './machine-error-code-form-schema';

export const EMPTY_VALUE: MachineErrorCodeFormValue = { code: '', description: null };

export function toFormValue(errorCode: MachineErrorCode): MachineErrorCodeFormValue {
  return { code: errorCode.code, description: errorCode.description };
}
