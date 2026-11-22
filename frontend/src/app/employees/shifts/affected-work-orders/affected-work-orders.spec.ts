import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import { AffectedWorkOrders } from './affected-work-orders';

describe('AffectedWorkOrders', () => {
  it('inputs_ByDefault_KeepTheEditDialogTexts', () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(AffectedWorkOrders);
    fixture.componentRef.setInput('workOrders', []);

    expect(fixture.componentInstance.titleKey()).toBe('SHIFTS.AFFECTED.TITLE');
    expect(fixture.componentInstance.hintKey()).toBe('SHIFTS.AFFECTED.HINT');
  });
});
