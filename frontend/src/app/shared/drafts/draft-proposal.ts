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
  /** The display name of the chat owner. Only a chat draft has it. */
  readonly requestedByName?: string | null;
}

const SOURCE_KEYS: Readonly<Record<DraftSource, string>> = {
  [DraftSource.Ingest]: 'DRAFTS.SOURCES.INGEST',
  [DraftSource.Case]: 'DRAFTS.SOURCES.CASE',
  [DraftSource.Chat]: 'DRAFTS.SOURCES.CHAT',
};

/** The translation key of the source of a draft: a file ingest, a maintenance case, or a chat. */
export function draftSourceKey(source: DraftSource): string {
  return SOURCE_KEYS[source];
}

/** The source text of a draft: the translated source, and the chat owner after it (`Chat · Ann`). */
export function draftSourceText(label: string, requestedByName: string | null | undefined): string {
  return requestedByName ? `${label} · ${requestedByName}` : label;
}
