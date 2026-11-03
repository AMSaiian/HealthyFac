import { HttpStatusCode } from '@angular/common/http';

import { isSessionEndedByServer } from './session-errors';

function networkError(status: number): Error {
  return Object.assign(new Error('refresh failed'), { response: new Response(null, { status }) });
}

describe('isSessionEndedByServer', () => {
  it('isSessionEndedByServer_With400Response_ReturnsTrue', () => {
    const result = isSessionEndedByServer(networkError(HttpStatusCode.BadRequest));

    expect(result).toBe(true);
  });

  it.each([
    ['a 500 response', networkError(HttpStatusCode.InternalServerError)],
    ['a fetch error', new TypeError('Failed to fetch')],
    ['a plain error', new Error('boom')],
    ['a non-error value', 'text'],
  ])('isSessionEndedByServer_WithTransientFailure_ReturnsFalse (%s)', (_name, error) => {
    const result = isSessionEndedByServer(error);

    expect(result).toBe(false);
  });
});
