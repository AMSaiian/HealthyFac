import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import type { OperatingPhase } from '@machines/phases/operating-phase';
import { PhaseTable } from '@machines/phases/phase-table/phase-table';

const PHASE_ID = 'phase-1';
const EYE_ICON = 'i.pi-eye';
const TRASH_ICON = 'i.pi-trash';
const PENCIL_ICON = 'i.pi-pencil';

const PHASE: OperatingPhase = {
  id: PHASE_ID,
  code: 'run',
  name: 'Run',
  description: null,
  priority: 1,
  isTracked: true,
  conditions: [],
  limits: [],
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: null,
} as unknown as OperatingPhase;

function setup(canWrite: boolean): { element: HTMLElement; component: PhaseTable } {
  TestBed.configureTestingModule({ providers: [provideTranslateService()] });
  const fixture = TestBed.createComponent(PhaseTable);
  fixture.componentRef.setInput('rows', [PHASE]);
  fixture.componentRef.setInput('canWrite', canWrite);
  fixture.detectChanges();
  return { element: fixture.nativeElement as HTMLElement, component: fixture.componentInstance };
}

describe('PhaseTable', () => {
  it('rowButtons_WithoutWriteRight_ShowsTheEyeButtonAndNoDeleteButton', () => {
    const { element } = setup(false);

    const eyeButtons = element.querySelectorAll(`button:has(${EYE_ICON})`);
    expect(eyeButtons).toHaveLength(1);
    expect(eyeButtons[0].getAttribute('aria-label')).toBe('ACTIONS.TOOLTIPS.SHOW_DETAILS');
    expect(element.querySelectorAll(`button:has(${TRASH_ICON})`)).toHaveLength(0);
    expect(element.querySelectorAll(`button:has(${PENCIL_ICON})`)).toHaveLength(0);
  });

  it('rowButtons_WithWriteRight_ShowsTheEyeButtonAndTheDeleteButton', () => {
    const { element } = setup(true);

    expect(element.querySelectorAll(`button:has(${EYE_ICON})`)).toHaveLength(1);
    expect(element.querySelectorAll(`button:has(${TRASH_ICON})`)).toHaveLength(1);
  });

  it('eyeButton_Click_EmitsUpdateRequestedWithThePhaseId', () => {
    const { element, component } = setup(false);
    const requested: string[] = [];
    component.updateRequested.subscribe((id) => requested.push(id));

    (element.querySelector(`button:has(${EYE_ICON})`) as HTMLButtonElement).click();

    expect(requested).toEqual([PHASE_ID]);
  });
});
