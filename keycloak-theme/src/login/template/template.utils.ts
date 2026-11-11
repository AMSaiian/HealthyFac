import { AUTOPLAY_BLOCKED_ERROR, REDUCED_MOTION_QUERY } from './template.constants';

export interface BackgroundVideo {
  muted: boolean;
  pause(): void;
  play(): Promise<void>;
}

/** Tells whether the visitor asked for reduced motion. A window without `matchMedia` counts as no preference. */
export function isReducedMotion(view: Pick<Window, 'matchMedia'> | null | undefined): boolean {
  return view?.matchMedia(REDUCED_MOTION_QUERY).matches ?? false;
}

/**
 * Pauses the background video for reduced motion, and plays it muted otherwise. A browser that blocks autoplay only
 * delays the video until the next user action; any other error is thrown.
 */
export function applyMotionPreference(video: BackgroundVideo, isMotionReduced: boolean): Promise<void> {
  if (isMotionReduced) {
    video.pause();
    return Promise.resolve();
  }
  video.muted = true;
  return video.play().catch((error: unknown) => {
    if (!(error instanceof DOMException && error.name === AUTOPLAY_BLOCKED_ERROR)) {
      throw error;
    }
  });
}

export interface MessageSlots {
  displayMessage?: boolean;
}

export interface PageMessage {
  type: string;
}

/**
 * Gives the message that the template shows above the page, or `undefined`. Before the page component exists
 * (`slots` is `undefined`) nothing shows, so a page that hides the message never flashes it for one frame.
 */
export function shownMessageOf<TMessage extends PageMessage>(
  message: TMessage | undefined,
  slots: MessageSlots | undefined,
  isAppInitiatedAction: boolean | undefined,
): TMessage | undefined {
  const isHiddenWarning = message?.type === 'warning' && isAppInitiatedAction;
  return message && slots && (slots.displayMessage ?? true) && !isHiddenWarning ? message : undefined;
}
