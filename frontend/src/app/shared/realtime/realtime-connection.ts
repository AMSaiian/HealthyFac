import { inject, Injectable, InjectionToken } from '@angular/core';
import { HttpTransportType, type HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { type Observable, Subject } from 'rxjs';

import { APP_CONFIG } from '@shared/config/app-config';
import { REALTIME_HUB_PATH, type RealtimeHint, type RealtimeNotification } from '@shared/realtime/realtime';
import { retryDelay } from '@shared/realtime/realtime-connection.utils';
import { AuthStore, MIN_TOKEN_VALIDITY_SECONDS } from '@shared/security/auth-store/auth.store';
import { CLOCK } from '@shared/time/clock';

const MILLISECONDS_PER_SECOND = 1000;

/** A start that has not ended after this time counts as failed. */
export const START_TIMEOUT_MILLISECONDS = 30_000;

type ConnectKind = 'start' | 'renew' | 'recover';

const NOTIFICATION_METHOD = 'Notification';
const HINT_METHOD = 'Hint';
const SESSION_ENDED_METHOD = 'SessionEnded';

export const HUB_CONNECTION_FACTORY = new InjectionToken<() => HubConnection>('HUB_CONNECTION_FACTORY', {
  providedIn: 'root',
  factory: () => {
    const config = inject(APP_CONFIG);
    const authStore = inject(AuthStore);
    return (): HubConnection =>
      new HubConnectionBuilder()
        .withUrl(`${config.apiBaseUrl}${REALTIME_HUB_PATH}`, {
          transport: HttpTransportType.WebSockets,
          skipNegotiation: true,
          accessTokenFactory: () => authStore.accessToken(),
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (context) => retryDelay(context.previousRetryCount, Math.random),
        })
        .build();
  },
});

/**
 * Owns the one SignalR connection of the app. It starts after sign-in and stops at sign-out. A new connection
 * replaces the current one 30 seconds before the token expires, because the gateway closes a connection at the
 * token expiry. A failed start and a close with an error start a new connection with the retry delays. A new
 * connection that follows a drop (a retry of a failed start, a restart after a close with an error) emits
 * `reconnected$`, because the pages missed the hints of the gap. The first start and a planned renew emit nothing. A
 * start that has not ended after 30 seconds counts as failed, because a WebSocket that the browser blocks can fire no
 * close.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeConnection {
  private readonly _createConnection = inject(HUB_CONNECTION_FACTORY);
  private readonly _authStore = inject(AuthStore);
  private readonly _clock = inject(CLOCK);

  private readonly _notifications = new Subject<RealtimeNotification>();
  private readonly _hints = new Subject<RealtimeHint>();
  private readonly _sessionEnded = new Subject<void>();
  private readonly _reconnected = new Subject<void>();

  private _current: HubConnection | null = null;
  private _isStopped = true;
  private _retryTimer: ReturnType<typeof setTimeout> | null = null;
  private _renewTimer: ReturnType<typeof setTimeout> | null = null;
  private _lastRenewAt: number | null = null;
  private readonly _pendingStarts = new Map<HubConnection, ReturnType<typeof setTimeout>>();

  readonly notifications$: Observable<RealtimeNotification> = this._notifications.asObservable();
  readonly hints$: Observable<RealtimeHint> = this._hints.asObservable();
  readonly sessionEnded$: Observable<void> = this._sessionEnded.asObservable();
  readonly reconnected$: Observable<void> = this._reconnected.asObservable();

  start(): Promise<void> {
    this._isStopped = false;
    return this._connect(0, 'start');
  }

  async stop(): Promise<void> {
    this._isStopped = true;
    this._lastRenewAt = null;
    this._clearTimers();
    const pending = [...this._pendingStarts.keys()];
    for (const timer of this._pendingStarts.values()) {
      clearTimeout(timer);
    }
    this._pendingStarts.clear();
    const connection = this._current;
    this._current = null;
    for (const hub of pending) {
      this._stopWithoutWaiting(hub);
    }
    await connection?.stop();
  }

  private async _connect(attempt: number, kind: ConnectKind): Promise<void> {
    if (kind === 'renew' && !(await this._refreshToken())) {
      this._retryAfterFailure(attempt, kind);
      return;
    }
    const connection = this._createConnection();
    this._attach(connection);
    const isStarted = await this._startWithLimit(connection);
    if (!isStarted) {
      this._stopWithoutWaiting(connection);
      this._retryAfterFailure(attempt, kind);
      return;
    }
    if (this._isStopped) {
      await connection.stop();
      return;
    }
    const previous = this._current;
    this._current = connection;
    await previous?.stop();
    this._scheduleRenew();
    if (kind === 'recover') {
      this._reconnected.next();
    }
  }

  /**
   * Starts the connection and gives the result: `true` when it started, `false` when it failed or did not end within
   * `START_TIMEOUT_MILLISECONDS`. The late result of a start that the limit ended is ignored.
   */
  private _startWithLimit(connection: HubConnection): Promise<boolean> {
    return new Promise<boolean>((resolve) => {
      const finish = (isStarted: boolean): void => {
        const timer = this._pendingStarts.get(connection);
        if (timer !== undefined) {
          clearTimeout(timer);
          this._pendingStarts.delete(connection);
        }
        resolve(isStarted);
      };
      this._pendingStarts.set(
        connection,
        setTimeout(() => {
          this._pendingStarts.delete(connection);
          resolve(false);
        }, START_TIMEOUT_MILLISECONDS),
      );
      connection.start().then(
        () => {
          finish(true);
        },
        () => {
          finish(false);
        },
      );
    });
  }

  /**
   * Starts the stop of a connection and does not wait for it. The SignalR stop of a connection whose start has not
   * ended waits for that start, and a start that never ends never lets it finish, so a caller that waits could hang.
   */
  private _stopWithoutWaiting(connection: HubConnection): void {
    void connection.stop().catch(() => undefined);
  }

  /** The renew takes a fresh token through the one refresh of `AuthStore`, never the token that it still holds. */
  private async _refreshToken(): Promise<boolean> {
    try {
      await this._authStore.forceRefresh();
      return true;
    } catch {
      return false;
    }
  }

  private _retryAfterFailure(attempt: number, kind: ConnectKind): void {
    if (!this._isStopped) {
      this._scheduleRetry(attempt + 1, kind === 'start' ? 'recover' : kind);
    }
  }

  private _attach(connection: HubConnection): void {
    connection.on(NOTIFICATION_METHOD, (notification: RealtimeNotification) => {
      this._notifications.next(notification);
    });
    connection.on(HINT_METHOD, (hint: RealtimeHint) => {
      this._hints.next(hint);
    });
    connection.on(SESSION_ENDED_METHOD, () => {
      this._sessionEnded.next();
    });
    connection.onreconnected(() => {
      this._reconnected.next();
    });
    connection.onclose((error) => {
      if (connection !== this._current) {
        return;
      }
      this._current = null;
      if (error !== undefined && !this._isStopped && this._authStore.isAuthenticated()) {
        this._scheduleRetry(0, 'recover');
      }
    });
  }

  private _scheduleRetry(attempt: number, kind: ConnectKind): void {
    this._clearTimers();
    this._retryTimer = setTimeout(
      () => {
        this._retryTimer = null;
        void this._connect(attempt, kind);
      },
      retryDelay(attempt, Math.random),
    );
  }

  private _scheduleRenew(): void {
    if (this._renewTimer !== null) {
      clearTimeout(this._renewTimer);
      this._renewTimer = null;
    }
    const expiresAt = this._authStore.tokenExpiresAt();
    if (expiresAt === null) {
      return;
    }
    const renewAt = expiresAt.epochMilliseconds - MIN_TOKEN_VALIDITY_SECONDS * MILLISECONDS_PER_SECOND;
    if (this._lastRenewAt !== null && renewAt <= this._lastRenewAt) {
      return;
    }
    const delay = Math.max(0, renewAt - this._clock.now().epochMilliseconds);
    this._renewTimer = setTimeout(() => {
      this._renewTimer = null;
      this._lastRenewAt = renewAt;
      void this._connect(0, 'renew');
    }, delay);
  }

  private _clearTimers(): void {
    if (this._retryTimer !== null) {
      clearTimeout(this._retryTimer);
      this._retryTimer = null;
    }
    if (this._renewTimer !== null) {
      clearTimeout(this._renewTimer);
      this._renewTimer = null;
    }
  }
}
