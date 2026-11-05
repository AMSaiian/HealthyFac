import type { Routes } from '@angular/router';

import { MachineErrorCodesGridStore } from './error-codes/list/machine-error-codes-grid.store';
import { machineErrorCodesResolver } from './error-codes/list/machine-error-codes.resolver';
import { MachineErrorCodesPage } from './error-codes/list/machine-error-codes-page/machine-error-codes-page';
import { MachineErrorCodesApi } from './error-codes/machine-error-codes-api';
import { MachinesGridStore } from './list/machines-grid.store';
import { MachinesPage } from './list/machines-page/machines-page';
import { MachinesApi } from './machines-api';

export const MACHINES_ROUTES: Routes = [
  { path: '', component: MachinesPage, providers: [MachinesApi, MachinesGridStore] },
  {
    path: ':machineId/error-codes',
    title: 'TITLES.MACHINE_ERROR_CODES',
    component: MachineErrorCodesPage,
    providers: [MachinesApi, MachineErrorCodesApi, MachineErrorCodesGridStore],
    resolve: { machine: machineErrorCodesResolver },
  },
];
