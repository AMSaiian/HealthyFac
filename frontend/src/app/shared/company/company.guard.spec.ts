import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import type { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { firstValueFrom, type Observable } from 'rxjs';

import { AuthStore } from '@shared/security/auth-store/auth.store';

import { companyGuard } from './company.guard';

const COMPANY_URL = '/api/core/v1/company';
const COMPANY = { name: 'Test Company', timeZoneId: 'Europe/Kyiv' };
const SERVER_ERROR = { status: 500, statusText: 'Server Error' };

function runGuard(): { result: Promise<boolean>; http: HttpTestingController } {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: AuthStore, useValue: { isAuthenticated: () => true } },
    ],
  });
  const result$ = TestBed.runInInjectionContext(() =>
    companyGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
  ) as Observable<boolean>;
  return { result: firstValueFrom(result$), http: TestBed.inject(HttpTestingController) };
}

async function respond(
  http: HttpTestingController,
  answer: (request: ReturnType<HttpTestingController['expectOne']>) => void,
): Promise<void> {
  TestBed.tick();
  await vi.waitFor(() => {
    answer(http.expectOne(COMPANY_URL));
  });
}

describe('companyGuard', () => {
  it('companyGuard_AfterSuccessfulResponse_ResolvesTrue', async () => {
    const { result, http } = runGuard();

    await respond(http, (request) => request.flush(COMPANY));

    await expect(result).resolves.toBe(true);
  });

  it('companyGuard_AfterErrorResponse_ResolvesFalse', async () => {
    const { result, http } = runGuard();

    await respond(http, (request) => request.flush(null, SERVER_ERROR));

    await expect(result).resolves.toBe(false);
  });

  it('companyGuard_AfterFailedLoad_RetriesOnceAndResolvesTrue', async () => {
    const { result, http } = runGuard();
    await respond(http, (request) => request.flush(null, SERVER_ERROR));
    await expect(result).resolves.toBe(false);

    const retried = firstValueFrom(
      TestBed.runInInjectionContext(() =>
        companyGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
      ) as Observable<boolean>,
    );
    await respond(http, (request) => request.flush(COMPANY));

    await expect(retried).resolves.toBe(true);
    http.verify();
  });
});
