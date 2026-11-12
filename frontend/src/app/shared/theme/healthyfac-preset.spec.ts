import Lara from '@primeuix/themes/lara';

import { HEADER_CELL_SHADES, HealthyFacPreset } from './healthyfac-preset';

type Theme = 'light' | 'dark';

const LIGHT_DARK = /^light-dark\((\{[^}]+\}),\s*(\{[^}]+\})\)$/;

function buttonTextSecondaryHover(): string {
  const token = Lara.components?.button?.text?.secondary?.hoverBackground;
  if (token === undefined) {
    throw new Error('Lara has no button text secondary hover token');
  }
  return token;
}

function sortedHeaderCellBackground(): string {
  const token = HealthyFacPreset.components?.datatable?.headerCell?.selectedBackground;
  if (token === undefined) {
    throw new Error('Lara has no datatable header cell selected background token');
  }
  return token;
}

function shadesOf(token: string): Readonly<Record<Theme, string>> {
  const match = LIGHT_DARK.exec(token);
  if (match === null) {
    throw new Error(`Expected a light-dark token, found ${token}`);
  }
  return { light: match[1], dark: match[2] };
}

function duplicatedOrHoverShades(shades: readonly string[], buttonHover: string): string[] {
  return shades.filter((shade) => shade === buttonHover || shades.indexOf(shade) !== shades.lastIndexOf(shade));
}

describe('healthyfac preset', () => {
  it('headerCellShades_InBothThemes_DifferFromEachOtherAndFromTheFilterButtonHover', () => {
    const buttonHover = shadesOf(buttonTextSecondaryHover());
    const sorted = shadesOf(sortedHeaderCellBackground());
    const headerShades = [HEADER_CELL_SHADES.background, HEADER_CELL_SHADES.hoverBackground].map(shadesOf);

    for (const theme of ['light', 'dark'] as const) {
      const shades = headerShades.map((shade) => shade[theme]);

      expect(sorted[theme]).toBe(headerShades[0][theme]);
      expect(duplicatedOrHoverShades(shades, buttonHover[theme])).toEqual([]);
    }
  });

  it('headerCellHoverExtend_InBothThemes_EqualsTheDatatableHoverBackground', () => {
    const extendHover = (HealthyFacPreset.extend as { headerCell: { hoverBackground: string } }).headerCell
      .hoverBackground;
    const datatableHover = HealthyFacPreset.components?.datatable?.headerCell?.hoverBackground;

    expect(extendHover).toBe(datatableHover);
    expect(shadesOf(extendHover ?? '')).toEqual(shadesOf(datatableHover ?? ''));
  });

  it('headerCellShades_WithAShadeOfTheFilterButtonHover_ReportsIt', () => {
    const shades = ['{surface.100}', '{surface.200}', '{surface.300}'];

    expect(duplicatedOrHoverShades(shades, '{surface.100}')).toEqual(['{surface.100}']);
  });
});
