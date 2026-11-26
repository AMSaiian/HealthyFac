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
});
