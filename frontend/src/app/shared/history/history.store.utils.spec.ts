import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { formatCompose, parseCompose, withCompose, withoutCompose } from './history.store.utils';

describe('history.store.utils', () => {
  it.each([
    ['machine', { name: 'machine', id: null }],
    ['machine:m1', { name: 'machine', id: 'm1' }],
    ['machine:', { name: 'machine', id: null }],
    ['file:a:b', { name: 'file', id: 'a:b' }],
  ])('parseCompose_With%s', (value, expected) => {
    const result = parseCompose(value);

    expect(result).toEqual(expected);
  });

  it('formatCompose_WithAndWithoutId_JoinsWithAColon', () => {
    expect(formatCompose('machine')).toBe('machine');
    expect(formatCompose('machine', 'm1')).toBe('machine:m1');
  });

  it('withoutCompose_WithOtherParams_KeepsThem', () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const router = TestBed.inject(Router);

    const result = withoutCompose(router.parseUrl('/machines?search=a&compose=machine:m1#top'));

    expect(router.serializeUrl(result)).toBe('/machines?search=a#top');
  });

  it('withCompose_WithOtherParams_KeepsThem', () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    const router = TestBed.inject(Router);

    const result = withCompose(router.parseUrl('/machines?search=a'), 'machine:m1');

    expect(router.serializeUrl(result)).toBe('/machines?search=a&compose=machine:m1');
  });
});
