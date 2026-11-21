import { inject, Injectable, InjectionToken } from '@angular/core';
import { HttpTransportType, type HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { type Observable, Subject } from 'rxjs';

import { APP_CONFIG } from '@shared/config/app-config';
import { REALTIME_HUB_PATH, type RealtimeHint, type RealtimeNotification } from '@shared/realtime/realtime';
import { retryDelay } from '@shared/realtime/realtime-connection.utils';
import { AuthStore, MIN_TOKEN_VALIDITY_SECONDS } from '@shared/security/auth-store/auth.store';
import { CLOCK } from '@shared/time/clock';

const MILLISECONDS_PER_SECOND = 1000;
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
 * `reconnected$`, because the pages missed the hints of the gap. The first start and a planned renew emit nothing.
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
    const connection = this._current;
    this._current = null;
    await connection?.stop();
  }

  private async _connect(attempt: number, kind: ConnectKind): Promise<void> {
    if (kind === 'renew' && !(await this._refreshToken())) {
      this._retryAfterFailure(attempt, kind);
      return;
    }
    const connection = this._createConnection();
    this._attach(connection);
    try {
      await connection.start();
    } catch {
      await connection.stop().catch(() => undefined);
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
