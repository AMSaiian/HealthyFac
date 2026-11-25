export const SUMMARY_TAB = 'summary';
export const SIGNALS_TAB = 'signals';
export const AI_LOG_TAB = 'aiLog';

/** The tabs of the case page; the first one has no route segment. */
export const CASE_TABS = [SUMMARY_TAB, SIGNALS_TAB, AI_LOG_TAB] as const;
