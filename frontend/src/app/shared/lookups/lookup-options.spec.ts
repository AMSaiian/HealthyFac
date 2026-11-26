import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';

import { createLookup } from '@shared/lookups/create-lookup';
import {
  keepSelectedOptions,
  labelsById,
  LOOKUP_DEBOUNCE_MILLISECONDS,
  type LookupOption,
  machineLabelOf,
  mergeLookupOptions,
} from '@shared/lookups/lookup-options';

const lathe: LookupOption = { id: 'm1', displayName: 'Lathe', isDeleted: false };
const press: LookupOption = { id: 'm2', displayName: 'Press', isDeleted: true };
const mill: LookupOption = { id: 'm3', displayName: 'Mill', isDeleted: false };

describe('mergeLookupOptions', { tags: ['Platform'] }, () => {
  it('mergeLookupOptions_WithSelectedOptionInTheFoundPage_ListsItOnceFirst', () => {
    const merged = mergeLookupOptions([press], [lathe, press, mill]);

    expect(merged).toEqual([press, lathe, mill]);
  });
});

describe('keepSelectedOptions', { tags: ['Platform'] }, () => {
  it('keepSelectedOptions_WhenALaterPageLacksTheSelectedOption_KeepsTheRememberedObject', () => {
    const page = signal<LookupOption[]>([lathe, press]);
    const selected = signal<readonly string[]>([]);
    const result = TestBed.runInInjectionContext(() => keepSelectedOptions(page, () => selected()));
    TestBed.tick();
    selected.set(['m2']);
    page.set([mill]);
    TestBed.tick();

    expect(result()).toEqual([press, mill]);
  });

  it('keepSelectedOptions_WhenALaterPageHoldsTheSelectedOption_ListsItOnce', () => {
    const page = signal<LookupOption[]>([lathe, press]);
    const result = TestBed.runInInjectionContext(() => keepSelectedOptions(page, () => ['m2']));
    TestBed.tick();
    page.set([lathe, press, mill]);
    TestBed.tick();

    expect(result().map((option) => option.id)).toEqual(['m2', 'm1', 'm3']);
  });
});

describe('createLookup', { tags: ['Platform'] }, () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('request_WithTypedTextInQuickSuccession_SendsOneFetchWithTheLastText', async () => {
    const fetch = vi.fn<(search: string) => Subject<LookupOption[]>>(() => new Subject());
    const lookup = TestBed.runInInjectionContext(() => createLookup(fetch));

    lookup.request({ text: 'p', isImmediate: false });
    await vi.advanceTimersByTimeAsync(LOOKUP_DEBOUNCE_MILLISECONDS - 1);
    lookup.request({ text: 'pr', isImmediate: false });
    await vi.advanceTimersByTimeAsync(LOOKUP_DEBOUNCE_MILLISECONDS);

    expect(fetch.mock.calls.map((call) => call[0])).toEqual(['pr']);
  });

  it('request_WithAnImmediateRequest_SendsTheFetchWithoutThePause', async () => {
    const fetch = vi.fn<(search: string) => Subject<LookupOption[]>>(() => new Subject());
    const lookup = TestBed.runInInjectionContext(() => createLookup(fetch));

    lookup.request({ text: '', isImmediate: true });
    await vi.advanceTimersByTimeAsync(0);

    expect(fetch.mock.calls.map((call) => call[0])).toEqual(['']);
    expect(lookup.isLoading()).toBe(true);
  });

  it('request_WhenTheFetchAnswers_ListsTheOptionsAndStopsLoading', async () => {
    const answer = new Subject<LookupOption[]>();
    const lookup = TestBed.runInInjectionContext(() => createLookup(() => answer));
    lookup.request({ text: '', isImmediate: true });
    await vi.advanceTimersByTimeAsync(0);

    answer.next([lathe]);
    TestBed.tick();

    expect(lookup.options()).toEqual([lathe]);
    expect(lookup.isLoading()).toBe(false);
  });
});

describe('machineLabelOf', { tags: ['Platform'] }, () => {
  it.each([
    ['KnownDeletedName_KeepsTheNameAndTheDeletedFlag', true],
    ['KnownLiveName_IsNotDeleted', false],
  ])('machineLabelOf_%s', (_name, isDeleted) => {
    expect(machineLabelOf('Lathe', isDeleted, 'Unknown')).toEqual({ name: 'Lathe', isDeleted });
  });

  it.each([null, undefined])('machineLabelOf_Name%s_ShowsTheUnknownNameWithoutTheDeletedFlag', (name) => {
    expect(machineLabelOf(name, true, 'Unknown')).toEqual({ name: 'Unknown', isDeleted: false });
  });
});

describe('labelsById', { tags: ['Platform'] }, () => {
  it('labelsById_Options_MapsEachIdToItsNameAndItsDeletedFlag', () => {
    const labels = labelsById([lathe, press]);

    expect(labels.get('m1')).toEqual({ name: 'Lathe', isDeleted: false });
    expect(labels.get('m2')).toEqual({ name: 'Press', isDeleted: true });
    expect(labels.get('m3')).toBeUndefined();
  });
});
