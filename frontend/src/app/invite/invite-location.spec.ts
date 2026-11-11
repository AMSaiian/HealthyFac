import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';

import { InviteLocation } from '@invite/invite-location';

describe('InviteLocation', () => {
  const hash = { value: '' };

  function setup(): InviteLocation {
    TestBed.configureTestingModule({
      providers: [
        InviteLocation,
        {
          provide: DOCUMENT,
          useValue: {
            location: {
              get hash() {
                return hash.value;
              },
            },
          },
        },
      ],
    });
    return TestBed.inject(InviteLocation);
  }

  beforeEach(() => {
    hash.value = '';
  });

  it('readToken_WithEncodedTokenInTheFragment_GivesTheDecodedToken', () => {
    hash.value = '#token=a%2Bb%3D';

    const token = setup().readToken();

    expect(token).toBe('a+b=');
  });

  it('readToken_WithoutFragment_GivesNull', () => {
    const token = setup().readToken();

    expect(token).toBeNull();
  });
});
