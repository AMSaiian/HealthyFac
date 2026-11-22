import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import { Temporal } from '@shared/time/temporal';

import { blockStateOf } from '@employees/shifts/block-list-editor/block-list-editor.utils';
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

  it('newOverrideForToday_WithTheLoadedPastBlockAndAnAddedBlock_IsValidAndEmitsBoth', () => {
    const today = Temporal.PlainDate.from('2027-04-12');
    const NOW_2202 = today.toPlainDateTime('22:02').toZonedDateTime('Europe/Kyiv').toInstant();
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(OverrideDialog);
    fixture.componentRef.setInput('initialValue', { date: today, blocks: [] });
    fixture.componentRef.setInput('minDate', today);
    fixture.componentRef.setInput('now', NOW_2202);
    fixture.componentRef.setInput('timeZoneId', 'Europe/Kyiv');
    fixture.componentRef.setInput('loadedBlocks', TEMPLATE_DAY);
    let validity: boolean | null = null;
    let emitted: readonly ShiftBlock[] = [];
    fixture.componentInstance.validityChange.subscribe((isValid) => (validity = isValid));
    fixture.componentInstance.valueChange.subscribe((value) => (emitted = value.blocks));
    fixture.detectChanges();

    (fixture.componentInstance as unknown as DialogInternals).setBlocks([
      ...TEMPLATE_DAY,
      { start: '22:05', end: '23:55' },
    ]);
    fixture.detectChanges();

    expect(blockStateOf(TEMPLATE_DAY[0], today, NOW_2202, 'Europe/Kyiv')).toBe('ended');
    expect(validity).toBe(true);
    expect(timesOf(emitted)).toEqual([
      { start: '08:00', end: '17:00' },
      { start: '22:05', end: '23:55' },
    ]);
  });

  it('storedBlocks_OnEditOfAnExistingOverride_AreTheOverrideBlocks', () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(OverrideDialog);
    fixture.componentRef.setInput('initialValue', { date: Temporal.PlainDate.from('2027-04-12'), blocks: OTHER_DAY });
    fixture.componentRef.setInput('isEdit', true);
    fixture.componentRef.setInput('minDate', Temporal.PlainDate.from('2027-04-12'));
    fixture.componentRef.setInput('now', Temporal.Instant.from('2027-04-12T19:02:00Z'));
    fixture.componentRef.setInput('timeZoneId', 'Europe/Kyiv');
    fixture.componentRef.setInput('loadedBlocks', TEMPLATE_DAY);

    const stored = (fixture.componentInstance as unknown as { storedBlocks(): readonly ShiftBlock[] }).storedBlocks();

    expect(timesOf(stored)).toEqual(OTHER_DAY);
  });
});
