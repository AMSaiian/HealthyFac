import { TestBed } from '@angular/core/testing';
import type { RouterStateSnapshot } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { firstValueFrom, isObservable, type Observable } from 'rxjs';

import { ScheduleStore } from '@work-orders/schedule/schedule-store/schedule.store';
import { schedulePageGuard } from './schedule-page.guard';

describe('schedulePageGuard', { tags: ['WorkOrders'] }, () => {
  const pendingCount = vi.fn();
  const confirm = vi.fn();

  function run(): boolean | Observable<boolean> {
    return TestBed.runInInjectionContext(
      () =>
        schedulePageGuard(null as never, null as never, null as never, null as unknown as RouterStateSnapshot) as never,
    );
  }

  beforeEach(() => {
    pendingCount.mockReset();
    confirm.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: ScheduleStore, useValue: { pendingCount } },
        { provide: ConfirmationService, useValue: { confirm } },
      ],
    });
  });

  it('schedulePageGuard_WithoutPendingChanges_LeavesWithoutAsking', () => {
    pendingCount.mockReturnValue(0);

    expect(run()).toBe(true);
    expect(confirm).not.toHaveBeenCalled();
  });

  it('schedulePageGuard_WithPendingChangesAndAnAccept_Leaves', async () => {
    pendingCount.mockReturnValue(2);
    confirm.mockImplementation((request: { accept: () => void }) => request.accept());

    const result = run();

    expect(isObservable(result)).toBe(true);
    expect(await firstValueFrom(result as Observable<boolean>)).toBe(true);
  });

  it('schedulePageGuard_WithPendingChangesAndAReject_Stays', async () => {
    pendingCount.mockReturnValue(2);
    confirm.mockImplementation((request: { reject: () => void }) => request.reject());

    expect(await firstValueFrom(run() as Observable<boolean>)).toBe(false);
  });
});
