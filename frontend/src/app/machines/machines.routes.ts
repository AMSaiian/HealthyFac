import type { Routes } from '@angular/router';

import { FilesApi } from '@files/files-api';
import { IngestApi } from '@files/ingest/ingest-api';
import { FilesGridStore } from '@files/list/files-grid.store';
import { MachineFilesPage } from './files/machine-files-page/machine-files-page';
import { machineFilesResolver } from './files/machine-files.resolver';
import { StatusDefinitionsGridStore } from './statuses/list/status-definitions-grid.store';
import { statusDefinitionsResolver } from './statuses/list/status-definitions.resolver';
import { StatusDefinitionsPage } from './statuses/list/status-definitions-page/status-definitions-page';
import { StatusDefinitionsApi } from './statuses/status-definitions-api';
import { DraftsApi } from './drafts/drafts-api';
import { MachinesGridStore } from './list/machines-grid.store';
import { MachinesPage } from './list/machines-page/machines-page';
import { MachinesApi } from './machines-api';
import { MetricDefinitionsApi } from './metrics/metric-definitions-api';
import { MetricUnitsApi } from './metrics/metric-units-api';
import { MetricsPage } from './metrics/metrics-page/metrics-page';
import { metricsPageGuard } from './metrics/metrics-page.guard';
import { MetricsPageStore } from '@machines/metrics/metrics-page-store/metrics-page.store';
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
    path: ':machineId/files',
    title: 'TITLES.MACHINE_FILES',
    component: MachineFilesPage,
    providers: [MachinesApi, FilesApi, IngestApi, FilesGridStore],
    resolve: { machine: machineFilesResolver },
  },
  {
    path: ':machineId/metrics',
    title: 'TITLES.METRICS',
    component: MetricsPage,
    providers: [MachinesApi, MetricDefinitionsApi, MetricUnitsApi, DraftsApi, MetricsPageStore],
    resolve: { machine: metricsResolver },
    canDeactivate: [metricsPageGuard],
  },
];
