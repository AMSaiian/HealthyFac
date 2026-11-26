import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { RedirectCommand, Router, type UrlTree } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { firstValueFrom, type Observable, throwError, toArray } from 'rxjs';

import { NETWORK_ERROR_URL, NOT_FOUND_URL } from '@shared/http/fallback-urls';
import { catchResolverFailure } from '@shared/http/resolver-failure';

const REQUESTED_URL = '/machines/m1/statuses';

const messages = { add: vi.fn() };

function fail(error: unknown): Promise<unknown[]> {
  TestBed.configureTestingModule({
    providers: [
      { provide: MessageService, useValue: messages },
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  const router = TestBed.inject(Router);
  vi.spyOn(router, 'currentNavigation').mockReturnValue({ finalUrl: router.parseUrl(REQUESTED_URL) } as never);
  const operator = TestBed.runInInjectionContext(() => catchResolverFailure<unknown>());
  return firstValueFrom((throwError(() => error) as Observable<unknown>).pipe(operator, toArray()));
}

function redirectOf(result: unknown): { target: string; browserUrl: UrlTree | undefined } {
  const redirect = result as RedirectCommand;
  return {
    target: TestBed.inject(Router).serializeUrl(redirect.redirectTo as UrlTree),
    browserUrl: redirect.navigationBehaviorOptions?.browserUrl as UrlTree | undefined,
  };
}

describe('catchResolverFailure', { tags: ['Platform'] }, () => {
  beforeEach(() => {
    messages.add.mockClear();
  });

  it('catchResolverFailure_404_RedirectsWithTheRequestedBrowserUrl', async () => {
    const [result] = await fail(new HttpErrorResponse({ status: HttpStatusCode.NotFound }));

    expect(result).toBeInstanceOf(RedirectCommand);
    const { target, browserUrl } = redirectOf(result);
    expect(target).toBe(NOT_FOUND_URL);
    expect(TestBed.inject(Router).serializeUrl(browserUrl as UrlTree)).toBe(REQUESTED_URL);
    expect(messages.add).not.toHaveBeenCalled();
  });

  it('catchResolverFailure_Status0_RedirectsToNetworkErrorWithoutToast', async () => {
    const [result] = await fail(new HttpErrorResponse({ status: 0 }));

    const { target, browserUrl } = redirectOf(result);
    expect(target).toBe(NETWORK_ERROR_URL);
    expect(TestBed.inject(Router).serializeUrl(browserUrl as UrlTree)).toBe(REQUESTED_URL);
    expect(messages.add).not.toHaveBeenCalled();
  });

  it('catchResolverFailure_500_ShowsToastAndCompletesEmpty', async () => {
    const values = await fail(new HttpErrorResponse({ status: HttpStatusCode.InternalServerError }));

    expect(values).toEqual([]);
    expect(messages.add).toHaveBeenCalledTimes(1);
    expect(messages.add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'error', summary: 'TOASTS.ERROR', detail: 'ERRORS.HTTP.SERVER' }),
    );
  });

  it('catchResolverFailure_NonHttpError_Rethrows', async () => {
    await expect(fail(new Error('boom'))).rejects.toThrow('boom');
  });
});
