import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tab, TabList, TabPanel, TabPanels, Tabs } from 'primeng/tabs';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

import { SignalSeverity } from '@maintenance/case';
import { AiLogPanel } from '@maintenance/detail/ai-log-panel/ai-log-panel';
import { CaseOutcomeCard } from '@maintenance/detail/case-outcome/case-outcome';
import { CasePageStore } from '@maintenance/detail/case-page-store/case-page.store';
import { CaseSignals } from '@maintenance/detail/case-signals/case-signals';
import { CloseCaseDialog } from '@maintenance/detail/close-case-dialog/close-case-dialog';
import {
  AI_UNAVAILABLE_KEY,
  SEVERITY_LABEL_KEYS,
  STATE_LABEL_KEYS,
} from '@maintenance/list/cases-grid/cases-grid.utils';

const SIGNALS_TAB = 'signals';
const AI_LOG_TAB = 'aiLog';

@Component({
  imports: [
    AiLogPanel,
    ButtonDirective,
    CaseOutcomeCard,
    CaseSignals,
    CloseCaseDialog,
    LocalDateTimePipe,
    RouterLink,
    Tab,
    TabList,
    TabPanel,
    TabPanels,
    Tabs,
    Tag,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 overflow-y-auto p-4' },
  selector: 'hf-case-page',
  templateUrl: './case-page.html',
})
export class CasePage {
  protected readonly store = inject(CasePageStore);
  protected readonly aiUnavailableKey = AI_UNAVAILABLE_KEY;
  protected readonly SIGNALS_TAB = SIGNALS_TAB;
  protected readonly AI_LOG_TAB = AI_LOG_TAB;
  protected readonly tab = signal<string | number>(SIGNALS_TAB);

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([...Object.values(STATE_LABEL_KEYS), ...Object.values(SEVERITY_LABEL_KEYS)]),
    { initialValue: {} as Record<string, string> },
  );

  protected stateLabel(state: keyof typeof STATE_LABEL_KEYS): string {
    const key = STATE_LABEL_KEYS[state];
    return this._labels()[key] ?? key;
  }

  protected severityLabel(severity: SignalSeverity): string {
    const key = SEVERITY_LABEL_KEYS[severity];
    return this._labels()[key] ?? key;
  }

  protected severityTag(severity: SignalSeverity): 'danger' | 'warn' {
    return severity === SignalSeverity.Alarm ? 'danger' : 'warn';
  }
}
