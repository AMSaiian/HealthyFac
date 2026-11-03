import { CSP_NONCE_PLACEHOLDER, readCspNonce } from './csp-nonce';

function createRoot(nonce: string | null): Element {
  const root = document.createElement('hf-root');
  if (nonce !== null) {
    root.setAttribute('ngCspNonce', nonce);
  }
  return root;
}

describe('readCspNonce', () => {
  it('readCspNonce_WithValue_ReturnsValue', () => {
    const result = readCspNonce(createRoot('abc123'));

    expect(result).toBe('abc123');
  });

  it.each([CSP_NONCE_PLACEHOLDER, null])('readCspNonce_WithPlaceholderOrMissing_ReturnsUndefined (%s)', (nonce) => {
    const result = readCspNonce(createRoot(nonce));

    expect(result).toBeUndefined();
  });

  it('readCspNonce_WithoutRoot_ReturnsUndefined', () => {
    const result = readCspNonce(null);

    expect(result).toBeUndefined();
  });
});
