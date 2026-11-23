import type { Routes } from '@angular/router';

import { permissionGuard } from '@shared/security/permission.guard';
import { Permission } from '@shared/security/permissions';

import { WorkOrderDraftsApi } from '@work-orders/drafts/work-order-drafts-api';
import { WorkOrderDraftsGridStore } from '@work-orders/drafts/work-order-drafts-grid.store';
import { WorkOrderDraftsPage } from '@work-orders/drafts/work-order-drafts-page/work-order-drafts-page';
import { BoardStore } from '@work-orders/board/board-store/board.store';
import { BoardPage } from '@work-orders/board/board-page/board-page';
import { WorkOrdersGridStore } from '@work-orders/list/work-orders-grid.store';
import { WorkOrdersPage } from '@work-orders/list/work-orders-page/work-orders-page';
import { schedulePageGuard } from '@work-orders/schedule/schedule-page.guard';
import { SchedulePage } from '@work-orders/schedule/schedule-page/schedule-page';
import { ScheduleStore } from '@work-orders/schedule/schedule-store/schedule.store';
import { WorkOrdersApi } from '@work-orders/work-orders-api';
import { ScheduleApi } from '@schedule/schedule-api';

export const WORK_ORDERS_ROUTES: Routes = [
  {
    path: '',
    providers: [WorkOrdersApi, WorkOrdersGridStore],
    children: [
      { path: '', title: 'TITLES.WORK_ORDERS', component: WorkOrdersPage },
      {
        path: 'drafts',
        title: 'TITLES.WORK_ORDER_DRAFTS',
        component: WorkOrderDraftsPage,
        canActivate: [permissionGuard(Permission.DraftsRead)],
        providers: [WorkOrderDraftsApi, WorkOrderDraftsGridStore],
      },
      { path: 'board', title: 'TITLES.WORK_ORDERS', component: BoardPage, providers: [BoardStore] },
      {
        path: 'schedule',
        title: 'TITLES.WORK_ORDERS',
        component: SchedulePage,
        canActivate: [permissionGuard(Permission.ShiftsRead)],
        canDeactivate: [schedulePageGuard],
        providers: [ScheduleApi, ScheduleStore],
      },
    ],
  },
];
