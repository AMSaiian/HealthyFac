import { TestBed } from '@angular/core/testing';
import { HttpTransportType, type HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

import { HUB_CONNECTION_FACTORY, RealtimeConnection, START_TIMEOUT_MILLISECONDS } from './realtime-connection';
import { type RealtimeHint, type RealtimeNotification, RealtimeNotificationKind, RealtimeTopic } from './realtime';
import { APP_CONFIG, type AppConfig } from '@shared/config/app-config';
import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

class FakeHub {
  readonly handlers = new Map<string, (payload?: unknown) => void>();
  closeHandler: ((error?: Error) => void) | undefined;
  isStarted = false;
  stopCount = 0;
  startBehavior: Error | null | 'pending' = null;
  private _settlePendingStart: (() => void) | null = null;
  private _resolvePendingStart: (() => void) | null = null;
  private _isStartPending = false;

  start = vi.fn(() => {
    if (this.startBehavior === 'pending') {
      this._isStartPending = true;
      return new Promise<void>((resolve) => {
        this._resolvePendingStart = () => {
          this._isStartPending = false;
          resolve();
        };
      });
    }
    if (this.startBehavior !== null) {
      return Promise.reject(this.startBehavior);
    }
    this.isStarted = true;
    return Promise.resolve();
  });
  stop = vi.fn(() => {
    this.stopCount += 1;
    this.isStarted = false;
    if (!this._isStartPending) {
      return Promise.resolve();
    }
    // As the SignalR client does: a stop during a start resolves only after that start has ended.
    return new Promise<void>((resolve) => {
      this._settlePendingStart = resolve;
    });
  });

  /** Ends a pending start with success, as a slow network does. */
  endPendingStart(): void {
    this.isStarted = true;
    this._resolvePendingStart?.();
    this._settlePendingStart?.();
  }

  on(method: string, handler: (payload?: unknown) => void): void {
    this.handlers.set(method, handler);
  }

  onclose(handler: (error?: Error) => void): void {
    this.closeHandler = handler;
  }

  onreconnected(): void {
    return;
  }
}

const NOW = Temporal.Instant.from('2026-11-21T10:00:00Z');
const TOKEN_LIFETIME_MS = 300_000;
const RENEW_LEAD_MS = 30_000;

describe('RealtimeConnection', () => {
  let hubs: FakeHub[];
  let startErrors: (Error | null | 'pending')[];
  let isAuthenticated: boolean;
  let calls: string[];
  let expiresAt: Temporal.Instant;
  let isRefreshExtending: boolean;
  let refreshError: Error | null;

  function createConnection(): RealtimeConnection {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: HUB_CONNECTION_FACTORY,
          useValue: () => {
            const hub = new FakeHub();
            calls.push('create');
            hub.startBehavior = startErrors.shift() ?? null;
            hubs.push(hub);
            return hub as unknown as HubConnection;
          },
        },
        provideFakeAuthStore({
          isAuthenticated: () => isAuthenticated,
          tokenExpiresAt: () => expiresAt,
          forceRefresh: () => {
            calls.push('refresh');
            if (refreshError !== null) {
              return Promise.reject(refreshError);
            }
            if (isRefreshExtending) {
              expiresAt = expiresAt.add({ milliseconds: TOKEN_LIFETIME_MS });
            }
            return Promise.resolve();
          },
        }),
        {
          provide: CLOCK,
          useValue: {
            now: () => Temporal.Instant.fromEpochMilliseconds(vi.getMockedSystemTime()?.getTime() ?? 0),
            timeZoneId: () => 'UTC',
          },
        },
      ],
    });
    return TestBed.inject(RealtimeConnection);
  }

  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(NOW.epochMilliseconds);
    hubs = [];
    startErrors = [];
    isAuthenticated = true;
    calls = [];
    expiresAt = NOW.add({ milliseconds: TOKEN_LIFETIME_MS });
    isRefreshExtending = true;
    refreshError = null;
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('factory_Default_BuildsAWebSocketsConnectionWithoutNegotiate', async () => {
    const accessToken = vi.fn().mockResolvedValue('token-1');
    TestBed.configureTestingModule({
      providers: [
        { provide: APP_CONFIG, useValue: { apiBaseUrl: 'https://api.example.test' } as AppConfig },
        provideFakeAuthStore({ accessToken }),
      ],
    });

    const withUrl = vi.spyOn(HubConnectionBuilder.prototype, 'withUrl');

    TestBed.inject(HUB_CONNECTION_FACTORY)();

    const [url, options] = withUrl.mock.calls[0] as unknown as [
      string,
      { transport: HttpTransportType; skipNegotiation: boolean; accessTokenFactory: () => Promise<string> },
    ];
    expect(url).toBe('https://api.example.test/api/realtime/v1/hub');
    expect(options.transport).toBe(HttpTransportType.WebSockets);
    expect(options.skipNegotiation).toBe(true);
    expect(await options.accessTokenFactory()).toBe('token-1');
  });

  it('start_Always_StartsOneConnection', async () => {
    const connection = createConnection();

    await connection.start();

    expect(hubs).toHaveLength(1);
    expect(hubs[0].isStarted).toBe(true);
  });

  it('stop_AfterStart_StopsTheConnection', async () => {
    const connection = createConnection();
    await connection.start();

    await connection.stop();

    expect(hubs[0].stopCount).toBe(1);
  });

  it('onNotification_FromTheHub_EmitsOnNotifications', async () => {
    const connection = createConnection();
    const received: RealtimeNotification[] = [];
    connection.notifications$.subscribe((notification) => received.push(notification));
    await connection.start();
    const notification = {
      notificationId: 'n-1',
      kind: RealtimeNotificationKind.ShiftChanged,
      occurredAt: NOW.toString(),
      actorSubject: null,
      data: { employeeId: 'e-1', date: '2026-11-21' },
    };

    hubs[0].handlers.get('Notification')?.(notification);

    expect(received).toEqual([notification]);
  });

  it('onHint_FromTheHub_EmitsOnHints', async () => {
    const connection = createConnection();
    const received: RealtimeHint[] = [];
    connection.hints$.subscribe((hint) => received.push(hint));
    await connection.start();

    hubs[0].handlers.get('Hint')?.({ topic: RealtimeTopic.Cases, caseId: 'c-1' });

    expect(received).toEqual([{ topic: RealtimeTopic.Cases, caseId: 'c-1' }]);
  });

  it('renew_BeforeTheTokenExpires_StartsASecondConnectionAndStopsTheFirstAfterIt', async () => {
    const connection = createConnection();
    await connection.start();

    await vi.advanceTimersByTimeAsync(TOKEN_LIFETIME_MS - RENEW_LEAD_MS - 1);
    expect(hubs).toHaveLength(1);
    await vi.advanceTimersByTimeAsync(1);

    expect(hubs).toHaveLength(2);
    expect(hubs[1].isStarted).toBe(true);
    expect(hubs[0].stopCount).toBe(1);
  });

  it('renew_Always_RefreshesTheTokenBeforeItCreatesTheNewConnection', async () => {
    const connection = createConnection();
    await connection.start();

    await vi.advanceTimersByTimeAsync(TOKEN_LIFETIME_MS - RENEW_LEAD_MS);

    expect(calls).toEqual(['create', 'refresh', 'create']);
  });

  it('renew_WhenTheRefreshDoesNotChangeTheExpiry_StartsNoSecondRenewAtOnce', async () => {
    isRefreshExtending = false;
    const connection = createConnection();
    await connection.start();

    await vi.advanceTimersByTimeAsync(TOKEN_LIFETIME_MS - RENEW_LEAD_MS);
    await vi.advanceTimersByTimeAsync(5000);

    expect(hubs).toHaveLength(2);
    expect(calls.filter((call) => call === 'refresh')).toHaveLength(1);
  });

  it('renew_WhenTheRefreshFails_CreatesNoConnectionAndRetriesWithTheDelays', async () => {
    vi.spyOn(Math, 'random').mockReturnValue(0);
    const connection = createConnection();
    await connection.start();
    refreshError = new Error('refresh failed');

    await vi.advanceTimersByTimeAsync(TOKEN_LIFETIME_MS - RENEW_LEAD_MS);
    expect(hubs).toHaveLength(1);
    refreshError = null;
    await vi.advanceTimersByTimeAsync(2000);

    expect(hubs).toHaveLength(2);
  });

  it('reconnected_AfterARetryOfAFailedFirstStart_Emits', async () => {
    startErrors.push(new Error('1'));
    vi.spyOn(Math, 'random').mockReturnValue(0);
    const connection = createConnection();
    const reconnected = vi.fn();
    connection.reconnected$.subscribe(reconnected);

    await connection.start();
    expect(reconnected).not.toHaveBeenCalled();
    await vi.advanceTimersByTimeAsync(2000);

    expect(reconnected).toHaveBeenCalledTimes(1);
  });

  it('reconnected_AfterACloseWithAnError_Emits', async () => {
    vi.spyOn(Math, 'random').mockReturnValue(0);
    const connection = createConnection();
    const reconnected = vi.fn();
    connection.reconnected$.subscribe(reconnected);
    await connection.start();

    hubs[0].closeHandler?.(new Error('closed by the server'));
    await vi.advanceTimersByTimeAsync(0);

    expect(reconnected).toHaveBeenCalledTimes(1);
  });

  it('reconnected_AfterTheFirstStartAndAPlannedRenew_DoesNotEmit', async () => {
    const connection = createConnection();
    const reconnected = vi.fn();
    connection.reconnected$.subscribe(reconnected);

    await connection.start();
    await vi.advanceTimersByTimeAsync(TOKEN_LIFETIME_MS - RENEW_LEAD_MS);

    expect(hubs).toHaveLength(2);
    expect(reconnected).not.toHaveBeenCalled();
  });

  it('renew_AfterARestartFollowingACloseWithAnError_StillRunsAtTheRenewTime', async () => {
    vi.spyOn(Math, 'random').mockReturnValue(0);
    const connection = createConnection();
    await connection.start();

    hubs[0].closeHandler?.(new Error('closed by the server'));
    await vi.advanceTimersByTimeAsync(0);
    expect(hubs).toHaveLength(2);
    await vi.advanceTimersByTimeAsync(TOKEN_LIFETIME_MS - RENEW_LEAD_MS);

    expect(hubs).toHaveLength(3);
    expect(calls.filter((call) => call === 'refresh')).toHaveLength(1);
  });

  it('start_WhenTheStartNeverEnds_StopsTheConnectionAt30SecondsAndRetriesAfterTheFirstDelay', async () => {
    startErrors.push('pending');
    vi.spyOn(Math, 'random').mockReturnValue(0);
    const connection = createConnection();

    void connection.start();
    await vi.advanceTimersByTimeAsync(START_TIMEOUT_MILLISECONDS - 1);
    expect(hubs[0].stopCount).toBe(0);
    await vi.advanceTimersByTimeAsync(1);
    expect(hubs[0].stopCount).toBeGreaterThan(0);
    expect(hubs).toHaveLength(1);
    await vi.advanceTimersByTimeAsync(2000);

    expect(hubs).toHaveLength(2);
    expect(hubs[1].isStarted).toBe(true);
  });

  it('start_WhenTheStartEndsAfter29Seconds_KeepsTheConnectionAndStartsNoSecondOne', async () => {
    startErrors.push('pending');
    const connection = createConnection();
    void connection.start();
    await vi.advanceTimersByTimeAsync(START_TIMEOUT_MILLISECONDS - 1000);

    hubs[0].endPendingStart();
    await vi.advanceTimersByTimeAsync(START_TIMEOUT_MILLISECONDS * 2);

    expect(hubs).toHaveLength(1);
    expect(hubs[0].stopCount).toBe(0);
  });

  it('renew_WhenTheNewStartNeverEnds_KeepsTheOldConnectionStopsThePendingOneAndRetries', async () => {
    vi.spyOn(Math, 'random').mockReturnValue(0);
    const connection = createConnection();
    await connection.start();
    startErrors.push('pending');

    await vi.advanceTimersByTimeAsync(TOKEN_LIFETIME_MS - RENEW_LEAD_MS);
    expect(hubs).toHaveLength(2);
    await vi.advanceTimersByTimeAsync(START_TIMEOUT_MILLISECONDS);
    expect(hubs[1].stopCount).toBeGreaterThan(0);
    expect(hubs[0].stopCount).toBe(0);
    await vi.advanceTimersByTimeAsync(2000);

    expect(hubs).toHaveLength(3);
  });

  it('stop_DuringAPendingStart_StartsNoNewConnectionAfter30Seconds', async () => {
    startErrors.push('pending');
    const connection = createConnection();
    void connection.start();
    await vi.advanceTimersByTimeAsync(1000);

    await connection.stop();
    await vi.advanceTimersByTimeAsync(START_TIMEOUT_MILLISECONDS * 3);

    expect(hubs).toHaveLength(1);
    expect(hubs[0].stopCount).toBeGreaterThan(0);
  });

  it('renew_WhenTheNewStartFails_KeepsTheFirstConnectionRunning', async () => {
    const connection = createConnection();
    await connection.start();
    startErrors.push(new Error('unavailable'));

    await vi.advanceTimersByTimeAsync(TOKEN_LIFETIME_MS - RENEW_LEAD_MS);

    expect(hubs).toHaveLength(2);
    expect(hubs[0].stopCount).toBe(0);
  });

  it('start_WhenTheStartKeepsFailing_RetriesAfter2000And10000And30000Milliseconds', async () => {
    startErrors.push(new Error('1'), new Error('2'), new Error('3'));
    vi.spyOn(Math, 'random').mockReturnValue(0);
    const connection = createConnection();

    await connection.start();
    expect(hubs).toHaveLength(1);
    await vi.advanceTimersByTimeAsync(1999);
    expect(hubs).toHaveLength(1);
    await vi.advanceTimersByTimeAsync(1);
    expect(hubs).toHaveLength(2);
    await vi.advanceTimersByTimeAsync(10_000);
    expect(hubs).toHaveLength(3);
    await vi.advanceTimersByTimeAsync(30_000);

    expect(hubs).toHaveLength(4);
    expect(hubs[3].isStarted).toBe(true);
  });

  it('onclose_WithAnError_StartsANewConnectionAfterZeroMilliseconds', async () => {
    vi.spyOn(Math, 'random').mockReturnValue(0);
    const connection = createConnection();
    await connection.start();

    hubs[0].closeHandler?.(new Error('closed by the server'));
    await vi.advanceTimersByTimeAsync(0);

    expect(hubs).toHaveLength(2);
    expect(hubs[1].isStarted).toBe(true);
  });

  it('onclose_WhenTheNewStartFails_FollowsTheRetryDelays', async () => {
    vi.spyOn(Math, 'random').mockReturnValue(0);
    const connection = createConnection();
    await connection.start();
    startErrors.push(new Error('1'), new Error('2'));

    hubs[0].closeHandler?.(new Error('closed by the server'));
    await vi.advanceTimersByTimeAsync(0);
    expect(hubs).toHaveLength(2);
    await vi.advanceTimersByTimeAsync(2000);
    expect(hubs).toHaveLength(3);
    await vi.advanceTimersByTimeAsync(10_000);

    expect(hubs).toHaveLength(4);
  });

  it('onclose_AfterStop_StartsNothing', async () => {
    const connection = createConnection();
    await connection.start();
    const hub = hubs[0];
    await connection.stop();

    hub.closeHandler?.(new Error('closed'));
    await vi.advanceTimersByTimeAsync(60_000);

    expect(hubs).toHaveLength(1);
  });

  it('onclose_WithoutAnError_StartsNothing', async () => {
    const connection = createConnection();
    await connection.start();

    hubs[0].closeHandler?.();
    await vi.advanceTimersByTimeAsync(60_000);

    expect(hubs).toHaveLength(1);
  });

  it('onclose_WhileSignedOut_StartsNothing', async () => {
    const connection = createConnection();
    await connection.start();
    isAuthenticated = false;

    hubs[0].closeHandler?.(new Error('closed'));
    await vi.advanceTimersByTimeAsync(60_000);

    expect(hubs).toHaveLength(1);
  });

  it('onclose_OfAReplacedConnection_StartsNothing', async () => {
    const connection = createConnection();
    await connection.start();
    await vi.advanceTimersByTimeAsync(TOKEN_LIFETIME_MS - RENEW_LEAD_MS);

    hubs[0].closeHandler?.(new Error('closed'));
    await vi.advanceTimersByTimeAsync(1);

    expect(hubs).toHaveLength(2);
  });
});
