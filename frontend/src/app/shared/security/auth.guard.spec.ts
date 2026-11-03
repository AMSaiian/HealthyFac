import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { authGuard } from './auth.guard';
import { AuthStore } from './auth.store';

const REQUESTED_URL = '/some/path?x=1';

describe('authGuard', () => {
  const isAuthenticated = signal(false);
  const login = vi.fn().mockResolvedValue(undefined);

  function run(): unknown {
    TestBed.configureTestingModule({ providers: [{ provide: AuthStore, useValue: { isAuthenticated, login } }] });
    return TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, { url: REQUESTED_URL } as RouterStateSnapshot),
    );
  }

  beforeEach(() => {
    login.mockClear();
  });

  it('authGuard_WithSession_Allows', () => {
    isAuthenticated.set(true);

    const result = run();

    expect(result).toBe(true);
    expect(login).not.toHaveBeenCalled();
  });

  it('authGuard_WithoutSession_CallsLoginWithRequestedUrlAndBlocks', () => {
    isAuthenticated.set(false);

    const result = run();

    expect(result).toBe(false);
    expect(login).toHaveBeenCalledWith(REQUESTED_URL);
  });
});
