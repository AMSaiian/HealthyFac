import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import { Temporal } from '@shared/time/temporal';

import type { DaysRequest } from '@work-orders/assignment-picker/assignment-picker';
import type { AssignmentValue } from '@work-orders/assignment-picker/assignment-picker.utils';

import { AssignDialog } from './assign-dialog';

const TODAY = Temporal.PlainDate.from('2027-06-01');

function setup(initial: AssignmentValue): {
  fixture: ReturnType<typeof TestBed.createComponent<AssignDialog>>;
  days: DaysRequest[];
  validity: boolean[];
} {
  TestBed.configureTestingModule({ providers: [provideTranslateService()] });
  const fixture = TestBed.createComponent(AssignDialog);
  fixture.componentRef.setInput('initialValue', initial);
  fixture.componentRef.setInput('estimateMinutes', 60);
  fixture.componentRef.setInput('employees', []);
  fixture.componentRef.setInput('today', TODAY);
  const days: DaysRequest[] = [];
  const validity: boolean[] = [];
  fixture.componentInstance.daysRequested.subscribe((request) => days.push(request));
  fixture.componentInstance.validityChange.subscribe((isValid) => validity.push(isValid));
  fixture.detectChanges();
  return { fixture, days, validity };
}

describe('AssignDialog', { tags: ['WorkOrders'] }, () => {
  it('opening_WithAnAssignee_SendsOneDaysRequest', () => {
    const { days } = setup({ assigneeId: 'e1', start: Temporal.PlainDateTime.from('2027-06-02T10:00') });

    expect(days).toHaveLength(1);
    expect(days[0].assigneeId).toBe('e1');
  });

  it('opening_WithoutAssignee_SendsNoDaysRequest', () => {
    const { days } = setup({ assigneeId: null, start: null });

    expect(days).toHaveLength(0);
  });

  it.each([
    { name: 'NoAssigneeAndNoStart', assigneeId: null, start: null, expected: false },
    { name: 'AssigneeWithoutStart', assigneeId: 'e1', start: null, expected: false },
    {
      name: 'AssigneeAndStart',
      assigneeId: 'e1',
      start: Temporal.PlainDateTime.from('2027-06-02T10:00'),
      expected: true,
    },
  ])('validity_$name', ({ assigneeId, start, expected }) => {
    const { validity } = setup({ assigneeId, start });

    expect(validity.at(-1)).toBe(expected);
  });
});
