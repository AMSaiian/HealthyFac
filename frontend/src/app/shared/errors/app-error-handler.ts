import type { ErrorHandler } from '@angular/core';
import { inject, Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

@Injectable()
export class AppErrorHandler implements ErrorHandler {
  private readonly _messages = inject(MessageService);
  private readonly _translate = inject(TranslateService);

  handleError(error: unknown): void {
    console.error(error);
    this._messages.add({
      severity: 'error',
      summary: this._translate.instant('TOASTS.ERROR'),
      detail: this._translate.instant('ERRORS.UNEXPECTED'),
    });
  }
}
