const BYTES_PER_STEP = 1024;
const SIZE_UNITS: readonly string[] = ['B', 'KB', 'MB', 'GB'];
const DECIMALS = 1;

/** Shows a size in bytes as B, KB, MB, or GB with one decimal. The base is 1024. */
export function formatFileSize(bytes: number): string {
  if (bytes < BYTES_PER_STEP) {
    return `${bytes} ${SIZE_UNITS[0]}`;
  }
  let value = bytes;
  let unitIndex = 0;
  while (value >= BYTES_PER_STEP && unitIndex < SIZE_UNITS.length - 1) {
    value /= BYTES_PER_STEP;
    unitIndex += 1;
  }
  return `${value.toFixed(DECIMALS)} ${SIZE_UNITS[unitIndex]}`;
}
