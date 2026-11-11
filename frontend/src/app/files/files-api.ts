import { HttpClient, type HttpContext, type HttpEvent } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type {
  FilesPage,
  FilesScope,
  SearchFilesBody,
  TieFileBody,
  UntieFileBody,
  UpdateFileBody,
  UploadFileResponse,
} from '@files/file';

const FILES_URL = '/api/document/v1/files';
const MACHINES_URL = '/api/document/v1/machines';
const FILES_SEGMENT = 'files';
const SEARCH_SEGMENT = 'search';
const MACHINE_SEGMENT = 'machine';
const FILE_FIELD = 'file';
const DESCRIPTION_FIELD = 'description';

interface WriteOptions {
  readonly context?: HttpContext;
}

function baseUrl(scope: FilesScope): string {
  return scope.kind === 'company'
    ? FILES_URL
    : `${MACHINES_URL}/${encodeURIComponent(scope.machineId)}/${FILES_SEGMENT}`;
}

function fileUrl(scope: FilesScope, id: string): string {
  return `${baseUrl(scope)}/${encodeURIComponent(id)}`;
}

/** Owns every HTTP call of the company files and the machine files. Provide it on the feature routes. */
@Injectable()
export class FilesApi {
  private readonly _http = inject(HttpClient);

  search(scope: FilesScope, body: SearchFilesBody): Observable<FilesPage> {
    return this._http.post<FilesPage>(`${baseUrl(scope)}/${SEARCH_SEGMENT}`, body);
  }

  /** Uploads one file and reports the progress. The last event holds the response. */
  upload(
    scope: FilesScope,
    file: File,
    description: string | null,
    options?: WriteOptions,
  ): Observable<HttpEvent<UploadFileResponse>> {
    const form = new FormData();
    form.append(FILE_FIELD, file, file.name);
    if (description !== null && description !== '') {
      form.append(DESCRIPTION_FIELD, description);
    }
    return this._http.post<UploadFileResponse>(baseUrl(scope), form, {
      ...options,
      reportProgress: true,
      observe: 'events',
    });
  }

  update(scope: FilesScope, id: string, body: UpdateFileBody, options?: WriteOptions): Observable<void> {
    return this._http.put<void>(fileUrl(scope, id), body, options);
  }

  delete(scope: FilesScope, id: string, options?: WriteOptions): Observable<void> {
    return this._http.delete<void>(fileUrl(scope, id), options);
  }

  /** Ties a company file to a machine. Only the company route has it. */
  tie(fileId: string, body: TieFileBody, options?: WriteOptions): Observable<void> {
    return this._http.put<void>(`${FILES_URL}/${encodeURIComponent(fileId)}/${MACHINE_SEGMENT}`, body, options);
  }

  /** Removes the tie of a company file. The row version goes in the body of the DELETE. */
  untie(fileId: string, body: UntieFileBody, options?: WriteOptions): Observable<void> {
    return this._http.delete<void>(`${FILES_URL}/${encodeURIComponent(fileId)}/${MACHINE_SEGMENT}`, {
      ...options,
      body,
    });
  }
}
