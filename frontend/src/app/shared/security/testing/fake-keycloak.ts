import type Keycloak from 'keycloak-js';
import { vi } from 'vitest';

export interface FakeKeycloak {
  init: ReturnType<typeof vi.fn>;
  login: ReturnType<typeof vi.fn>;
  logout: ReturnType<typeof vi.fn>;
  updateToken: ReturnType<typeof vi.fn>;
  isTokenExpired: ReturnType<typeof vi.fn>;
  token: string | undefined;
  refreshToken: string | undefined;
  tokenParsed: object | undefined;
  idTokenParsed: object | undefined;
  onTokenExpired: (() => void) | undefined;
  onAuthRefreshSuccess: (() => void) | undefined;
}

export function createFakeKeycloak(): FakeKeycloak {
  return {
    init: vi.fn().mockResolvedValue(false),
    login: vi.fn().mockResolvedValue(undefined),
    logout: vi.fn().mockResolvedValue(undefined),
    updateToken: vi.fn().mockResolvedValue(true),
    isTokenExpired: vi.fn().mockReturnValue(false),
    token: 'token-1',
    refreshToken: 'refresh-1',
    tokenParsed: {},
    idTokenParsed: {},
    onTokenExpired: undefined,
    onAuthRefreshSuccess: undefined,
  };
}

export function asKeycloak(fake: FakeKeycloak): Keycloak {
  return fake as unknown as Keycloak;
}
