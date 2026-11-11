import { type Signal, signal } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { catchError, type Observable, of, pipe, switchMap, tap, timer } from 'rxjs';

import {
  keepSelectedOptions,
  LOOKUP_DEBOUNCE_MILLISECONDS,
  type LookupOption,
  type LookupRequest,
} from '@shared/lookups/lookup-options';

/** The options of one picker: the latest lookup page plus the selected options that the page does not hold. */
export interface Lookup {
  readonly options: Signal<LookupOption[]>;
  readonly isLoading: Signal<boolean>;
  request(request: LookupRequest): void;
}

/**
 * Creates a lookup. A newer request replaces an older one, and a typed request waits for the typing pause. Call it in
 * an injection context.
 */
export function createLookup(
  fetch: (search: string) => Observable<LookupOption[]>,
  selectedIds: () => readonly (string | null)[] = () => [],
): Lookup {
  const found = signal<LookupOption[]>([]);
  const isLoading = signal(false);
  const request = rxMethod<LookupRequest>(
    pipe(
      switchMap(({ text, isImmediate }) =>
        timer(isImmediate ? 0 : LOOKUP_DEBOUNCE_MILLISECONDS).pipe(
          tap(() => isLoading.set(true)),
          switchMap(() => fetch(text).pipe(catchError(() => of(found())))),
          tap((options) => {
            found.set(options);
            isLoading.set(false);
          }),
        ),
      ),
    ),
  );
  return { options: keepSelectedOptions(found, selectedIds), isLoading: isLoading.asReadonly(), request };
}
