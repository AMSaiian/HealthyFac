export const DraftActionType = {
  /** A draft was applied or dismissed, so the lists of drafts and the draft counts are old. */
  Changed: 'drafts.changed',
  CompareMachine: 'drafts.compareMachine',
  ReviewStatus: 'drafts.reviewStatus',
} as const;
