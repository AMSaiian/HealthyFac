import { type infer as Infer, object, string } from 'zod/mini';

/** The payload of the action `machines.drafts`: the machine whose pending drafts the dialog lists. */
export const machineDraftsActionPayloadSchema = object({ machineId: string() });

export type MachineDraftsActionPayload = Infer<typeof machineDraftsActionPayloadSchema>;

/** The payload of the action `drafts.compareMachine`: the update draft that the user compares with the machine. */
export const draftCompareActionPayloadSchema = object({ draftId: string() });

export type DraftCompareActionPayload = Infer<typeof draftCompareActionPayloadSchema>;

/** The payload of the action `drafts.reviewStatus`: the status draft, its attempt, and the target machine. */
export const statusDraftReviewActionPayloadSchema = object({
  machineId: string(),
  attemptId: string(),
  draftId: string(),
});

export type StatusDraftReviewActionPayload = Infer<typeof statusDraftReviewActionPayloadSchema>;
