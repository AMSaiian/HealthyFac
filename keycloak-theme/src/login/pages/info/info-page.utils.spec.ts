import { describe, expect, it } from 'vitest';

import { requiredActionKeys, summaryToShow, titleOf } from './info-page.utils';

describe('summaryToShow', () => {
  it('summaryToShow_WithTheTitleAsSummary_GivesNull', () => {
    expect(summaryToShow('You need to change your password', 'You need to change your <b>password</b>')).toBeNull();
  });

  it('summaryToShow_WithAnotherSummary_GivesTheSummary', () => {
    const summary = 'Your account has been updated.';

    expect(summaryToShow('Account updated', summary)).toBe(summary);
  });
});

describe('requiredActionKeys', () => {
  it('requiredActionKeys_WithActions_PrefixesEachOne', () => {
    expect(requiredActionKeys(['UPDATE_PASSWORD', 'VERIFY_EMAIL'])).toEqual([
      'requiredAction.UPDATE_PASSWORD',
      'requiredAction.VERIFY_EMAIL',
    ]);
  });

  it('requiredActionKeys_WithoutActions_GivesAnEmptyList', () => {
    expect(requiredActionKeys(undefined)).toEqual([]);
  });
});

describe('titleOf', () => {
  const translate = (key: string): string => (key === 'accountUpdatedTitle' ? 'Account updated' : key);

  it('titleOf_WithAKnownHeaderKey_GivesTheTranslation', () => {
    expect(titleOf('accountUpdatedTitle', 'Your account has been updated', translate)).toBe('Account updated');
  });

  it('titleOf_WithAnUnknownHeaderKey_FallsBackToTheSummary', () => {
    expect(titleOf('missingTitle', 'Your account has been updated', translate)).toBe('Your account has been updated');
  });

  it('titleOf_WithoutAHeader_GivesTheSummary', () => {
    expect(titleOf(undefined, 'Your account has been updated', translate)).toBe('Your account has been updated');
  });
});
