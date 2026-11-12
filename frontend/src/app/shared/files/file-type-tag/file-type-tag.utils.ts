import { ATTACHMENT_EXTENSIONS } from '@shared/files/upload-limits';

/** The CSS class of each attachment type. `styles.css` sets the color token of each class. */
export const FILE_TYPE_CLASSES: Readonly<Partial<Record<string, string>>> = Object.fromEntries(
  ATTACHMENT_EXTENSIONS.map((extension) => [extension, `hf-file-type-${extension}`]),
);

/** Returns the class of an attachment type, or `undefined` for an extension that is not an attachment type. */
export function fileTypeClass(extension: string): string | undefined {
  return FILE_TYPE_CLASSES[extension.toLowerCase()];
}
