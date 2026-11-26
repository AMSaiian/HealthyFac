import { safeParse } from 'zod/mini';

import {
  machineDraftsActionPayloadSchema,
  statusDraftReviewActionPayloadSchema,
} from '@machines/drafts/draft-action-payloads';

describe('draft action payloads', { tags: ['Drafts'] }, () => {
  it('statusDraftReviewActionPayloadSchema_WithAChatAndNoAttempt_Accepts', () => {
    const result = safeParse(statusDraftReviewActionPayloadSchema, { machineId: 'm', chatId: 'c', draftId: 'd' });

    expect(result.success).toBe(true);
  });

  it('statusDraftReviewActionPayloadSchema_WithNeitherAttemptNorChat_Rejects', () => {
    const result = safeParse(statusDraftReviewActionPayloadSchema, { machineId: 'm', draftId: 'd' });

    expect(result.success).toBe(false);
  });

  it('machineDraftsActionPayloadSchema_WithAChat_Accepts', () => {
    expect(safeParse(machineDraftsActionPayloadSchema, { chatId: 'c' }).success).toBe(true);
  });

  it('machineDraftsActionPayloadSchema_WithNoScope_Rejects', () => {
    expect(safeParse(machineDraftsActionPayloadSchema, {}).success).toBe(false);
  });

  it('statusDraftReviewActionPayloadSchema_WithBothAttemptAndChat_Rejects', () => {
    const result = safeParse(statusDraftReviewActionPayloadSchema, {
      machineId: 'm',
      attemptId: 'a',
      chatId: 'c',
      draftId: 'd',
    });

    expect(result.success).toBe(false);
  });

  it('machineDraftsActionPayloadSchema_WithBothAttemptAndChat_Rejects', () => {
    expect(safeParse(machineDraftsActionPayloadSchema, { attemptId: 'a', chatId: 'c' }).success).toBe(false);
  });
});
