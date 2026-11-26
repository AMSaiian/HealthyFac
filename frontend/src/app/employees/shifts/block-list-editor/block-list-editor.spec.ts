import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import { Temporal } from '@shared/time/temporal';

import type { ShiftBlock } from '@employees/shifts/shift';

import { BlockListEditor } from './block-list-editor';

interface EditorMethods {
  rows(): { state: string; minStart?: Temporal.PlainTime; minEnd?: Temporal.PlainTime }[];
  add(): void;
  change(index: number, part: 'start' | 'end', time: Temporal.PlainTime | null): void;
}

describe('BlockListEditor', { tags: ['Shifts'] }, () => {
  const MORNING: ShiftBlock = { start: '08:00', end: '12:00' };

  const TODAY = Temporal.PlainDate.from('2027-04-12');
  const NOW = TODAY.toPlainDateTime('13:02').toZonedDateTime('Europe/Kyiv').toInstant();

  function setup(
    blocks: readonly ShiftBlock[],
    date: Temporal.PlainDate | null = null,
    storedBlocks: readonly ShiftBlock[] = blocks,
  ): { editor: EditorMethods; emitted: (readonly ShiftBlock[])[] } {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(BlockListEditor);
    fixture.componentRef.setInput('blocks', blocks);
    fixture.componentRef.setInput('label', 'Monday');
    fixture.componentRef.setInput('date', date);
    fixture.componentRef.setInput('storedBlocks', storedBlocks);
    fixture.componentRef.setInput('now', NOW);
    fixture.componentRef.setInput('timeZoneId', 'Europe/Kyiv');
    const emitted: (readonly ShiftBlock[])[] = [];
    fixture.componentInstance.blocksChange.subscribe((value) => emitted.push(value));
    return { editor: fixture.componentInstance as unknown as EditorMethods, emitted };
  }

  it('add_WithRoomForAnotherBlock_EmitsTheListOnceWithOneMoreBlock', () => {
    const { editor, emitted } = setup([MORNING]);

    editor.add();

    expect(emitted).toHaveLength(1);
    expect(emitted[0]).toHaveLength(2);
    expect(emitted[0][0]).toEqual(MORNING);
  });

  it('change_WithTheSameTime_EmitsNothing', () => {
    const { editor, emitted } = setup([MORNING]);

    editor.change(0, 'start', Temporal.PlainTime.from('08:00'));

    expect(emitted).toEqual([]);
  });

  it('change_WithAnotherTime_EmitsTheListWithThatPartChanged', () => {
    const { editor, emitted } = setup([MORNING]);

    editor.change(0, 'end', Temporal.PlainTime.from('13:30'));

    expect(emitted).toEqual([[{ start: '08:00', end: '13:30' }]]);
  });

  it('rows_ForToday_MarksEndedRunningAndFutureBlocksWithTheirMinimums', () => {
    const { editor } = setup([MORNING, { start: '13:00', end: '17:00' }, { start: '18:00', end: '19:00' }], TODAY);

    const rows = editor.rows();

    expect(rows.map((row) => row.state)).toEqual(['ended', 'running', 'future']);
    expect(rows[0].minStart).toBeUndefined();
    expect(rows[0].minEnd).toBeUndefined();
    expect(rows[1].minStart).toBeUndefined();
    expect(rows[1].minEnd?.toString()).toBe('13:05:00');
    expect(rows[2].minStart?.toString()).toBe('13:05:00');
  });

  it('rows_ForAnotherDate_HasOnlyFutureBlocksWithoutMinimums', () => {
    const { editor } = setup([MORNING], TODAY.add({ days: 1 }));

    expect(editor.rows()).toEqual([expect.objectContaining({ state: 'future', minStart: undefined })]);
  });

  it('add_ForTodayAfterAnEndedBlock_StartsAtNowOnTheGrid', () => {
    const { editor, emitted } = setup([MORNING], TODAY);

    editor.add();

    expect(emitted[0][1]).toEqual({ start: '13:05', end: '14:05' });
  });

  it('rows_ForANewBlockOnToday_StaysFutureWithTheMinimumAndCanBeRemoved', () => {
    const { editor } = setup([MORNING], TODAY, []);

    const rows = editor.rows();

    expect(rows.map((row) => row.state)).toEqual(['future']);
    expect(rows[0].minStart?.toString()).toBe('13:05:00');
  });

  it('rows_ForARunningStoredBlockWithAnotherEnd_StaysRunning', () => {
    const { editor } = setup([{ start: '13:00', end: '18:00' }], TODAY, [{ start: '13:00', end: '17:00' }]);

    expect(editor.rows().map((row) => row.state)).toEqual(['running']);
  });
});
