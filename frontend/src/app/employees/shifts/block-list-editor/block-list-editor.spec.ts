import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import { Temporal } from '@shared/time/temporal';

import type { ShiftBlock } from '@employees/shifts/shift';

import { BlockListEditor } from './block-list-editor';

interface EditorMethods {
  add(): void;
  change(index: number, part: 'start' | 'end', time: Temporal.PlainTime | null): void;
}

describe('BlockListEditor', () => {
  const MORNING: ShiftBlock = { start: '08:00', end: '12:00' };

  function setup(blocks: readonly ShiftBlock[]): { editor: EditorMethods; emitted: (readonly ShiftBlock[])[] } {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(BlockListEditor);
    fixture.componentRef.setInput('blocks', blocks);
    fixture.componentRef.setInput('label', 'Monday');
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
});
