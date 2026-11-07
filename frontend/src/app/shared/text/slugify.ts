import slugify from 'slugify';

/**
 * Turns a name into a metric key. Cyrillic letters become Latin letters (Ukrainian rules), and every other
 * character that is not a letter or a digit becomes an underscore.
 */
export function toKeySlug(name: string): string {
  return slugify(name, { locale: 'uk', lower: true, strict: true, replacement: '_' });
}
