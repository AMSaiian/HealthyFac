/** A signed, short-lived URL that reads one file. */
export interface FileLink {
  readonly id: string;
  readonly url: string;
  readonly expiresAt: string;
}

export interface FileLinksResponse {
  readonly links: readonly FileLink[];
}
