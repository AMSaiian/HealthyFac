import { inject, Injectable } from '@angular/core';
import { map, type Observable, of } from 'rxjs';

import { FilesLinksApi } from '@shared/files/files-links-api';

/**
 * The one door of the rich text to the Document service: it resolves the signed links of the images and uploads a new
 * image. The editor and the display call it, so neither repeats the flow. It is a root service, not a store.
 */
@Injectable({ providedIn: 'root' })
export class RichTextImageLinks {
  private readonly _api = inject(FilesLinksApi);

  /** Asks the server once for the links of the given file IDs. A missing ID has no entry in the result. */
  resolve(ids: readonly string[]): Observable<ReadonlyMap<string, string>> {
    if (ids.length === 0) {
      return of(new Map<string, string>());
    }
    return this._api
      .getLinks(ids)
      .pipe(map((response) => new Map(response.links.map((link) => [link.id, link.url] as const))));
  }

  /** Uploads one image for a description and returns the encrypted file ID. */
  upload(file: File): Observable<string> {
    return this._api.uploadEmbeddedImage(file).pipe(map((response) => response.id));
  }
}
