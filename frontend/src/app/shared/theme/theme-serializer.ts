const DARK_VALUE = 'dark';
const LIGHT_VALUE = 'light';

export const themeSerializer = {
  read: (raw: string | null): { isDark: boolean } => ({ isDark: raw === DARK_VALUE }),
  write: (state: { isDark: boolean }): string => (state.isDark ? DARK_VALUE : LIGHT_VALUE),
};
