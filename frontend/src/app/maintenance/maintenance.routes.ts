import type { Routes } from '@angular/router';

import { isOtherParamChanged, tabMatcher } from '@shared/tabs/tab-route';

import { CasesApi } from '@maintenance/cases-api';
import { CasePage } from '@maintenance/detail/case-page/case-page';
import { CASE_TABS } from '@maintenance/detail/case-page/case-page.utils';
import { CasePageStore } from '@maintenance/detail/case-page-store/case-page.store';
import { CASE_ID_PARAMETER, caseResolver } from '@maintenance/detail/case.resolver';
import { CasesGridStore } from '@maintenance/list/cases-grid.store';
import { CasesPage } from '@maintenance/list/cases-page/cases-page';

export const MAINTENANCE_ROUTES: Routes = [
  {
    path: 'cases',
    providers: [CasesApi],
    children: [
      { path: '', title: 'TITLES.MAINTENANCE', component: CasesPage, providers: [CasesGridStore] },
      {
        matcher: tabMatcher(`:${CASE_ID_PARAMETER}`, CASE_TABS),
        runGuardsAndResolvers: isOtherParamChanged,
        title: 'TITLES.CASE',
        component: CasePage,
        providers: [CasePageStore],
        resolve: { detail: caseResolver },
      },
    ],
  },
];
