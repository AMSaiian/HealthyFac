import { type EnvironmentProviders, inject, provideAppInitializer, type Provider } from '@angular/core';
import { Router } from '@angular/router';
import { Location, PlatformNavigation } from '@angular/common';
import type { SpyLocation } from '@angular/common/testing';
import { provideLocationMocks } from '@angular/common/testing';

type HistoryEntry = NonNullable<PlatformNavigation['currentEntry']>;

interface FakeEntry {
  readonly key: string;
  url: string;
  state: unknown;
}

const FAKE_ORIGIN = 'http://localhost';
const NOT_SUPPORTED = 'Not supported by the fake';

function toAbsoluteUrl(path: string, query: string): string {
  return new URL(`${path.length === 0 ? '/' : path}${query.length > 0 ? `?${query}` : ''}`, FAKE_ORIGIN).href;
}

/**
 * A Navigation API fake for tests. It follows a `SpyLocation`, so back, forward, and the entry state work in jsdom.
 * Test code only.
 */
export class FakePlatformNavigation extends PlatformNavigation {
  private _entries: FakeEntry[];
  private _index = 0;
  private _nextKey = 1;

  constructor(location: SpyLocation) {
    super();
    this._entries = [{ key: '0', url: toAbsoluteUrl(location.path(), ''), state: undefined }];
    this._follow(location);
  }

  override get canGoBack(): boolean {
    return this._index > 0;
  }

  override get canGoForward(): boolean {
    return this._index < this._entries.length - 1;
  }

  override get currentEntry(): HistoryEntry {
    return this._toHistoryEntry(this._index);
  }

  override entries(): ReturnType<PlatformNavigation['entries']> {
    return this._entries.map((_entry, index) => this._toHistoryEntry(index));
  }

  override updateCurrentEntry(options: { state: unknown }): void {
    this._entries[this._index].state = options.state;
  }

  /** Sets the entry state before the app starts, like a refresh of a page that has a state. */
  seedCurrentEntryState(state: unknown): void {
    this._entries[this._index].state = state;
  }

  override transition = null;
  override onnavigate = null;
  override onnavigatesuccess = null;
  override onnavigateerror = null;
  override oncurrententrychange = null;

  override navigate(): never {
    throw new Error(NOT_SUPPORTED);
  }

  override reload(): never {
    throw new Error(NOT_SUPPORTED);
  }

  override traverseTo(): never {
    throw new Error(NOT_SUPPORTED);
  }

  override back(): never {
    throw new Error(NOT_SUPPORTED);
  }

  override forward(): never {
    throw new Error(NOT_SUPPORTED);
  }

  override addEventListener(): never {
    throw new Error(NOT_SUPPORTED);
  }

  override removeEventListener(): never {
    throw new Error(NOT_SUPPORTED);
  }

  override dispatchEvent(): never {
    throw new Error(NOT_SUPPORTED);
  }

  private _follow(location: SpyLocation): void {
    const go = location.go.bind(location);
    const replaceState = location.replaceState.bind(location);
    const back = location.back.bind(location);
    const forward = location.forward.bind(location);
    const historyGo = location.historyGo.bind(location);
    location.go = (path: string, query = '', state: unknown = null): void => {
      go(path, query, state);
      this._entries.splice(this._index + 1);
      this._entries.push({ key: String(this._nextKey++), url: toAbsoluteUrl(path, query), state: undefined });
      this._index = this._entries.length - 1;
    };
    location.replaceState = (path: string, query = '', state: unknown = null): void => {
      replaceState(path, query, state);
      this._entries[this._index].url = toAbsoluteUrl(path, query);
      this._entries[this._index].state = undefined;
    };
    location.back = (): void => {
      this._move(-1);
      back();
    };
    location.forward = (): void => {
      this._move(1);
      forward();
    };
    location.historyGo = (relativePosition = 0): void => {
      this._move(relativePosition);
      historyGo(relativePosition);
    };
  }

  private _move(delta: number): void {
    const next = this._index + delta;
    if (next >= 0 && next < this._entries.length) {
      this._index = next;
    }
  }

  private _toHistoryEntry(index: number): HistoryEntry {
    const entry = this._entries[index];
    return {
      key: entry.key,
      id: entry.key,
      url: entry.url,
      index,
      sameDocument: true,
      getState: () => entry.state,
    } as unknown as HistoryEntry;
  }
}

export function provideFakePlatformNavigation(): (Provider | EnvironmentProviders)[] {
  return [
    ...provideLocationMocks(),
    provideAppInitializer(() => {
      inject(Router).setUpLocationChangeListener();
    }),
    { provide: PlatformNavigation, useFactory: () => new FakePlatformNavigation(inject(Location) as SpyLocation) },
  ];
}
