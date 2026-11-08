import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { CompanyStore } from '@shared/company/company.store';

import { CLOCK } from './clock';

function setup(timeZoneId: string | null): void {
  TestBed.configureTestingModule({
    providers: [{ provide: CompanyStore, useValue: { timeZoneId: signal(timeZoneId) } }],
  });
}

describe('CLOCK', () => {
  it('timeZoneId_AfterCompanyLoaded_ReturnsCompanyZone', () => {
    setup('Europe/Kyiv');

    expect(TestBed.inject(CLOCK).timeZoneId()).toBe('Europe/Kyiv');
  });

  it('timeZoneId_BeforeCompanyLoaded_Throws', () => {
    setup(null);

    expect(() => TestBed.inject(CLOCK).timeZoneId()).toThrow('The company time zone is not loaded.');
  });
});
