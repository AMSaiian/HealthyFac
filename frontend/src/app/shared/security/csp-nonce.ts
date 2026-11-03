export const CSP_NONCE_PLACEHOLDER = '__CSP_NONCE__';

const NONCE_ATTRIBUTE = 'ngCspNonce';

export function readCspNonce(root: Element | null): string | undefined {
  const nonce = root?.getAttribute(NONCE_ATTRIBUTE);
  return nonce && nonce !== CSP_NONCE_PLACEHOLDER ? nonce : undefined;
}
