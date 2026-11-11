/// <reference types="vite/client" />

const TEMPLATES = import.meta.glob('./board-page.html', { query: '?raw', import: 'default', eager: true }) as Record<
  string,
  string
>;
const TEMPLATE = TEMPLATES['./board-page.html'];
const GROUP_THEN_COLUMN = /cdkDropListGroup[^>]*>\s*@for[^{]*\{\s*<hf-board-column/;

describe('board page wiring', () => {
  it('template_ColumnsRow_IsOneDropListGroup', () => {
    expect(TEMPLATE).toMatch(GROUP_THEN_COLUMN);
  });

  it('template_HeaderDropList_StaysOutsideTheColumnGroup', () => {
    const headerList = TEMPLATE.indexOf('cdkDropListOrientation="horizontal"');
    const group = TEMPLATE.indexOf('cdkDropListGroup');

    expect(headerList).toBeGreaterThan(-1);
    expect(headerList).toBeLessThan(group);
  });
});
