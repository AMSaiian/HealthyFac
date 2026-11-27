import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { LookupsApi } from '@shared/lookups/lookups-api';

describe('LookupsApi', { tags: ['Platform'] }, () => {
  it('machinesByIds_WithIds_PostsTheIdsAndReturnsTheOptions', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);
    const result: unknown[] = [];

    TestBed.inject(LookupsApi)
      .machinesByIds(['a', 'b'])
      .subscribe((options) => result.push(...options));
    const request = http.expectOne('/api/core/v1/machines/lookup');
    request.flush({ items: [{ id: 'a', displayName: 'Lathe', isDeleted: true }] });

    expect(request.request.body).toEqual({ ids: ['a', 'b'] });
    expect(result).toEqual([{ id: 'a', displayName: 'Lathe', isDeleted: true }]);
  });

  it('files_WithText_PostsTheSearchAndMapsTheFileNames', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);
    const result: unknown[] = [];

    TestBed.inject(LookupsApi)
      .files('man')
      .subscribe((options) => result.push(...options));
    const request = http.expectOne('/api/document/v1/files/lookup');
    request.flush({ files: [{ id: 'f1', fileName: 'manual.pdf' }] });

    expect(request.request.body).toEqual({ search: 'man' });
    expect(result).toEqual([{ id: 'f1', displayName: 'manual.pdf', isDeleted: false }]);
  });

  it('casesOfMachines_WithMachineIds_PostsTheMachineFilterNewestFirst', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);

    TestBed.inject(LookupsApi).casesOfMachines(['m1']).subscribe();
    const request = http.expectOne('/api/insights/v1/cases/search');
    request.flush({ items: [] });

    expect(request.request.body).toEqual({
      offset: 0,
      limit: 50,
      sort: { column: 'openedAt', direction: 'desc' },
      filter: { machineIds: { ids: ['m1'] } },
    });
  });
});
