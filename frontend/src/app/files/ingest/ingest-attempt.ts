export enum IngestState {
  Queued = 'queued',
  InProcess = 'inProcess',
  Completed = 'completed',
  Failed = 'failed',
  Interrupted = 'interrupted',
}

export enum IngestErrorCode {
  FileNotFound = 'fileNotFound',
  NoText = 'noText',
  TooManyPages = 'tooManyPages',
  ConversionFailed = 'conversionFailed',
  ConversionTimeout = 'conversionTimeout',
  InvalidExtraction = 'invalidExtraction',
  ProviderUnavailable = 'providerUnavailable',
}

export enum IngestInterruptReason {
  Cancelled = 'cancelled',
  LeaseExpired = 'leaseExpired',
  ServiceStopped = 'serviceStopped',
  FileDeleted = 'fileDeleted',
}

export interface IngestAttempt {
  readonly id: string;
  readonly fileId: string;
  readonly machineId: string | null;
  readonly fileName: string;
  readonly extension: string;
  readonly sizeBytes: number;
  readonly state: IngestState;
  readonly errorCode: IngestErrorCode | null;
  readonly interruptReason: IngestInterruptReason | null;
  readonly warning: string | null;
  readonly queuedAt: string;
  readonly startedAt: string | null;
  readonly finishedAt: string | null;
  readonly pageCount: number | null;
  readonly chunkCount: number | null;
  readonly proposedDraftCount: number | null;
}

export interface IngestAttemptsPage {
  readonly items: readonly IngestAttempt[];
}

export interface StartIngestResponse {
  readonly id: string;
}
