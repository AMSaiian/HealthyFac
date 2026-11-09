import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';

/** Starts the download of a file link. The browser saves the file, because the link answers with `attachment`. */
@Injectable({ providedIn: 'root' })
export class FileDownloader {
  private readonly _document = inject(DOCUMENT);

  start(url: string): void {
    this._document.defaultView?.location.assign(url);
  }
}
