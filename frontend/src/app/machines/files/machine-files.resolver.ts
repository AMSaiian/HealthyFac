import { HttpContext } from '@angular/common/http';
import { inject } from '@angular/core';
import type { RedirectCommand, ResolveFn } from '@angular/router';

import { catchResolverFailure } from '@shared/http/resolver-failure';
import { SKIP_ERROR_TOAST } from '@shared/http/problem-details.interceptor';

import type { Machine } from '@machines/machine';
import { MachinesApi } from '@machines/machines-api';

export const MACHINE_ID_PARAMETER = 'machineId';

/**
 * Loads the machine of the route before the files page activates. A missing machine (`404`) leads to the not-found
 * page, and a network failure (status `0`) to the network page. Both keep the requested address in the address bar
 * (`browserUrl`). Another HTTP failure shows its status toast here, because the load skips the interceptor toast, and
 * completes without a value, so the router cancels the navigation. A failure that is not an HTTP response goes to the
 * global error handler.
 */
export const machineFilesResolver: ResolveFn<Machine | RedirectCommand> = (route) => {
  const api = inject(MachinesApi);

  return api
    .get(route.paramMap.get(MACHINE_ID_PARAMETER) ?? '', { context: new HttpContext().set(SKIP_ERROR_TOAST, true) })
    .pipe(catchResolverFailure());
};
