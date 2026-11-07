import type { Routes } from '@angular/router';

import { StatusDefinitionsGridStore } from './statuses/list/status-definitions-grid.store';
import { statusDefinitionsResolver } from './statuses/list/status-definitions.resolver';
import { StatusDefinitionsPage } from './statuses/list/status-definitions-page/status-definitions-page';
import { StatusDefinitionsApi } from './statuses/status-definitions-api';
import { MachinesGridStore } from './list/machines-grid.store';
import { MachinesPage } from './list/machines-page/machines-page';
import { MachinesApi } from './machines-api';
import { MetricDefinitionsApi } from './metrics/metric-definitions-api';
import { MetricUnitsApi } from './metrics/metric-units-api';
import { MetricsPage } from './metrics/metrics-page/metrics-page';
import { metricsPageGuard } from './metrics/metrics-page.guard';
import { MetricsPageStore } from './metrics/metrics-page.store';
import { metricsResolver } from './metrics/metrics.resolver';

export const MACHINES_ROUTES: Routes = [
  { path: '', component: MachinesPage, providers: [MachinesApi, MachinesGridStore] },
  {
    path: ':machineId/statuses',
    title: 'TITLES.STATUS_DEFINITIONS',
    component: StatusDefinitionsPage,
    providers: [MachinesApi, StatusDefinitionsApi, StatusDefinitionsGridStore],
    resolve: { machine: statusDefinitionsResolver },
  },
  {
    path: ':machineId/metrics',
    title: 'TITLES.METRICS',
    component: MetricsPage,
    providers: [MachinesApi, MetricDefinitionsApi, MetricUnitsApi, MetricsPageStore],
    resolve: { machine: metricsResolver },
    canDeactivate: [metricsPageGuard],
  },
];
