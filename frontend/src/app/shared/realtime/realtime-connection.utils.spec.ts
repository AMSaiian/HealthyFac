import { retryDelay } from './realtime-connection.utils';

describe('retryDelay', () => {
  it('retryDelay_WithZeroJitter_GivesTheBaseDelays', () => {
    const delays = [0, 1, 2, 3, 4, 5].map((attempt) => retryDelay(attempt, () => 0));

    expect(delays).toEqual([0, 2000, 10000, 30000, 30000, 30000]);
  });

  it('retryDelay_WithMaximumJitter_AddsAtMostTwentyPercent', () => {
    const delays = [1, 2, 3, 9].map((attempt) => retryDelay(attempt, () => 0.999));

    expect(delays[0]).toBeGreaterThan(2000);
    expect(delays[0]).toBeLessThanOrEqual(2400);
    expect(delays[1]).toBeGreaterThan(10000);
    expect(delays[1]).toBeLessThanOrEqual(12000);
    expect(delays[2]).toBeGreaterThan(30000);
    expect(delays[2]).toBeLessThanOrEqual(36000);
    expect(delays[3]).toBe(delays[2]);
  });
});
