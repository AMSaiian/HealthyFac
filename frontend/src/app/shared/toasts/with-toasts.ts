import { inject } from '@angular/core';
import { signalStoreFeature, withMethods } from '@ngrx/signals';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

const SUCCESS_SUMMARY_KEY = 'TOASTS.SUCCESS';
const ERROR_SUMMARY_KEY = 'TOASTS.ERROR';

export function withToasts() {
  return signalStoreFeature(
    withMethods(() => {
      const messages = inject(MessageService);
      const translate = inject(TranslateService);

      return {
        _toastSuccess(detailKey: string, params?: object): void {
          messages.add({
            severity: 'success',
            summary: translate.instant(SUCCESS_SUMMARY_KEY),
            detail: translate.instant(detailKey, params),
          });
        },
        _toastError(detailKey: string, params?: object): void {
          messages.add({
            severity: 'error',
            summary: translate.instant(ERROR_SUMMARY_KEY),
            detail: translate.instant(detailKey, params),
          });
        },
      };
    }),
  );
}
