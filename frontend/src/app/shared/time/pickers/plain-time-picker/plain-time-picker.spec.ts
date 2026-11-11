import { TestBed } from '@angular/core/testing';

import { Temporal } from '@shared/time/temporal';

import { PlainTimePicker } from './plain-time-picker';

describe('PlainTimePicker', () => {
  function setup(): { picker: PlainTimePicker; set: ReturnType<typeof vi.spyOn> } {
    const fixture = TestBed.createComponent(PlainTimePicker);
    fixture.componentRef.setInput('value', Temporal.PlainTime.from('08:00'));
    fixture.detectChanges();
    const picker = fixture.componentInstance;
    return { picker, set: vi.spyOn(picker.value, 'set') };
  }

  it('onPickerChange_WithTheSameTime_DoesNotSetTheModel', () => {
    const { picker, set } = setup();

    (picker as unknown as { onPickerChange: (date: Date) => void }).onPickerChange(new Date(2000, 0, 1, 8, 0));

    expect(set).not.toHaveBeenCalled();
  });

  it('onPickerChange_WithAnotherTime_SetsTheModelOnce', () => {
    const { picker, set } = setup();

    (picker as unknown as { onPickerChange: (date: Date) => void }).onPickerChange(new Date(2000, 0, 1, 9, 5));

    expect(set).toHaveBeenCalledTimes(1);
    expect(set.mock.calls[0][0].toString()).toBe('09:05:00');
  });
});
