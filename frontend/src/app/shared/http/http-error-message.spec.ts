import { httpErrorMessageKey } from './http-error-message';

describe('httpErrorMessageKey', () => {
  it.each([
    [0, 'ERRORS.HTTP.NETWORK'],
    [400, 'ERRORS.HTTP.BAD_REQUEST'],
    [401, 'ERRORS.HTTP.UNAUTHORIZED'],
    [403, 'ERRORS.HTTP.FORBIDDEN'],
    [404, 'ERRORS.HTTP.NOT_FOUND'],
    [409, 'ERRORS.HTTP.CONFLICT'],
    [429, 'ERRORS.HTTP.TOO_MANY_REQUESTS'],
  ])('httpErrorMessageKey_WithMappedStatuses_ReturnsKeys (%i)', (status, key) => {
    const result = httpErrorMessageKey(status);

    expect(result).toBe(key);
  });

  it.each([500, 599])('httpErrorMessageKey_WithServerRangeEdges_ReturnsServer (%i)', (status) => {
    const result = httpErrorMessageKey(status);

    expect(result).toBe('ERRORS.HTTP.SERVER');
  });

  it.each([418, 600])('httpErrorMessageKey_WithOtherStatus_ReturnsUnknown (%i)', (status) => {
    const result = httpErrorMessageKey(status);

    expect(result).toBe('ERRORS.HTTP.UNKNOWN');
  });
});
