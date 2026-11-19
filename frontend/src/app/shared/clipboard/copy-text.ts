/** Copies a text to the clipboard. */
export function copyText(text: string): void {
  void navigator.clipboard.writeText(text);
}
