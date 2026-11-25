import { Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';
import { Tooltip } from 'primeng/tooltip';

import type { CaseOutcome } from '@maintenance/case';
import { FINAL_TEXT_CLASSES } from '@maintenance/detail/ai-log-panel/ai-log-panel.utils';
import { OutcomeLink, outcomeView } from '@maintenance/detail/case-outcome/case-outcome.utils';

import { WORK_ORDER_DRAFTS_PATH, WORK_ORDERS_PATH } from '@work-orders/work-order-links';

/** The result of a case in one sentence with its link, and the final text of the agent as "Why". It reads no store. */
@Component({
  imports: [ButtonDirective, Card, RouterLink, Tooltip, TranslatePipe],
  host: { class: 'block flex-none' },
  selector: 'hf-case-outcome',
  templateUrl: './case-outcome.html',
})
export class CaseOutcomeCard {
  readonly outcome = input.required<CaseOutcome>();
  /** True when the case is open and the user may run it again. */
  readonly canRunAgain = input(false);
  readonly runAgain = output();

  protected readonly finalTextClasses = FINAL_TEXT_CLASSES;
  protected readonly OutcomeLink = OutcomeLink;
  protected readonly draftsPath = WORK_ORDER_DRAFTS_PATH;
  protected readonly workOrdersPath = WORK_ORDERS_PATH;
  protected readonly view = computed(() => outcomeView(this.outcome(), this.canRunAgain()));
}
