import { computed, type ResourceRef, type Signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';

import type { RichTextImageLinks } from './rich-text-image-links';
import { collectFileIds } from './rich-text-images';

/** The resource that asks for the image links of a description, and the link map that a view reads. */
export interface DescriptionLinks {
  readonly resource: ResourceRef<ReadonlyMap<string, string> | undefined>;
  readonly links: Signal<ReadonlyMap<string, string>>;
}

const NO_LINKS: ReadonlyMap<string, string> = new Map();

/**
 * Resolves the signed links of the images in a rich text description. A description without an image sends no request.
 * Call it in an injection context, because the resource needs one.
 */
export function descriptionLinks(description: () => string | null, links: RichTextImageLinks): DescriptionLinks {
  const resource = rxResource({
    params: () => {
      const ids = collectFileIds(description());
      return ids.length === 0 ? undefined : ids;
    },
    stream: ({ params }) => links.resolve(params),
  });
  return {
    resource,
    links: computed((): ReadonlyMap<string, string> => (resource.hasValue() ? resource.value() : NO_LINKS)),
  };
}
