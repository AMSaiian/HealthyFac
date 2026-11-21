import { toStatusEditor } from './status-definitions-grid.store.utils';

describe('toStatusEditor', () => {
  it.each([
    ['WithoutCompose', null, null],
    ['WithTheNewName', 'status', { statusDefinitionId: null }],
    ['WithAStoredId', 'status:s1', { statusDefinitionId: 's1' }],
    ['WithAnotherName', 'phase:p1', null],
  ])('toStatusEditor_%s', (_name, compose, expected) => {
    const result = toStatusEditor(compose);

    expect(result).toEqual(expected);
  });
});
