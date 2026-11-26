import draftKinds from '@testdata/chat-draft-kinds.json';

import { ChatDraftKind } from '@shared/chat/chat';
import { CHAT_DRAFT_JUMP_KINDS } from '@shared/chat/chat-message/chat-message.utils';

describe('chat draft kinds', { tags: ['Chat'] }, () => {
  it('chatDraftKinds_AgainstTheSharedKindsFile_AreTheSameAsTheEnum', () => {
    expect([...Object.values(ChatDraftKind)].sort()).toEqual([...draftKinds].sort());
  });

  it('chatDraftJumpKinds_AgainstTheSharedKindsFile_HaveExactlyItsKeys', () => {
    expect([...CHAT_DRAFT_JUMP_KINDS].sort()).toEqual([...draftKinds].sort());
  });
});
