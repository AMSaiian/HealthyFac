import { HttpStatusCode } from '@angular/common/http';

export class SessionEndedError extends Error {}

export function isSessionEndedByServer(error: unknown): boolean {
  return (
    error instanceof Error &&
    'response' in error &&
    error.response instanceof Response &&
    error.response.status === HttpStatusCode.BadRequest
  );
}
