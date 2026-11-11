export const RESOURCES_DIRECTORY = 'dist';

export const BACKGROUND_VIDEO_FILE = 'login-bg-painted-1080p.webm';

export const FAVICON_LINKS: readonly { rel: string; type?: string; sizes?: string; file: string }[] = [
  { rel: 'icon', type: 'image/svg+xml', file: 'favicon.svg' },
  { rel: 'icon', type: 'image/x-icon', sizes: '48x48', file: 'favicon.ico' },
  { rel: 'icon', type: 'image/png', sizes: '48x48', file: 'icon-48.png' },
  { rel: 'icon', type: 'image/png', sizes: '32x32', file: 'icon-32.png' },
  { rel: 'icon', type: 'image/png', sizes: '16x16', file: 'icon-16.png' },
  { rel: 'apple-touch-icon', file: 'apple-touch-icon.png' },
];

export const FLAGS_DIRECTORY = 'flags/4x3';

/** The error of `play()` when the browser blocks autoplay; the video then waits for the next user action. */
export const AUTOPLAY_BLOCKED_ERROR = 'NotAllowedError';

export const REDUCED_MOTION_QUERY = '(prefers-reduced-motion: reduce)';

export const MESSAGE_SEVERITIES = {
  success: 'success',
  warning: 'warn',
  info: 'info',
  error: 'error',
} as const;
