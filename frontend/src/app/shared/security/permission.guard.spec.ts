import { TestBed } from '@angular/core/testing';
import { type ActivatedRouteSnapshot, Router, type RouterStateSnapshot } from '@angular/router';

import { AuthStore } from '@shared/security/auth-store/auth.store';
import { permissionGuard } from './permission.guard';
import { Permission } from './permissions';

describe('permissionGuard', () => {
  function run(hasPermission: boolean): unknown {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthStore,
          useValue: {
            hasPermission: (permission: Permission) => hasPermission && permission === Permission.EmployeesRead,
          },
        },
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
