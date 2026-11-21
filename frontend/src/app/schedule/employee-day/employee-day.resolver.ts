import { inject } from '@angular/core';
import type { RedirectCommand, ResolveFn } from '@angular/router';

import { catchResolverFailure } from '@shared/http/resolver-failure';

import type { DaySchedule } from '@work-orders/work-order';
import { EmployeeDayStore } from '@schedule/employee-day/employee-day.store';

const EMPLOYEE_ID_PARAMETER = 'id';

/**
 * Loads the first day of the employee of the route before the page activates. A missing employee (`404`) leads to the
 * not-found page, and a network failure (status `0`) to the network page. Both keep the requested address in the
 * address bar (`browserUrl`). Another HTTP failure shows its status toast and cancels the navigation.
 */
export const employeeDayResolver: ResolveFn<DaySchedule | RedirectCommand> = (route) =>
  inject(EmployeeDayStore)
    .openDay(route.paramMap.get(EMPLOYEE_ID_PARAMETER) ?? '')
    .pipe(catchResolverFailure());
