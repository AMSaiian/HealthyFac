import { ChatLinkKind } from '@shared/chat/chat';
import { CHAT_LINKS_MAX_COUNT } from '@shared/chat/chat-limits';
import {
  applyMention,
  canAddLink,
  caseLabel,
  linksForSend,
  mentionKinds,
  type MentionOption,
  mentionQueryAt,
} from '@shared/chat/mention-picker/mention-picker.utils';

function option(kind: ChatLinkKind, entityId: string, label = entityId): MentionOption {
  return { kind, entityId, label };
}

describe('mention-picker.utils', { tags: ['Chat'] }, () => {
  it('mentionKinds_WithAWorkerSet_OmitsTheEmployeeKind', () => {
    const held = new Set(['machines:read', 'cases:read', 'files:read', 'work-orders:read']);

    const kinds = mentionKinds((permission) => held.has(permission));

    expect(kinds).toEqual([
      ChatLinkKind.Machine,
      ChatLinkKind.MaintenanceCase,
      ChatLinkKind.File,
      ChatLinkKind.WorkOrder,
    ]);
  });

  it('mentionKinds_WithNoPermission_IsEmpty', () => {
    expect(mentionKinds(() => false)).toEqual([]);
  });

  it('caseLabel_WithAMachineAndADate_JoinsThem', () => {
    expect(caseLabel('Pump 3', '2026-11-26')).toBe('Pump 3 2026-11-26');
  });

  it('canAddLink_AtTheCap_ReturnsFalse', () => {
    const links = Array.from({ length: CHAT_LINKS_MAX_COUNT }, (_, index) => option(ChatLinkKind.Machine, `m${index}`));

    expect(canAddLink(links, option(ChatLinkKind.Machine, 'new'))).toBe(false);
  });

  it('canAddLink_BelowTheCap_ReturnsTrue', () => {
    const links = Array.from({ length: CHAT_LINKS_MAX_COUNT - 1 }, (_, index) =>
      option(ChatLinkKind.Machine, `m${index}`),
    );

    expect(canAddLink(links, option(ChatLinkKind.Machine, 'new'))).toBe(true);
  });

  it('canAddLink_WithTheSameKindAndEntity_ReturnsFalse', () => {
    const links = [option(ChatLinkKind.Machine, 'm1')];

    expect(canAddLink(links, option(ChatLinkKind.Machine, 'm1', 'other label'))).toBe(false);
  });

  it('canAddLink_WithTheSameEntityOfAnotherKind_ReturnsTrue', () => {
    const links = [option(ChatLinkKind.Machine, 'x1')];

    expect(canAddLink(links, option(ChatLinkKind.WorkOrder, 'x1'))).toBe(true);
  });

  it('mentionQueryAt_WithAnAtWordBeforeTheCaret_ReturnsItsStartAndText', () => {
    expect(mentionQueryAt('Check @pu', 9)).toEqual({ start: 6, query: 'pu' });
  });

  it('mentionQueryAt_WithAnAtInsideAWord_ReturnsNull', () => {
    expect(mentionQueryAt('mail me@pu', 10)).toBeNull();
  });

  it('mentionQueryAt_WithoutAnAt_ReturnsNull', () => {
    expect(mentionQueryAt('plain text', 5)).toBeNull();
  });

  it('applyMention_WithAQuery_ReplacesItWithTheLabelAndMovesTheCaret', () => {
    const result = applyMention('Check @pu now', { start: 6, query: 'pu' }, 9, 'Pump 3');

    expect(result).toEqual({ text: 'Check @Pump 3  now', caret: 14 });
  });

  it('linksForSend_WithADeletedMention_DropsItsLink', () => {
    const links = [option(ChatLinkKind.Machine, 'm1', 'Pump 3'), option(ChatLinkKind.Employee, 'e1', 'Ann')];

    const result = linksForSend('Check @Pump 3 please', links);

    expect(result).toEqual([{ kind: ChatLinkKind.Machine, entityId: 'm1', position: 6 }]);
  });

  it('mentionQueryAt_WithATrailingSpaceAfterTheText_ReturnsNull', () => {
    expect(mentionQueryAt('Check @Pump 3 ', 14)).toBeNull();
  });

  it('mentionQueryAt_WithWordsAfterAPickedMention_ReturnsNull', () => {
    expect(mentionQueryAt('Check @Pump 3 p', 15, ['Pump 3'])).toBeNull();
  });

  it('mentionQueryAt_WithANewAtAfterAPickedMention_ReturnsTheNewQuery', () => {
    expect(mentionQueryAt('Check @Pump 3 and @ba', 21, ['Pump 3'])).toEqual({ start: 18, query: 'ba' });
  });

  it('linksForSend_WithTwoLabelsThatShareTheStart_GivesEachItsOwnPosition', () => {
    const machine = { kind: ChatLinkKind.Machine, entityId: 'm', label: 'Pump 3' };
    const found = { kind: ChatLinkKind.MaintenanceCase, entityId: 'c', label: 'Pump 3 2026-11-26' };

    const links = linksForSend('See @Pump 3 2026-11-26 and @Pump 3.', [machine, found]);

    expect(links).toEqual([
      { kind: ChatLinkKind.Machine, entityId: 'm', position: 27 },
      { kind: ChatLinkKind.MaintenanceCase, entityId: 'c', position: 4 },
    ]);
  });

  it('linksForSend_WithTheShorterMentionDeleted_DropsItsLink', () => {
    const machine = { kind: ChatLinkKind.Machine, entityId: 'm', label: 'Pump 3' };
    const found = { kind: ChatLinkKind.MaintenanceCase, entityId: 'c', label: 'Pump 3 2026-11-26' };

    const links = linksForSend('See @Pump 3 2026-11-26', [machine, found]);

    expect(links).toEqual([{ kind: ChatLinkKind.MaintenanceCase, entityId: 'c', position: 4 }]);
  });

  it('mentionQueryAt_WithANewMentionThatStartsWithAPickedLabel_ReturnsTheNewQuery', () => {
    expect(mentionQueryAt('Check @Pump 3 and @Pump 30', 26, ['Pump 3'])).toEqual({ start: 18, query: 'Pump 30' });
  });
});
