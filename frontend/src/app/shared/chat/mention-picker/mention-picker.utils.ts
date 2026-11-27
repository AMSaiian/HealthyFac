import { type ChatLinkInput, ChatLinkKind } from '@shared/chat/chat';
import { CHAT_LINKS_MAX_COUNT } from '@shared/chat/chat-limits';
import { Permission } from '@shared/security/permissions';

/** One entity that a mention can name: the kind, the ID, and the label that the text and the chip show. */
export interface MentionOption {
  readonly kind: ChatLinkKind;
  readonly entityId: string;
  readonly label: string;
}

/** The options of one kind, found for the typed text. */
export interface MentionGroup {
  readonly kind: ChatLinkKind;
  readonly options: readonly MentionOption[];
}

/** The `@text` before the caret: where the `@` stands and the text after it. */
export interface MentionQuery {
  readonly start: number;
  readonly query: string;
}

const MENTION_KIND_PERMISSIONS: readonly (readonly [ChatLinkKind, Permission])[] = [
  [ChatLinkKind.Machine, Permission.MachinesRead],
  [ChatLinkKind.MaintenanceCase, Permission.CasesRead],
  [ChatLinkKind.File, Permission.FilesRead],
  [ChatLinkKind.Employee, Permission.EmployeesRead],
  [ChatLinkKind.WorkOrder, Permission.WorkOrdersRead],
];

/** The kinds that a user may mention: the kinds whose read permission the user holds, in a fixed order. */
export function mentionKinds(hasPermission: (permission: Permission) => boolean): readonly ChatLinkKind[] {
  return MENTION_KIND_PERMISSIONS.filter(([, permission]) => hasPermission(permission)).map(([kind]) => kind);
}

/** The label of a case: the machine name and the opening date. */
export function caseLabel(machineName: string, openedAtText: string): string {
  return `${machineName} ${openedAtText}`;
}

/** Tells whether the option may become a link: under the cap and not yet linked. */
export function canAddLink(links: readonly MentionOption[], option: MentionOption): boolean {
  return (
    links.length < CHAT_LINKS_MAX_COUNT &&
    !links.some((link) => link.kind === option.kind && link.entityId === option.entityId)
  );
}

const WORD_CHARACTER = /[\p{L}\p{N}_]/u;

/** Tells whether the `@` at `start` begins the whole `@label` of a picked link, with a word end behind the label. */
function isPickedMention(before: string, start: number, label: string): boolean {
  const end = start + label.length + 1;
  return before.startsWith(`@${label}`, start) && end < before.length && !WORD_CHARACTER.test(before[end]);
}

/**
 * The `@text` that ends at the caret, or `null` when the caret is not behind an `@` word. An `@` that starts the
 * `@label` of a picked link is not a new mention, so the words after a picked mention open no picker.
 */
export function mentionQueryAt(text: string, caret: number, linkLabels: readonly string[] = []): MentionQuery | null {
  const before = text.slice(0, caret);
  const start = before.lastIndexOf('@');
  if (start === -1 || linkLabels.some((label) => isPickedMention(before, start, label))) {
    return null;
  }
  if (start > 0 && !/\s/.test(before[start - 1])) {
    return null;
  }
  const query = before.slice(start + 1);
  return /\s$/.test(query) || /[\r\n]/.test(query) ? null : { start, query };
}

/** Replaces the `@text` with `@label ` and gives the new text and caret. */
export function applyMention(
  text: string,
  mention: MentionQuery,
  caret: number,
  label: string,
): { readonly text: string; readonly caret: number } {
  const inserted = `@${label} `;
  return {
    text: `${text.slice(0, mention.start)}${inserted}${text.slice(caret)}`,
    caret: mention.start + inserted.length,
  };
}

/** Finds the first `@label` that ends at a word end and starts outside the taken ranges. */
function freePosition(text: string, label: string, taken: readonly (readonly [number, number])[]): number {
  const mention = `@${label}`;
  for (let at = text.indexOf(mention); at !== -1; at = text.indexOf(mention, at + 1)) {
    const end = at + mention.length;
    const isWordEnd = end >= text.length || !WORD_CHARACTER.test(text[end]);
    const isFree = taken.every(([from, to]) => end <= from || at >= to);
    if (isWordEnd && isFree) {
      return at;
    }
  }
  return -1;
}

/**
 * The links that go with the text: each link takes the position of its own `@label` in the final text, and a link whose
 * `@label` the user deleted is dropped. The longest labels match first, and a position serves one link only.
 */
export function linksForSend(text: string, links: readonly MentionOption[]): ChatLinkInput[] {
  const taken: (readonly [number, number])[] = [];
  const positions = new Map<MentionOption, number>();
  for (const link of [...links].sort((a, b) => b.label.length - a.label.length)) {
    const position = freePosition(text, link.label, taken);
    if (position !== -1) {
      taken.push([position, position + link.label.length + 1]);
      positions.set(link, position);
    }
  }
  return links.flatMap((link) => {
    const position = positions.get(link);
    return position === undefined ? [] : [{ kind: link.kind, entityId: link.entityId, position }];
  });
}
