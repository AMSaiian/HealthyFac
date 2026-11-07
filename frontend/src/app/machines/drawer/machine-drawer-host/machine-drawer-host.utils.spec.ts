import type { DepartmentOption } from '@shared/employee/employee-api';

import type { MachineFormValue } from '@machines/drawer/machine-form/machine-form-schema';

import { withDefaultDepartment } from './machine-drawer-host.utils';

const PREFILLED: MachineFormValue = { name: 'Lathe', serialNumber: 'A-1', departmentId: '', description: null };
const FIRST: DepartmentOption = { id: 'department-1', name: 'Assembly' };
const SECOND: DepartmentOption = { id: 'department-2', name: 'Paint' };

describe('withDefaultDepartment', () => {
  it('withDefaultDepartment_WithEmptyIdAndOneDepartment_FillsItIn', () => {
    const result = withDefaultDepartment(PREFILLED, [FIRST]);

    expect(result.departmentId).toBe(FIRST.id);
  });

  it('withDefaultDepartment_WithEmptyIdAndTwoDepartments_StaysEmpty', () => {
    const result = withDefaultDepartment(PREFILLED, [FIRST, SECOND]);

    expect(result.departmentId).toBe('');
  });

  it('withDefaultDepartment_WithSetId_StaysUnchanged', () => {
    const result = withDefaultDepartment({ ...PREFILLED, departmentId: SECOND.id }, [FIRST]);

    expect(result.departmentId).toBe(SECOND.id);
  });
});
