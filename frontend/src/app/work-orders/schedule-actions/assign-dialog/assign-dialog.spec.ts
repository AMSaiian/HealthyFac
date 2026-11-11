import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import { Temporal } from '@shared/time/temporal';

import { AssignDialog, type AssignSelection } from './assign-dialog';

describe('AssignDialog', () => {
  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  async function setup(): Promise<{
    fixture: ReturnType<typeof TestBed.createComponent<AssignDialog>>;
    selections: (AssignSelection | null)[];
  }> {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(AssignDialog);
    fixture.componentRef.setInput('initialValue', {
      assigneeId: 'e1',
      start: Temporal.PlainDateTime.from('2027-06-02T10:00'),
    });
    fixture.componentRef.setInput('employees', []);
    const selections: (AssignSelection | null)[] = [];
    fixture.componentInstance.selectionChange.subscribe((value) => selections.push(value));
    await settle();
    return { fixture, selections };
  }

  it('suggestion_ForTheChosenDayAndAgainAsAnEqualInstance_EmitsTheSelectionOnce', async () => {
    const { fixture, selections } = await setup();

    fixture.componentRef.setInput('suggestion', Temporal.PlainDateTime.from('2027-06-02T13:00'));
    await settle();
    fixture.componentRef.setInput('suggestion', Temporal.PlainDateTime.from('2027-06-02T13:00'));
    await settle();

    expect(selections).toHaveLength(1);
  });

  it('suggestion_ForAnotherDay_EmitsTheNewSelection', async () => {
    const { fixture, selections } = await setup();

    fixture.componentRef.setInput('suggestion', Temporal.PlainDateTime.from('2027-06-03T09:00'));
    await settle();

    expect(selections.map((selection) => selection?.date.toString())).toEqual(['2027-06-02', '2027-06-03']);
  });
});
