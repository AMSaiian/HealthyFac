import type { Provider } from '@angular/core';
import { Subject } from 'rxjs';
import { vi } from 'vitest';

import type { ChatTurnEnd, ChatTurnUpdate } from '@shared/chat/chat';
import type { RealtimeHint, RealtimeNotification } from '@shared/realtime/realtime';
import { RealtimeConnection } from '@shared/realtime/realtime-connection';

/** The streams of a fake `RealtimeConnection`: a spec pushes a hint or a reconnect through them. */
export interface FakeRealtimeConnection {
  readonly hints$: Subject<RealtimeHint>;
  readonly notifications$: Subject<RealtimeNotification>;
  readonly chatUpdates$: Subject<ChatTurnUpdate>;
  readonly chatEnds$: Subject<ChatTurnEnd>;
  readonly sessionEnded$: Subject<void>;
  readonly reconnected$: Subject<void>;
  readonly start: ReturnType<typeof vi.fn>;
  readonly stop: ReturnType<typeof vi.fn>;
}

export function createFakeRealtimeConnection(): FakeRealtimeConnection {
  return {
    hints$: new Subject<RealtimeHint>(),
    notifications$: new Subject<RealtimeNotification>(),
    chatUpdates$: new Subject<ChatTurnUpdate>(),
    chatEnds$: new Subject<ChatTurnEnd>(),
    sessionEnded$: new Subject<void>(),
    reconnected$: new Subject<void>(),
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
  };
}

/** Provides a fake `RealtimeConnection`, so a store that composes `withRealtimeReload` needs no hub. */
export function provideFakeRealtimeConnection(fake: FakeRealtimeConnection = createFakeRealtimeConnection()): Provider {
  return { provide: RealtimeConnection, useValue: fake };
}
