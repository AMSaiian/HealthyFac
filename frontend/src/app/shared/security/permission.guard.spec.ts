import { TestBed } from '@angular/core/testing';
import { type ActivatedRouteSnapshot, Router, type RouterStateSnapshot } from '@angular/router';

import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';
import { permissionGuard } from './permission.guard';
import { Permission } from './permissions';

describe('permissionGuard', { tags: ['Identity'] }, () => {
  function run(hasPermission: boolean): unknown {
    TestBed.configureTestingModule({
      providers: [
        provideFakeAuthStore({
          hasPermission: (permission: Permission) => hasPermission && permission === Permission.EmployeesRead,
        }),
        { provide: Router, useValue: { parseUrl: (url: string) => ({ url }) } },
      ],
    });
    return TestBed.runInInjectionContext(() =>
      permissionGuard(Permission.EmployeesRead)({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
  }

  it('permissionGuard_WithPermission_Activates', () => {
    const result = run(true);

    expect(result).toBe(true);
  });

  it('permissionGuard_WithoutPermission_RedirectsToHome', () => {
    const result = run(false);

    expect(result).toEqual({ url: '/' });
  });
});
