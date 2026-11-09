import { HttpClient, type HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { FileLinksResponse } from '@shared/files/file-links';

const LINKS_URL = '/api/document/v1/files/links';
const EMBEDDED_URL = '/api/document/v1/files/embedded';
const FILE_FIELD = 'file';

interface RequestOptions {
  readonly context?: HttpContext;
}

/** Reads the signed links of files and uploads the images of rich text. It is shared by every page that shows images. */
@Injectable({ providedIn: 'root' })
export class FilesLinksApi {
  private readonly _http = inject(HttpClient);

  getLinks(ids: readonly string[], options?: RequestOptions): Observable<FileLinksResponse> {
    return this._http.post<FileLinksResponse>(LINKS_URL, { ids }, options);
  }

  uploadEmbeddedImage(file: File, options?: RequestOptions): Observable<{ readonly id: string }> {
    const form = new FormData();
    form.append(FILE_FIELD, file, file.name);
    return this._http.post<{ readonly id: string }>(EMBEDDED_URL, form, options);
  }
}
