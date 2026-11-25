/** Where a draft proposal came from. */
export enum DraftSource {
  Ingest = 'ingest',
  Case = 'case',
  Chat = 'chat',
}

/** The origin of a draft. A draft holds the IDs of its own source only. */
export interface DraftProposalSummary {
  readonly source: DraftSource;
  readonly attemptId: string | null;
  /** Telemetry rows do not send it. */
  readonly caseId?: string | null;
  readonly chatId: string | null;
}
