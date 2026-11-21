import { inject } from '@angular/core';
import type { RedirectCommand, ResolveFn } from '@angular/router';

import { catchResolverFailure } from '@shared/http/resolver-failure';

import type { Machine } from '@machines/machine';
import { StatusDefinitionsGridStore } from '@machines/statuses/list/status-definitions-grid.store';

export const MACHINE_ID_PARAMETER = 'machineId';

/**
 * Opens the machine of the route in the grid store before the page activates. A missing machine (`404`) leads to the
 * not-found page, and a network failure (status `0`) to the network page. Both keep the requested address in the
 * address bar (`browserUrl`). Another HTTP failure shows its status toast here, because the load skips the
 * interceptor toast, and completes without a value, so the router cancels the navigation and the user stays on the
 * previous page. A failure that is not an HTTP response goes to the global error handler.
 */
export const statusDefinitionsResolver: ResolveFn<Machine | RedirectCommand> = (route) => {
  const store = inject(StatusDefinitionsGridStore);

  return store.openMachine(route.paramMap.get(MACHINE_ID_PARAMETER) ?? '').pipe(catchResolverFailure());
};
