export interface GridColumnState<TColumn extends string> {
  readonly column: TColumn;
  readonly isVisible: boolean;
}

export function visibleColumns<TColumn extends string>(columns: readonly GridColumnState<TColumn>[]): TColumn[] {
  return columns.filter((state) => state.isVisible).map((state) => state.column);
}

export function toggleColumn<TColumn extends string>(
  visible: readonly TColumn[],
  column: TColumn,
  isVisible: boolean,
): TColumn[] {
  return isVisible ? [...visible, column] : visible.filter((candidate) => candidate !== column);
}

/** Puts the visible columns in the new order. A hidden column keeps its slot. */
export function reorderColumns<TColumn extends string>(
  columns: readonly GridColumnState<TColumn>[],
  order: readonly TColumn[],
): GridColumnState<TColumn>[] {
  const queue = [...order];
  return columns.map((entry) => (entry.isVisible ? { column: queue.shift() ?? entry.column, isVisible: true } : entry));
}
