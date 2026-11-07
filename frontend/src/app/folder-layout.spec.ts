/// <reference types="vite/client" />

const FORM_SCHEMA_SUFFIX = '-form-schema.ts';
const UTILS_SUFFIX = '.utils.ts';
const SPEC_SUFFIX = '.spec.ts';
const SOURCE_SUFFIX = '.ts';
const FORM_FOLDER_SUFFIX = '-form';
const PATH_SEPARATOR = '/';

function fileName(path: string): string {
  return path.slice(path.lastIndexOf(PATH_SEPARATOR) + 1);
}

function folderName(path: string): string {
  const parts = path.split(PATH_SEPARATOR);
  return parts[parts.length - 2];
}

function isSource(path: string): boolean {
  return !path.endsWith(SPEC_SUFFIX);
}

function formSchemasOutsideTheirFolder(files: readonly string[]): string[] {
  return files
    .filter((path) => isSource(path) && path.endsWith(FORM_SCHEMA_SUFFIX))
    .filter((path) => {
      const name = fileName(path).slice(0, -FORM_SCHEMA_SUFFIX.length);
      return folderName(path) !== `${name}${FORM_FOLDER_SUFFIX}`;
    });
}

function utilsWithoutSourceFile(files: readonly string[]): string[] {
  const all = new Set(files);
  return files
    .filter((path) => isSource(path) && path.endsWith(UTILS_SUFFIX))
    .filter((path) => !all.has(`${path.slice(0, -UTILS_SUFFIX.length)}${SOURCE_SUFFIX}`));
}

describe('folder layout', () => {
  const files = Object.keys(import.meta.glob('./**/*.ts'));

  it('folderLayout_FormSchemas_LieInTheirFormFolder', () => {
    expect(formSchemasOutsideTheirFolder(files)).toEqual([]);
  });

  it('folderLayout_UtilsFiles_HaveTheirSourceFileNextToThem', () => {
    expect(utilsWithoutSourceFile(files)).toEqual([]);
  });
});
