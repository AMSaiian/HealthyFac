import type { DepartmentOption } from '@shared/employee/employee-api';

import type { MachineFormValue } from '@machines/drawer/machine-form/machine-form-schema';

import { withDefaultDepartment } from './machine-drawer-host.utils';

const PREFILLED: MachineFormValue = { name: 'Lathe', serialNumber: 'A-1', departmentId: '', description: null };
const FIRST: DepartmentOption = { id: 'department-1', name: 'Assembly' };
const SECOND: DepartmentOption = { id: 'department-2', name: 'Paint' };

describe('withDefaultDepartment', () => {
  it.each([
    { name: 'EmptyIdAndOneDepartment_FillsItIn', departmentId: '', departments: [FIRST], expected: FIRST.id },
    { name: 'EmptyIdAndTwoDepartments_StaysEmpty', departmentId: '', departments: [FIRST, SECOND], expected: '' },
    { name: 'SetId_StaysUnchanged', departmentId: SECOND.id, departments: [FIRST], expected: SECOND.id },
  ])('withDefaultDepartment_With$name', ({ departmentId, departments, expected }) => {
    const result = withDefaultDepartment({ ...PREFILLED, departmentId }, departments);

    expect(result.departmentId).toBe(expected);
  });
});
