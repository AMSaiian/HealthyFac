const RETRY_DELAYS_MILLISECONDS = [0, 2000, 10000, 30000] as const;
const MAX_JITTER_RATIO = 0.2;

/** The delay before the next connection attempt: 0, 2, 10, then 30 seconds, plus up to 20 percent of jitter. */
export function retryDelay(attempt: number, random: () => number): number {
  const base = RETRY_DELAYS_MILLISECONDS[Math.min(attempt, RETRY_DELAYS_MILLISECONDS.length - 1)];
  return base * (1 + random() * MAX_JITTER_RATIO);
}
