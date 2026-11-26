/// <reference types="vite/client" />

const SPEC_SUFFIX_PATTERN = /\.spec\.ts$/;
const TOP_LEVEL_DESCRIBE_PATTERN = /^describe(?:\.each\([^)]*\))?\(.*$/gm;
const TAGS_OPTION_PATTERN = /\btags:\s*\[\s*'/;

function describeLines(source: string): string[] {
  return source.match(TOP_LEVEL_DESCRIBE_PATTERN) ?? [];
}

function specsWithoutTags(sources: Readonly<Record<string, string>>): string[] {
  return Object.entries(sources)
    .filter(([, source]) => {
      const lines = describeLines(source);
      return lines.length === 0 || lines.some((line) => !TAGS_OPTION_PATTERN.test(line));
    })
    .map(([path]) => path)
    .sort();
}

describe('spec tags', { tags: ['Conventions'] }, () => {
  const sources = import.meta.glob('./**/*.spec.ts', { query: '?raw', import: 'default', eager: true }) as Record<
    string,
    string
  >;

  it('specTags_EverySpecFile_TagsEachTopLevelDescribe', () => {
    expect(Object.keys(sources).every((path) => SPEC_SUFFIX_PATTERN.test(path))).toBe(true);
    expect(specsWithoutTags(sources)).toEqual([]);
  });
});
