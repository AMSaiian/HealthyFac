import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { WorkOrderCategory, WorkOrderPriority } from '@work-orders/work-order';

import { WorkOrdersApi } from './work-orders-api';

describe('WorkOrdersApi', () => {
  let api: WorkOrdersApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), WorkOrdersApi],
    });
    api = TestBed.inject(WorkOrdersApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('scheduleDays_WithAnExcludedWorkOrder_SendsTheRangeAndTheExclusion', () => {
    api.scheduleDays('e1', '2027-06-01', '2027-07-12', 'wo-1').subscribe();

    const request = http.expectOne((r) => r.url === '/api/core/v1/employees/e1/schedule/days');

    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('from')).toBe('2027-06-01');
    expect(request.request.params.get('to')).toBe('2027-07-12');
    expect(request.request.params.get('excludeWorkOrderId')).toBe('wo-1');
    request.flush({ timeZoneId: 'Europe/Kyiv', days: [] });
  });

  it('scheduleDays_WithoutAnExcludedWorkOrder_OmitsTheParameter', () => {
    api.scheduleDays('e1', '2027-06-01', '2027-07-12').subscribe();

    const request = http.expectOne((r) => r.url === '/api/core/v1/employees/e1/schedule/days');

    expect(request.request.params.has('excludeWorkOrderId')).toBe(false);
    request.flush({ timeZoneId: 'Europe/Kyiv', days: [] });
  });

  it('freeSpans_WithADate_SendsTheDateAndTheExclusion', () => {
    api.freeSpans('e1', '2027-06-01', 'wo-1').subscribe();

    const request = http.expectOne((r) => r.url === '/api/core/v1/employees/e1/schedule/free-spans');

    expect(request.request.params.get('date')).toBe('2027-06-01');
    expect(request.request.params.get('excludeWorkOrderId')).toBe('wo-1');
    request.flush({ date: '2027-06-01', timeZoneId: 'Europe/Kyiv', spans: [] });
  });

  it('history_WithAnAfter_SendsTheLimitAndTheAfter', () => {
    api.history('wo-1', 10, 14).subscribe();

    const request = http.expectOne((r) => r.url === '/api/core/v1/work-orders/wo-1/history');

    expect(request.request.params.get('limit')).toBe('10');
    expect(request.request.params.get('after')).toBe('14');
    request.flush({ items: [], totalCount: 0, nextAfter: null });
  });

  it('history_WithoutAnAfter_OmitsIt', () => {
    api.history('wo-1', 10).subscribe();

    const request = http.expectOne((r) => r.url === '/api/core/v1/work-orders/wo-1/history');

    expect(request.request.params.has('after')).toBe(false);
    request.flush({ items: [], totalCount: 0, nextAfter: null });
  });

  it('update_WithAScheduleChange_SendsTheAssigneeAndTheStart', () => {
    api
      .update('wo-1', {
        title: 'T',
        description: null,
        priority: WorkOrderPriority.High,
        category: WorkOrderCategory.Repair,
        machineId: null,
        estimateMinutes: 60,
        fileIds: [],
        rowVersion: 3,
        assigneeId: 'e1',
        plannedStartLocal: '2027-06-01T09:00:00',
      })
      .subscribe();

    const request = http.expectOne('/api/core/v1/work-orders/wo-1');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toMatchObject({ assigneeId: 'e1', plannedStartLocal: '2027-06-01T09:00:00' });
    request.flush(null);
  });
});
