import { type infer as Infer, object, strictObject, string, union } from 'zod/mini';

/**
 * The payload of the action `machines.drafts`: the machine whose pending drafts the dialog lists, or the ingest attempt
 * or the chat whose drafts it lists.
 */
export const machineDraftsActionPayloadSchema = union([
  strictObject({ machineId: string() }),
  strictObject({ attemptId: string() }),
  strictObject({ chatId: string() }),
]);

export type MachineDraftsActionPayload = Infer<typeof machineDraftsActionPayloadSchema>;

/** The payload of the action `drafts.compareMachine`: the update draft that the user compares with the machine. */
export const draftCompareActionPayloadSchema = object({ draftId: string() });

export type DraftCompareActionPayload = Infer<typeof draftCompareActionPayloadSchema>;

/** The payload of a draft review action: the draft, the target machine, and exactly one of its attempt or its chat. */
const draftReviewPayloadSchema = union([
  strictObject({ machineId: string(), attemptId: string(), draftId: string() }),
  strictObject({ machineId: string(), chatId: string(), draftId: string() }),
]);

/** The payload of the action `drafts.reviewStatus`: the status draft, its attempt or chat, and the target machine. */
export const statusDraftReviewActionPayloadSchema = draftReviewPayloadSchema;

export type StatusDraftReviewActionPayload = Infer<typeof statusDraftReviewActionPayloadSchema>;

/** The payload of the action `drafts.reviewPhase`: the phase draft, its attempt or chat, and the target machine. */
export const phaseDraftReviewActionPayloadSchema = draftReviewPayloadSchema;

export type PhaseDraftReviewActionPayload = Infer<typeof phaseDraftReviewActionPayloadSchema>;
