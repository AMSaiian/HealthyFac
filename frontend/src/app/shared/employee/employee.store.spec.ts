import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AuthStore } from '@shared/security/auth.store';

import { EmployeeApi } from './employee-api';
import { EmployeeStore } from './employee.store';

const ASSEMBLY = { id: 'department-1', name: 'Assembly' };

describe('EmployeeStore', () => {
  function createStore(isAuthenticated: boolean): {
    store: InstanceType<typeof EmployeeStore>;
    departments: ReturnType<typeof vi.fn>;
  } {
    const departments = vi.fn(() => of({ items: [ASSEMBLY] }));
    TestBed.configureTestingModule({
      providers: [
        { provide: EmployeeApi, useValue: { departments } },
        { provide: AuthStore, useValue: { isAuthenticated: signal(isAuthenticated) } },
      ],
    });
    return { store: TestBed.inject(EmployeeStore), departments };
  }

  it('departments_WhenSignedOut_DoesNotRequest', () => {
    const { departments } = createStore(false);

    TestBed.tick();

    expect(departments).not.toHaveBeenCalled();
  });

  it('departmentName_WithKnownId_ReturnsName', async () => {
    const { store, departments } = createStore(true);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    const name = store.departmentName(ASSEMBLY.id);

    expect(departments).toHaveBeenCalledTimes(1);
    expect(name).toBe(ASSEMBLY.name);
    expect(store.departmentOptions()).toEqual([ASSEMBLY]);
  });

  it('departmentName_WithUnknownId_ReturnsUndefined', async () => {
    const { store } = createStore(true);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    const name = store.departmentName('missing-department');

    expect(name).toBeUndefined();
  });
});
