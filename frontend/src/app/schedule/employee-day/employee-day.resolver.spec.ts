import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import {
  type ActivatedRouteSnapshot,
  convertToParamMap,
  RedirectCommand,
  Router,
  type RouterStateSnapshot,
} from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { firstValueFrom, type Observable, of, throwError, toArray } from 'rxjs';

import { NETWORK_ERROR_URL, NOT_FOUND_URL } from '@shared/http/fallback-urls';

import type { DaySchedule } from '@work-orders/work-order';
import { EmployeeDayStore } from '@schedule/employee-day/employee-day.store';

import { employeeDayResolver } from './employee-day.resolver';

const EMPLOYEE_ID = 'encrypted-employee-id';
const DAY = { date: '2027-06-01' } as DaySchedule;

const messages = { add: vi.fn() };

function resolve(openDay: (id: string) => Observable<DaySchedule>): Promise<unknown[]> {
  TestBed.configureTestingModule({
    providers: [
      { provide: EmployeeDayStore, useValue: { openDay } },
      { provide: MessageService, useValue: messages },
      { provide: TranslateService, useValue: { instant: (key: string) => key } },
    ],
  });
  const route = {
    paramMap: convertToParamMap({ id: EMPLOYEE_ID }),
    queryParamMap: convertToParamMap({}),
  } as ActivatedRouteSnapshot;
  const result = TestBed.runInInjectionContext(() => employeeDayResolver(route, {} as RouterStateSnapshot));
  return firstValueFrom((result as Observable<unknown>).pipe(toArray()));
}

describe('employeeDayResolver', () => {
  beforeEach(() => {
    messages.add.mockClear();
  });

  it('employeeDayResolver_WithKnownEmployee_OpensTheDayOfTheRouteEmployee', async () => {
    const openDay = vi.fn(() => of(DAY));

    const [result] = await resolve(openDay);

    expect(openDay).toHaveBeenCalledExactlyOnceWith(EMPLOYEE_ID);
    expect(result).toBe(DAY);
  });

  it('employeeDayResolver_With404_RedirectsToTheNotFoundPage', async () => {
    const [result] = await resolve(() => throwError(() => new HttpErrorResponse({ status: HttpStatusCode.NotFound })));

    expect(result).toBeInstanceOf(RedirectCommand);
    expect(TestBed.inject(Router).serializeUrl((result as RedirectCommand).redirectTo as never)).toBe(NOT_FOUND_URL);
  });

  it('employeeDayResolver_WithStatus0_RedirectsToTheNetworkPage', async () => {
    const [result] = await resolve(() => throwError(() => new HttpErrorResponse({ status: 0 })));

    expect(TestBed.inject(Router).serializeUrl((result as RedirectCommand).redirectTo as never)).toBe(
      NETWORK_ERROR_URL,
    );
    expect(messages.add).not.toHaveBeenCalled();
  });

  it('employeeDayResolver_With500_ShowsTheToastAndDoesNotActivate', async () => {
    const values = await resolve(() =>
      throwError(() => new HttpErrorResponse({ status: HttpStatusCode.InternalServerError })),
    );

    expect(values).toEqual([]);
    expect(messages.add).toHaveBeenCalledTimes(1);
  });
});
