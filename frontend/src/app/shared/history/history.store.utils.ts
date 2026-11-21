import { UrlTree } from '@angular/router';

export const COMPOSE_PARAM = 'compose';

const COMPOSE_SEPARATOR = ':';

export function parseCompose(value: string): { name: string; id: string | null } {
  const separatorIndex = value.indexOf(COMPOSE_SEPARATOR);
  if (separatorIndex === -1) {
    return { name: value, id: null };
  }
  const id = value.slice(separatorIndex + 1);
  return { name: value.slice(0, separatorIndex), id: id.length === 0 ? null : id };
}

export function formatCompose(name: string, id: string | null = null): string {
  return id === null ? name : `${name}${COMPOSE_SEPARATOR}${id}`;
}

/** Copies the tree without the `compose` query param. The other query params stay. */
export function withoutCompose(tree: UrlTree): UrlTree {
  const queryParams = Object.fromEntries(Object.entries(tree.queryParams).filter(([key]) => key !== COMPOSE_PARAM));
  return new UrlTree(tree.root, queryParams, tree.fragment);
}

export function withCompose(tree: UrlTree, value: string): UrlTree {
  return new UrlTree(tree.root, { ...tree.queryParams, [COMPOSE_PARAM]: value }, tree.fragment);
}
