import type { DepartmentOption } from '@shared/employee/employee-api';

import type { MachineFormValue } from '@machines/drawer/machine-form-schema';

const SINGLE_DEPARTMENT_COUNT = 1;

/**
 * Fills the department of a new machine when the user has exactly one department. A value that the user or the
 * action already set stays unchanged.
 */
export function withDefaultDepartment(
  prefilled: MachineFormValue,
  departments: readonly DepartmentOption[],
): MachineFormValue {
  const isDefaultDepartmentUsed = prefilled.departmentId === '' && departments.length === SINGLE_DEPARTMENT_COUNT;
  return isDefaultDepartmentUsed ? { ...prefilled, departmentId: departments[0].id } : prefilled;
}
