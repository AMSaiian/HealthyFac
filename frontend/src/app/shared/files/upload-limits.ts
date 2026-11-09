/** The limits of a file upload. They mirror `FileValidationConstants` and `AllowedFileTypes` of the Document service. */
export const ATTACHMENT_MAX_BYTES = 104_857_600;
export const EMBEDDED_MAX_BYTES = 5_242_880;
export const ATTACHMENT_EXTENSIONS: readonly string[] = [
  'png',
  'jpg',
  'jpeg',
  'gif',
  'webp',
  'pdf',
  'docx',
  'xlsx',
  'pptx',
  'txt',
  'csv',
  'md',
];
export const IMAGE_EXTENSIONS: readonly string[] = ['png', 'jpg', 'jpeg', 'gif', 'webp'];
/** The media types of the image extensions. The editor gives them to the file picker of its image button. */
export const IMAGE_MIME_TYPES: readonly string[] = ['image/png', 'image/jpeg', 'image/gif', 'image/webp'];
