/// <reference types="vite/client" />

type Sources = Readonly<Record<string, string>>;

const PATH_SEPARATOR = '/';
const STORE_SUFFIX = '.store.ts';
const SLICE_SUFFIX = '.slice.ts';
const SPEC_SUFFIX = '.spec.ts';
const RELATIVE_PREFIX = './';
const SLICE_IMPORT = /from\s+['"]([^'"]+\.slice)['"]/g;
const OWN_STATE_CALL = /withState[<(]|withLinkedState[<(]/;
const TOAST_OR_CONFIRM_CALL = /_messages\.add\(|_confirmation\.confirm\(/;
const TOAST_OR_CONFIRM_OWNERS: readonly string[] = [
  'shared/toasts/',
  'shared/confirm/',
  'shared/rich-text/rich-text-editor',
  'shared/errors/',
];

function relativePath(path: string): string {
  return path.startsWith(RELATIVE_PREFIX) ? path.slice(RELATIVE_PREFIX.length) : path;
}

function fileName(path: string): string {
  return path.slice(path.lastIndexOf(PATH_SEPARATOR) + 1);
}

function folderOf(path: string): string {
  return path.slice(0, path.lastIndexOf(PATH_SEPARATOR));
}

function prefixOf(path: string): string {
  return fileName(path).split('.')[0];
}

function isSource(path: string): boolean {
  return path.endsWith('.ts') && !path.endsWith(SPEC_SUFFIX);
}

function slicesWithoutStore(paths: readonly string[]): string[] {
  const all = new Set(paths);
  return paths
    .filter((path) => isSource(path) && path.endsWith(SLICE_SUFFIX))
    .filter((path) => !all.has(`${folderOf(path)}${PATH_SEPARATOR}${prefixOf(path)}${STORE_SUFFIX}`));
}

function slicesWithoutOwnState(sources: Sources): string[] {
  return Object.keys(sources).filter(
    (path) => isSource(path) && path.endsWith(SLICE_SUFFIX) && !OWN_STATE_CALL.test(sources[path]),
  );
}

const STATE_CALL = /withLinkedState|withState/g;
const STATE_CALL_HEAD = /^(?:<[^>]*>)?\(\s*(?:\([^)]*\)\s*=>\s*)?\(?\s*\{/;
const NESTED_GROUP = /\{[^{}]*\}|\([^()]*\)|\[[^[\]]*\]/g;
const FIRST_KEY = /^\s*([A-Za-z_$][\w$]*)\s*(?::|$)/;
const STATUS_FLAG = /^(is|has)/;

function objectLiteralAfter(text: string): string {
  const open = text.indexOf('{');
  let depth = 0;
  for (let index = open; index < text.length; index++) {
    if (text[index] === '{') {
      depth++;
    } else if (text[index] === '}') {
      depth--;
      if (depth === 0) {
        return text.slice(open + 1, index);
      }
    }
  }
  return '';
}

function removeNestedGroups(text: string): string {
  let result = text;
  let previous: string;
  do {
    previous = result;
    result = result.replace(NESTED_GROUP, '');
  } while (result !== previous);
  return result;
}

function stateKeysOf(source: string): string[] {
  return [...source.matchAll(STATE_CALL)].flatMap((match) => {
    const rest = source.slice((match.index ?? 0) + match[0].length);
    if (!STATE_CALL_HEAD.test(rest)) {
      return [];
    }
    return removeNestedGroups(objectLiteralAfter(rest))
      .split(',')
      .map((segment) => FIRST_KEY.exec(segment)?.[1])
      .filter((key): key is string => key !== undefined);
  });
}

function slicesOwningOnlyStatusFlags(sources: Sources): string[] {
  return Object.keys(sources).filter((path) => {
    if (!isSource(path) || !path.endsWith(SLICE_SUFFIX)) {
      return false;
    }
    const keys = stateKeysOf(sources[path]);
    return keys.length > 0 && keys.every((key) => STATUS_FLAG.test(key));
  });
}

const DEVTOOLS_CALL_LAST = /withStoreDevtools\('([^']+)'\),?\s*\)\s*;/;

function storesWithoutDevtools(sources: Sources): string[] {
  return Object.keys(sources).filter((path) => {
    if (!isSource(path) || !path.endsWith(STORE_SUFFIX)) {
      return false;
    }
    const call = DEVTOOLS_CALL_LAST.exec(sources[path]);
    return call === null || call[1] !== fileName(path).slice(0, -STORE_SUFFIX.length);
  });
}

const STORE_FOLDER_SUFFIX = '-store';

function folderNameOf(path: string): string {
  return fileName(folderOf(path));
}

function slicedStoresOutsideTheirFolder(paths: readonly string[]): string[] {
  return paths.filter(
    (path) =>
      isSource(path) && path.endsWith(SLICE_SUFFIX) && folderNameOf(path) !== `${prefixOf(path)}${STORE_FOLDER_SUFFIX}`,
  );
}

function storeFoldersWithoutStoreOrSlice(paths: readonly string[]): string[] {
  const sources = paths.filter(isSource);
  return [...new Set(sources.map(folderOf))]
    .filter((folder) => folderNameOf(folder).endsWith(STORE_FOLDER_SUFFIX))
    .filter((folder) => {
      const name = folderNameOf(folder).slice(0, -STORE_FOLDER_SUFFIX.length);
      const hasStore = sources.includes(`${folder}${PATH_SEPARATOR}${name}${STORE_SUFFIX}`);
      const hasSlice = sources.some((path) => folderOf(path) === folder && path.endsWith(SLICE_SUFFIX));
      return !hasStore || !hasSlice;
    });
}

function slicedStoreViolations(paths: readonly string[]): string[] {
  return [...slicedStoresOutsideTheirFolder(paths), ...storeFoldersWithoutStoreOrSlice(paths)];
}

function isStoreOfSlice(importerPath: string, specifier: string): boolean {
  const sliceName = specifier.slice(specifier.lastIndexOf(PATH_SEPARATOR) + 1);
  return fileName(importerPath) === `${sliceName.split('.')[0]}${STORE_SUFFIX}`;
}

function sliceImportsOutsideTheirStore(sources: Sources): string[] {
  return Object.keys(sources)
    .filter(isSource)
    .flatMap((path) =>
      [...sources[path].matchAll(SLICE_IMPORT)]
        .map((match) => match[1])
        .filter((specifier) => !isStoreOfSlice(path, specifier))
        .map((specifier) => `${path} -> ${specifier}`),
    );
}

function toastOrConfirmCallsOutsideOwners(sources: Sources): string[] {
  return Object.keys(sources)
    .filter((path) => isSource(path) && TOAST_OR_CONFIRM_CALL.test(sources[path]))
    .filter((path) => !TOAST_OR_CONFIRM_OWNERS.some((owner) => path.startsWith(owner)));
}

function readSources(): Sources {
  const raw: Readonly<Record<string, string>> = import.meta.glob('./**/*.ts', {
    query: '?raw',
    import: 'default',
    eager: true,
  });
  return Object.fromEntries(Object.entries(raw).map(([path, text]) => [relativePath(path), text]));
}

describe('store slices', () => {
  const sources = readSources();
  const paths = Object.keys(sources);

  it('storeSlices_Slices_CallWithStateOrWithLinkedState', () => {
    expect(slicesWithoutOwnState(sources)).toEqual([]);
  });

  it('storeSlices_Slices_HaveTheirStoreNextToThem', () => {
    expect(slicesWithoutStore(paths)).toEqual([]);
  });

  it('storeSlices_Slices_AreImportedOnlyByTheirStore', () => {
    expect(sliceImportsOutsideTheirStore(sources)).toEqual([]);
  });

  it('storeSlices_ToastAndConfirmCalls_StayInTheirOwnerFolders', () => {
    expect(toastOrConfirmCallsOutsideOwners(sources)).toEqual([]);
  });

  it('everySlice_OwnsMoreThanStatusFlags', () => {
    expect(slicesOwningOnlyStatusFlags(sources)).toEqual([]);
  });

  it('everyStore_ComposesStoreDevtools', () => {
    expect(storesWithoutDevtools(sources)).toEqual([]);
  });

  it('everySlicedStore_SitsInItsStoreFolder', () => {
    expect(slicedStoreViolations(paths)).toEqual([]);
  });
});

describe('store slices checks', () => {
  it('storeSlices_SlicesWithoutOwnState_FlagsASliceWithoutWithState', () => {
    const sources = {
      'board/board.filter.slice.ts': `export function withBoardFilter() { return signalStoreFeature(); }`,
    };

    expect(slicesWithoutOwnState(sources)).toEqual(['board/board.filter.slice.ts']);
  });

  it('storeSlices_SlicesWithoutOwnState_AcceptsASliceWithWithStateCall', () => {
    const sources = {
      'board/board.filter.slice.ts': `signalStoreFeature(withState<FilterState>({ titleDraft: '' }));`,
      'board/board.items.slice.ts': `signalStoreFeature(withLinkedState((store) => ({ items: () => [] })));`,
    };

    expect(slicesWithoutOwnState(sources)).toEqual([]);
  });

  it('storeSlices_SlicesWithoutStore_FlagsASliceWithoutItsStore', () => {
    expect(slicesWithoutStore(['board/board.filter.slice.ts'])).toEqual(['board/board.filter.slice.ts']);
  });

  it('storeSlices_SlicesWithoutStore_AcceptsASliceWithItsStore', () => {
    expect(slicesWithoutStore(['board/board.filter.slice.ts', 'board/board.store.ts'])).toEqual([]);
  });

  it('storeSlices_SliceImports_FlagsAStoreThatIsNotTheOwner', () => {
    const sources = {
      'board/board.store.ts': `import { x } from './board.filter.slice';`,
      'files/files-grid.store.ts': `import { y } from '@board/board.filter.slice';`,
    };

    expect(sliceImportsOutsideTheirStore(sources)).toEqual(['files/files-grid.store.ts -> @board/board.filter.slice']);
  });

  it('storeSlices_ToastCalls_FlagsACallOutsideTheOwnerFolders', () => {
    const sources = {
      'work-orders/board/board.store.ts': `this._messages.add({ severity: 'error' });`,
      'shared/toasts/with-toasts.ts': `this._messages.add({ severity: 'error' });`,
    };

    expect(toastOrConfirmCallsOutsideOwners(sources)).toEqual(['work-orders/board/board.store.ts']);
  });

  it('everySlice_OwnsMoreThanStatusFlags_FlagsASliceWithOnlyFlags', () => {
    const sources = {
      'machines/metrics/metrics-page.example.slice.ts': `export function withMetricsPageExample() {
  return signalStoreFeature(withState({ isExampleOpen: false }));
}`,
    };

    expect(slicesOwningOnlyStatusFlags(sources)).toEqual(['machines/metrics/metrics-page.example.slice.ts']);
  });

  it('everySlice_OwnsMoreThanStatusFlags_AcceptsASliceWithAnotherField', () => {
    const sources = {
      'employees/employee-actions.action.slice.ts': `export function withEmployeeActionsAction() {
  return signalStoreFeature(withState<ActionState>({ action: null, isSaving: false }));
}`,
    };

    expect(slicesOwningOnlyStatusFlags(sources)).toEqual([]);
  });

  it('everyStore_ComposesStoreDevtools_FlagsAStoreWithoutIt', () => {
    const sources = {
      'machines/list/machines-grid.store.ts': `export const MachinesGridStore = signalStore(withToasts());`,
    };

    expect(storesWithoutDevtools(sources)).toEqual(['machines/list/machines-grid.store.ts']);
  });

  it('everyStore_ComposesStoreDevtools_FlagsACallThatIsNotLast', () => {
    const sources = {
      'machines/list/machines-grid.store.ts': `export const MachinesGridStore = signalStore(withStoreDevtools('machines-grid'), withToasts());`,
    };

    expect(storesWithoutDevtools(sources)).toEqual(['machines/list/machines-grid.store.ts']);
  });

  it('everyStore_ComposesStoreDevtools_FlagsACallWithAnotherName', () => {
    const sources = {
      'machines/list/machines-grid.store.ts': `export const MachinesGridStore = signalStore(withToasts(), withStoreDevtools('machines'));`,
    };

    expect(storesWithoutDevtools(sources)).toEqual(['machines/list/machines-grid.store.ts']);
  });

  it('everySlicedStore_SitsInItsStoreFolder_FlagsASliceOutsideItsFolder', () => {
    const paths = ['machines/metrics/metrics-page.items.slice.ts', 'machines/metrics/metrics-page.store.ts'];

    expect(slicedStoreViolations(paths)).toEqual(['machines/metrics/metrics-page.items.slice.ts']);
  });

  it('everySlicedStore_SitsInItsStoreFolder_AcceptsASlicedStoreInItsFolder', () => {
    const paths = [
      'machines/metrics/metrics-page-store/metrics-page.items.slice.ts',
      'machines/metrics/metrics-page-store/metrics-page.store.ts',
    ];

    expect(slicedStoreViolations(paths)).toEqual([]);
  });
});
