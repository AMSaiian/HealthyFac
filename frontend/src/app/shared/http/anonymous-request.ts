import { HttpContextToken } from '@angular/common/http';

/** Marks a request to a public endpoint: the auth interceptor sends it as it is and never starts a sign-in. */
export const ANONYMOUS_REQUEST = new HttpContextToken<boolean>(() => false);
