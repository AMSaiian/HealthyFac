import { describe, expect, it, vi } from 'vitest';
import { applyMotionPreference, type BackgroundVideo, isReducedMotion, shownMessageOf } from './template.utils';

function createVideo(play: () => Promise<void> = () => Promise.resolve()): BackgroundVideo & {
  pause: ReturnType<typeof vi.fn>;
  play: ReturnType<typeof vi.fn>;
} {
  return { muted: false, pause: vi.fn(), play: vi.fn(play) };
}

describe('motion preference', () => {
  it('isReducedMotion_WithReduceQuery_IsTrue', () => {
    const view = { matchMedia: vi.fn(() => ({ matches: true })) } as unknown as Pick<Window, 'matchMedia'>;

    expect(isReducedMotion(view)).toBe(true);
    expect(view.matchMedia).toHaveBeenCalledWith('(prefers-reduced-motion: reduce)');
  });

  it('isReducedMotion_WithoutWindow_IsFalse', () => {
    expect(isReducedMotion(null)).toBe(false);
  });

  it('applyMotionPreference_WithReducedMotion_PausesAndNeverPlays', async () => {
    const video = createVideo();

    await applyMotionPreference(video, true);

    expect(video.pause).toHaveBeenCalledTimes(1);
    expect(video.play).not.toHaveBeenCalled();
  });

  it('applyMotionPreference_WithoutReducedMotion_PlaysMuted', async () => {
    const video = createVideo();

    await applyMotionPreference(video, false);

    expect(video.muted).toBe(true);
    expect(video.play).toHaveBeenCalledTimes(1);
    expect(video.pause).not.toHaveBeenCalled();
  });

  it('applyMotionPreference_WhenAutoplayIsBlocked_Resolves', async () => {
    const video = createVideo(() => Promise.reject(new DOMException('blocked', 'NotAllowedError')));

    await expect(applyMotionPreference(video, false)).resolves.toBeUndefined();
  });

  it('applyMotionPreference_WithAnotherPlayError_Rejects', async () => {
    const video = createVideo(() => Promise.reject(new DOMException('bad', 'NotSupportedError')));

    await expect(applyMotionPreference(video, false)).rejects.toThrow('bad');
  });
});

describe('shownMessageOf', () => {
  const error = { type: 'error', summary: 'Action expired' };

  it('shownMessageOf_BeforeThePageExists_ShowsNothing', () => {
    expect(shownMessageOf(error, undefined, false)).toBeUndefined();
  });

  it('shownMessageOf_ForAPageThatHidesMessages_ShowsNothing', () => {
    expect(shownMessageOf(error, { displayMessage: false }, false)).toBeUndefined();
  });

  it('shownMessageOf_ForAPageThatShowsMessages_ShowsTheMessage', () => {
    expect(shownMessageOf(error, { displayMessage: true }, false)).toBe(error);
    expect(shownMessageOf(error, {}, false)).toBe(error);
  });

  it('shownMessageOf_ForAWarningOfAnAppInitiatedAction_ShowsNothing', () => {
    expect(shownMessageOf({ type: 'warning' }, { displayMessage: true }, true)).toBeUndefined();
  });
});
