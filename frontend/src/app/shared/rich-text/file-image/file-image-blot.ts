import Quill from 'quill';

export const FILE_IMAGE_BLOT_NAME = 'fileImage';
export const FILE_ID_ATTRIBUTE = 'data-file-id';
export const ALT_ATTRIBUTE = 'alt';
export const PENDING_CLASS = 'hf-image-pending';

/** The value of a file image: the encrypted file ID, and the alt text. An empty ID marks an upload in progress. */
export interface FileImageValue {
  readonly fileId: string;
  readonly alt: string;
}

interface EmbedBlotConstructor {
  new (scroll: unknown, domNode: Node): { readonly domNode: HTMLElement };
  create(value?: unknown): Node;
}

const { EmbedBlot } = Quill.import('parchment') as unknown as { EmbedBlot: EmbedBlotConstructor };

function escapeAttribute(value: string): string {
  return value.replaceAll('&', '&amp;').replaceAll('"', '&quot;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
}

/**
 * An image of the Document service in a rich text. The stored form is `<img data-file-id="..." alt="...">` and never
 * holds a `src`: the link is short-lived, so the editor shows it as a DOM-only attribute. `value` and `html` ignore
 * `src`, so setting it never changes the value that the form saves.
 */
export class FileImageBlot extends EmbedBlot {
  static blotName = FILE_IMAGE_BLOT_NAME;
  static tagName = 'IMG';

  static override create(value: FileImageValue): Node {
    const node = super.create() as HTMLElement;
    node.setAttribute(FILE_ID_ATTRIBUTE, value.fileId);
    node.setAttribute(ALT_ATTRIBUTE, value.alt);
    if (value.fileId === '') {
      node.classList.add(PENDING_CLASS);
    }
    return node;
  }

  static value(node: HTMLElement): FileImageValue {
    return { fileId: node.getAttribute(FILE_ID_ATTRIBUTE) ?? '', alt: node.getAttribute(ALT_ATTRIBUTE) ?? '' };
  }

  /** Writes the stored form. An image that is still uploading has no stored form yet. */
  html(): string {
    const { fileId, alt } = FileImageBlot.value(this.domNode);
    return fileId === ''
      ? ''
      : `<img ${FILE_ID_ATTRIBUTE}="${escapeAttribute(fileId)}" ${ALT_ATTRIBUTE}="${escapeAttribute(alt)}">`;
  }
}

const Delta = Quill.import('delta') as new () => object;

/**
 * A clipboard matcher for `<img>`. It keeps the image of the Document service and drops every other image, so a pasted
 * external image never enters the editor.
 */
export function keepFileImagesOnly(node: Node, delta: object): object {
  return (node as Element).hasAttribute(FILE_ID_ATTRIBUTE) ? delta : new Delta();
}

let isRegistered = false;

/** Registers the blot once. Call it before the first editor is created. */
export function registerFileImageBlot(): void {
  if (!isRegistered) {
    Quill.register(FileImageBlot as never, true);
    isRegistered = true;
  }
}
