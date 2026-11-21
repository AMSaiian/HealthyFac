import { inject } from '@angular/core';
import type { RedirectCommand, ResolveFn } from '@angular/router';

import { catchResolverFailure } from '@shared/http/resolver-failure';

import type { Machine } from '@machines/machine';
import { OperatingPhasesPageStore } from '@machines/phases/operating-phases-page-store/operating-phases-page.store';
import { MACHINE_ID_PARAMETER } from '@machines/statuses/list/status-definitions.resolver';

/**
 * Opens the machine of the route in the phases store before the page activates. A missing machine (`404`) leads to the
 * not-found page, and a network failure (status `0`) to the network page. Both keep the requested address in the
 * address bar (`browserUrl`). Another HTTP failure shows its status toast and cancels the navigation.
 */
export const operatingPhasesResolver: ResolveFn<Machine | RedirectCommand> = (route) => {
  const store = inject(OperatingPhasesPageStore);

  return store.openMachine(route.paramMap.get(MACHINE_ID_PARAMETER) ?? '').pipe(catchResolverFailure());
};
