import { HttpStatusCode } from '@angular/common/http';

const SERVER_ERROR_LAST_STATUS = 599;

const STATUS_KEYS: ReadonlyMap<number, string> = new Map([
  [0, 'ERRORS.HTTP.NETWORK'],
  [HttpStatusCode.BadRequest, 'ERRORS.HTTP.BAD_REQUEST'],
  [HttpStatusCode.Unauthorized, 'ERRORS.HTTP.UNAUTHORIZED'],
  [HttpStatusCode.Forbidden, 'ERRORS.HTTP.FORBIDDEN'],
  [HttpStatusCode.NotFound, 'ERRORS.HTTP.NOT_FOUND'],
  [HttpStatusCode.Conflict, 'ERRORS.HTTP.CONFLICT'],
  [HttpStatusCode.UnprocessableEntity, 'ERRORS.HTTP.UNPROCESSABLE'],
  [HttpStatusCode.TooManyRequests, 'ERRORS.HTTP.TOO_MANY_REQUESTS'],
]);

const SERVER_KEY = 'ERRORS.HTTP.SERVER';
const UNKNOWN_KEY = 'ERRORS.HTTP.UNKNOWN';

export function httpErrorMessageKey(status: number): string {
  const key = STATUS_KEYS.get(status);
  if (key !== undefined) {
    return key;
  }
  return status >= HttpStatusCode.InternalServerError && status <= SERVER_ERROR_LAST_STATUS ? SERVER_KEY : UNKNOWN_KEY;
}
