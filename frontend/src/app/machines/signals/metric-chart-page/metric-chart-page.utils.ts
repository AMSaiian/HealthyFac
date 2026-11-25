/** The route segment of the metric chart page after the machine ID. */
export const SIGNALS_SEGMENT = 'signals';

export const CHART_TAB = 'chart';
export const PHASES_TAB = 'phases';
export const STATES_TAB = 'states';

/** The tabs of the metric chart page; the first one has no route segment. */
export const CHART_TABS = [CHART_TAB, PHASES_TAB, STATES_TAB] as const;
