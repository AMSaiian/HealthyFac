import { themeSerializer } from './theme-serializer';

describe('themeSerializer', () => {
  it('read_WithDark_ReturnsDark', () => {
    const result = themeSerializer.read('dark');

    expect(result).toEqual({ isDark: true });
  });

  it.each(['garbage', null])('read_WithGarbageOrNull_ReturnsLight (%s)', (raw) => {
    const result = themeSerializer.read(raw);

    expect(result).toEqual({ isDark: false });
  });
});
