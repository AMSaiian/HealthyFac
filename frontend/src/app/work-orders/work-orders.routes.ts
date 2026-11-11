import type { Routes } from '@angular/router';

import { BoardStore } from '@work-orders/board/board.store';
import { BoardPage } from '@work-orders/board/board-page/board-page';
import { WorkOrdersGridStore } from '@work-orders/list/work-orders-grid.store';
import { WorkOrdersPage } from '@work-orders/list/work-orders-page/work-orders-page';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

export const WORK_ORDERS_ROUTES: Routes = [
  {
    path: '',
    providers: [WorkOrdersApi, WorkOrdersGridStore],
    children: [
      { path: '', title: 'TITLES.WORK_ORDERS', component: WorkOrdersPage },
      { path: 'board', title: 'TITLES.WORK_ORDERS', component: BoardPage, providers: [BoardStore] },
    ],
  },
];
