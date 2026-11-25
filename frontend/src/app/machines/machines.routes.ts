import type { Routes } from '@angular/router';

import { isOtherParamChanged, tabMatcher } from '@shared/tabs/tab-route';

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
import { OperatingPhasesPage } from './phases/operating-phases-page/operating-phases-page';
import { operatingPhasesResolver } from './phases/operating-phases.resolver';
import { OperatingPhasesApi } from './phases/operating-phases-api';
import { OperatingPhasesPageStore } from '@machines/phases/operating-phases-page-store/operating-phases-page.store';
import { MetricChartPage } from './signals/metric-chart-page/metric-chart-page';
import { CHART_TABS, SIGNALS_SEGMENT } from './signals/metric-chart-page/metric-chart-page.utils';
import { metricChartResolver } from './signals/metric-chart.resolver';
import { SignalsApi } from './signals/signals-api';
import { MetricChartPageStore } from '@machines/signals/metric-chart-page-store/metric-chart-page.store';
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
    path: ':machineId/phases',
    title: 'TITLES.OPERATING_PHASES',
    component: OperatingPhasesPage,
    providers: [MachinesApi, OperatingPhasesApi, MetricDefinitionsApi, StatusDefinitionsApi, OperatingPhasesPageStore],
    resolve: { machine: operatingPhasesResolver },
  },
  {
    matcher: tabMatcher(`:machineId/${SIGNALS_SEGMENT}`, CHART_TABS),
    runGuardsAndResolvers: isOtherParamChanged,
    title: 'TITLES.SIGNALS',
    component: MetricChartPage,
    providers: [MachinesApi, MetricDefinitionsApi, SignalsApi, MetricChartPageStore],
    resolve: { machine: metricChartResolver },
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
