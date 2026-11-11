import type { Routes } from '@angular/router';

import { permissionGuard } from '@shared/security/permission.guard';
import { Permission } from '@shared/security/permissions';

import { CompanyDayStore } from '@schedule/company-day/company-day.store';
import { CompanyDayPage } from '@schedule/company-day/company-day-page/company-day-page';
import { EmployeeDayStore } from '@schedule/employee-day/employee-day.store';
import { EmployeeDayPage } from '@schedule/employee-day/employee-day-page/employee-day-page';
import { ScheduleApi } from '@schedule/schedule-api';

export const SCHEDULE_ROUTES: Routes = [
  {
    path: 'employees/:id/schedule',
    title: 'TITLES.EMPLOYEE_SCHEDULE',
    component: EmployeeDayPage,
    canActivate: [permissionGuard(Permission.ShiftsRead)],
    providers: [ScheduleApi, EmployeeDayStore],
  },
  {
    path: 'company/schedule',
    title: 'TITLES.COMPANY_SCHEDULE',
    component: CompanyDayPage,
    canActivate: [permissionGuard(Permission.ShiftsManage)],
    providers: [ScheduleApi, CompanyDayStore],
  },
];
