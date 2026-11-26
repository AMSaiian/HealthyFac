import { ATTACHMENT_EXTENSIONS, EMBEDDED_MAX_BYTES, IMAGE_EXTENSIONS } from '@shared/files/upload-limits';

import { checkUploadCandidate, FILE_ERROR_KEYS, fileBaseNameOf, fileExtensionOf } from './upload-check';

describe('fileExtensionOf', { tags: ['Platform'] }, () => {
  it.each([
    ['report.PDF', 'pdf'],
    ['archive.tar.gz', 'gz'],
    ['a..md', 'md'],
    ['C:\\docs.v1\\noext', null],
    ['report.', null],
    ['noext', null],
  ])('reads the extension of %s', (name, expected) => {
    expect(fileExtensionOf(name)).toBe(expected);
  });
});

describe('checkUploadCandidate', { tags: ['Platform'] }, () => {
  it('accepts an allowed file with the right size', () => {
    expect(checkUploadCandidate({ name: 'a.pdf', size: 10 }, ATTACHMENT_EXTENSIONS)).toBeNull();
  });

  it.each([
    ['a type that is not allowed', 'a.zip'],
    ['a name without an extension', 'noext'],
  ])('refuses %s', (_case, name) => {
    expect(checkUploadCandidate({ name, size: 10 }, ATTACHMENT_EXTENSIONS)).toBe(FILE_ERROR_KEYS.TypeNotAllowed);
  });

  it('refuses an empty file', () => {
    expect(checkUploadCandidate({ name: 'a.pdf', size: 0 }, ATTACHMENT_EXTENSIONS)).toBe(FILE_ERROR_KEYS.Empty);
  });

  it('refuses a file above the limit and accepts the limit itself', () => {
    expect(checkUploadCandidate({ name: 'a.pdf', size: 104_857_601 }, ATTACHMENT_EXTENSIONS)).toBe(
      FILE_ERROR_KEYS.TooLarge,
    );
    expect(checkUploadCandidate({ name: 'a.pdf', size: 104_857_600 }, ATTACHMENT_EXTENSIONS)).toBeNull();
  });

  it('applies the image limit and the image types for an embedded image', () => {
    expect(checkUploadCandidate({ name: 'a.pdf', size: 10 }, IMAGE_EXTENSIONS, EMBEDDED_MAX_BYTES)).toBe(
      FILE_ERROR_KEYS.TypeNotAllowed,
    );
    expect(
      checkUploadCandidate({ name: 'a.png', size: EMBEDDED_MAX_BYTES + 1 }, IMAGE_EXTENSIONS, EMBEDDED_MAX_BYTES),
    ).toBe(FILE_ERROR_KEYS.TooLarge);
  });
});

describe('fileBaseNameOf', { tags: ['Platform'] }, () => {
  it.each([
    ['report.PDF', 'report'],
    ['archive.tar.gz', 'archive.tar'],
    ['C:\\docs\\scheme.png', 'scheme'],
    ['noext', 'noext'],
    ['.png', '.png'],
  ])('reads the base name of %s', (name, expected) => {
    expect(fileBaseNameOf(name)).toBe(expected);
  });
});
