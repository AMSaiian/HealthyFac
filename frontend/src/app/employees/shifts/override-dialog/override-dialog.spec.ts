import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import { Temporal } from '@shared/time/temporal';

import type { ShiftBlock } from '@employees/shifts/shift';

import { OverrideDialog } from './override-dialog';

interface DialogInternals {
  model(): { blocks: readonly ShiftBlock[] };
  setBlocks(blocks: readonly ShiftBlock[]): void;
}

describe('OverrideDialog', () => {
  const TEMPLATE_DAY: readonly ShiftBlock[] = [{ start: '08:00', end: '17:00' }];
  const OTHER_DAY: readonly ShiftBlock[] = [{ start: '10:00', end: '14:00' }];

  function timesOf(blocks: readonly ShiftBlock[]): ShiftBlock[] {
    return blocks.map((block) => ({ start: block.start, end: block.end }));
  }

  function setup(): { fixture: ComponentFixture<OverrideDialog>; internals: DialogInternals } {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(OverrideDialog);
    fixture.componentRef.setInput('initialValue', { date: Temporal.PlainDate.from('2027-04-13'), blocks: [] });
    fixture.componentRef.setInput('minDate', Temporal.PlainDate.from('2027-04-12'));
    fixture.componentRef.setInput('now', Temporal.Instant.from('2027-04-12T19:02:00Z'));
    fixture.componentRef.setInput('timeZoneId', 'Europe/Kyiv');
    fixture.componentRef.setInput('loadedBlocks', TEMPLATE_DAY);
    fixture.detectChanges();
    return { fixture, internals: fixture.componentInstance as unknown as DialogInternals };
  }

  it('loadedBlocks_BeforeAnEdit_ReplaceTheBlocks', () => {
    const { fixture, internals } = setup();

    fixture.componentRef.setInput('loadedBlocks', OTHER_DAY);
    fixture.detectChanges();

    expect(timesOf(internals.model().blocks)).toEqual(OTHER_DAY);
  });

  it('loadedBlocks_AfterAnEdit_KeepTheEditedBlocks', () => {
    const { fixture, internals } = setup();
    internals.setBlocks([{ start: '09:00', end: '10:00' }]);

    fixture.componentRef.setInput('loadedBlocks', OTHER_DAY);
    fixture.detectChanges();

    expect(timesOf(internals.model().blocks)).toEqual([{ start: '09:00', end: '10:00' }]);
  });
});
