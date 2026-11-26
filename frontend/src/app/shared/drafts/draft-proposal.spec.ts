import { DraftSource, draftSourceKey, draftSourceText } from '@shared/drafts/draft-proposal';

describe('draft-proposal', { tags: ['Drafts'] }, () => {
  it('draftSourceKey_ForEachSource_ReturnsItsOwnKey', () => {
    const keys = Object.values(DraftSource).map((source) => draftSourceKey(source));

    expect(keys).toEqual(['DRAFTS.SOURCES.INGEST', 'DRAFTS.SOURCES.CASE', 'DRAFTS.SOURCES.CHAT']);
  });

  it('draftSourceText_WithAnOwner_PutsTheNameAfterTheSource', () => {
    expect(draftSourceText('Chat', 'Ann')).toBe('Chat · Ann');
  });

  it('draftSourceText_WithNoOwner_ReturnsTheSourceOnly', () => {
    expect(draftSourceText('Chat', null)).toBe('Chat');
  });
});
