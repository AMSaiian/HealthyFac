import Lara from '@primeuix/themes/lara';

import { ATTACHMENT_EXTENSIONS } from '@shared/files/upload-limits';
import { FILE_TYPE_TOKENS } from '@shared/theme/healthyfac-preset';

const WCAG_AA_TEXT_CONTRAST = 4.5;
const PALETTE_TOKEN = /^\{(\w+)\.(\d+)\}$/;

function channelLuminance(channel: number): number {
  const srgb = channel / 255;
  return srgb <= 0.03928 ? srgb / 12.92 : ((srgb + 0.055) / 1.055) ** 2.4;
}

function relativeLuminance(hex: string): number {
  const rgb = parseInt(hex.slice(1), 16);
  return (
    0.2126 * channelLuminance((rgb >> 16) & 0xff) +
    0.7152 * channelLuminance((rgb >> 8) & 0xff) +
    0.0722 * channelLuminance(rgb & 0xff)
  );
}

function contrastWithWhite(hex: string): number {
  return (1 + 0.05) / (relativeLuminance(hex) + 0.05);
}

function paletteHexOf(token: string): string {
  const match = PALETTE_TOKEN.exec(token);
  if (match === null) {
    throw new Error(`Expected a palette token, found ${token}`);
  }
  const [, group, shade] = match;
  const scale: object | undefined = Reflect.get(Lara.primitive ?? {}, group);
  const hex: unknown = Reflect.get(scale ?? {}, shade);
  if (typeof hex !== 'string') {
    throw new Error(`Lara has no palette color for ${token}`);
  }
  return hex;
}

function tokensBelowContrast(tokens: readonly string[]): string[] {
  return tokens.filter((token) => contrastWithWhite(paletteHexOf(token)) < WCAG_AA_TEXT_CONTRAST);
}

describe('fileTypeColors', () => {
  it('fileTypeColors_EveryAllowedExtension_HasOneDistinctColor', () => {
    const tokens = Object.values(FILE_TYPE_TOKENS);

    expect(Object.keys(FILE_TYPE_TOKENS).sort()).toEqual([...ATTACHMENT_EXTENSIONS].sort());
    expect(new Set(tokens).size).toBe(tokens.length);
  });

  it('fileTypeColors_WithWhiteText_PassTheContrast', () => {
    const tokens = Object.values(FILE_TYPE_TOKENS);

    const belowContrast = tokensBelowContrast(tokens);

    expect(belowContrast).toEqual([]);
  });

  it('tokensBelowContrast_WithAmber600_ReportsIt', () => {
    const belowContrast = tokensBelowContrast(['{amber.600}']);

    expect(belowContrast).toEqual(['{amber.600}']);
  });
});
