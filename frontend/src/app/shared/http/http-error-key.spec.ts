import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';

import { httpErrorKey, isHttpStatus } from './http-error-key';

describe('httpErrorKey', () => {
  it('httpErrorKey_WithOverrideForTheStatus_ReturnsTheOverride', () => {
    const error = new HttpErrorResponse({ status: HttpStatusCode.Conflict });

    const key = httpErrorKey(error, { [HttpStatusCode.Conflict]: 'EMPLOYEES.TOASTS.ALREADY_MEMBER' });

    expect(key).toBe('EMPLOYEES.TOASTS.ALREADY_MEMBER');
  });

  it('httpErrorKey_WithNonHttpError_ReturnsTheNetworkKey', () => {
    const key = httpErrorKey(new Error('connection lost'));

    expect(key).toBe('ERRORS.HTTP.NETWORK');
  });

  it('isHttpStatus_WithListedAndUnlistedStatusAndANonHttpError_ReturnsTheMatch', () => {
    const error = new HttpErrorResponse({ status: HttpStatusCode.NotFound });

    const results = [
      isHttpStatus(error, HttpStatusCode.Conflict, HttpStatusCode.NotFound),
      isHttpStatus(error, HttpStatusCode.Conflict),
      isHttpStatus(new Error('connection lost'), HttpStatusCode.NotFound),
    ];

    expect(results).toEqual([true, false, false]);
  });
});
