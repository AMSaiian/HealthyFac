import { inject } from '@angular/core';
import type { RedirectCommand, ResolveFn } from '@angular/router';
import { tap } from 'rxjs';

import { catchResolverFailure } from '@shared/http/resolver-failure';
import { Temporal } from '@shared/time/temporal';

import type { Machine } from '@machines/machine';
import { MetricChartPageStore } from '@machines/signals/metric-chart-page-store/metric-chart-page.store';
import { MACHINE_ID_PARAMETER } from '@machines/statuses/list/status-definitions.resolver';

/** The query of a link that opens the dashboards on a from-to range (ISO instants), as the case page builds it. */
const FROM_PARAMETER = 'from';
const TO_PARAMETER = 'to';
const METRIC_KEY_PARAMETER = 'metricKey';

function isInstantRange(from: string, to: string): boolean {
  try {
    return Temporal.Instant.compare(Temporal.Instant.from(to), Temporal.Instant.from(from)) > 0;
  } catch {
    return false;
  }
}

/**
 * Opens the machine of the route in the metric chart store before the page activates. A missing machine (`404`) leads
 * to the not-found page, and a network failure (status `0`) to the network page. Both keep the requested address in the
 * address bar (`browserUrl`). Another HTTP failure shows its status toast and cancels the navigation.
 */
export const metricChartResolver: ResolveFn<Machine | RedirectCommand> = (route) => {
  const store = inject(MetricChartPageStore);

  return store.openMachine(route.paramMap.get(MACHINE_ID_PARAMETER) ?? '').pipe(
    tap(() => {
      const from = route.queryParamMap.get(FROM_PARAMETER);
      const to = route.queryParamMap.get(TO_PARAMETER);
      if (from !== null && to !== null && isInstantRange(from, to)) {
        store.selectCustomRange(from, to);
      }
      const metricKey = route.queryParamMap.get(METRIC_KEY_PARAMETER);
      if (metricKey !== null) {
        store.preferMetricKey(metricKey);
      }
    }),
    catchResolverFailure(),
  );
};
