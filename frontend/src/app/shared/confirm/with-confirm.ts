import { inject } from '@angular/core';
import { signalStoreFeature, withMethods } from '@ngrx/signals';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';

const CANCEL_KEY = 'ACTIONS.CANCEL';
const DEFAULT_ICON = 'pi pi-question-circle';
const DESTRUCTIVE_ICON = 'pi pi-exclamation-triangle';

export interface ConfirmRequest {
  readonly headerKey: string;
  readonly messageKey: string;
  readonly params?: object;
  readonly acceptKey: string;
  readonly icon?: string;
  readonly onReject?: () => void;
  readonly onAccept: () => void;
}

interface ConfirmStyle {
  readonly icon: string;
  readonly acceptButtonProps?: { readonly severity: 'danger' };
}

export function withConfirm() {
  return signalStoreFeature(
    withMethods(() => {
      const confirmation = inject(ConfirmationService);
      const translate = inject(TranslateService);
      const ask = (request: ConfirmRequest, style: ConfirmStyle): void => {
        confirmation.confirm({
          header: translate.instant(request.headerKey),
          message: translate.instant(request.messageKey, request.params),
          acceptLabel: translate.instant(request.acceptKey),
          rejectLabel: translate.instant(CANCEL_KEY),
          accept: request.onAccept,
          rejectButtonProps: { severity: 'secondary' },
          ...style,
          ...(request.icon === undefined ? {} : { icon: request.icon }),
          ...(request.onReject === undefined ? {} : { reject: request.onReject }),
        });
      };

      return {
        _confirm(request: ConfirmRequest): void {
          ask(request, { icon: DEFAULT_ICON });
        },
        _confirmDestructive(request: ConfirmRequest): void {
          ask(request, { icon: DESTRUCTIVE_ICON, acceptButtonProps: { severity: 'danger' } });
        },
      };
    }),
  );
}
