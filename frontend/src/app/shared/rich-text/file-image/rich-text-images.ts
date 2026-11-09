import { fileBaseNameOf } from '@shared/files/upload-check';
import { ALT_ATTRIBUTE, FILE_ID_ATTRIBUTE } from '@shared/rich-text/file-image/file-image-blot';
import { IMAGE_ALT_MAX_LENGTH } from '@shared/rich-text/rich-text-limits';

export const MISSING_IMAGE_CLASS = 'hf-missing-image';
const IMAGE_SELECTOR = `img[${FILE_ID_ATTRIBUTE}]`;
const HTML_MIME = 'text/html';

function parse(html: string): Document {
  return new DOMParser().parseFromString(html, HTML_MIME);
}

/** Builds the `alt` of an uploaded image: the base name of the file, cut to the limit of the backend. */
export function imageAltOf(fileName: string): string {
  return fileBaseNameOf(fileName).slice(0, IMAGE_ALT_MAX_LENGTH);
}

/** Lists the distinct file IDs of the images in a rich text, in document order. */
export function collectFileIds(html: string | null): readonly string[] {
  if (html === null || html === '') {
    return [];
  }
  const ids = Array.from(parse(html).querySelectorAll(IMAGE_SELECTOR), (image) =>
    image.getAttribute(FILE_ID_ATTRIBUTE),
  );
  return [...new Set(ids.filter((id): id is string => id !== null && id !== ''))];
}

/**
 * Builds the HTML for display: each image with a link gets its `src` and loses the file ID; an image without a link
 * becomes a marked placeholder that shows its alt text, or `missingText` when the alt is empty.
 */
export function toDisplayHtml(html: string, links: ReadonlyMap<string, string>, missingText: string): string {
  const document = parse(html);
  for (const image of Array.from(document.querySelectorAll(IMAGE_SELECTOR))) {
    const url = links.get(image.getAttribute(FILE_ID_ATTRIBUTE) ?? '');
    image.removeAttribute(FILE_ID_ATTRIBUTE);
    if (url === undefined) {
      image.classList.add(MISSING_IMAGE_CLASS);
      if ((image.getAttribute(ALT_ATTRIBUTE) ?? '') === '') {
        image.setAttribute(ALT_ATTRIBUTE, missingText);
      }
    } else {
      image.setAttribute('src', url);
    }
    if (!image.hasAttribute(ALT_ATTRIBUTE)) {
      image.setAttribute(ALT_ATTRIBUTE, '');
    }
  }
  return document.body.innerHTML;
}
