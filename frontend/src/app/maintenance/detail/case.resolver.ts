import { inject } from '@angular/core';
import type { RedirectCommand, ResolveFn } from '@angular/router';

import { catchResolverFailure } from '@shared/http/resolver-failure';

import type { CaseDetail } from '@maintenance/case';
import { CasePageStore } from '@maintenance/detail/case-page-store/case-page.store';

export const CASE_ID_PARAMETER = 'caseId';

/**
 * Opens the case of the route in the case store before the page activates. A missing case (`404`) leads to the
 * not-found page and a network failure to the network page, both with the requested address in the address bar.
 * Another HTTP failure shows its status toast and cancels the navigation.
 */
export const caseResolver: ResolveFn<CaseDetail | RedirectCommand> = (route) => {
  const store = inject(CasePageStore);

  return store.openCase(route.paramMap.get(CASE_ID_PARAMETER) ?? '').pipe(catchResolverFailure());
};
