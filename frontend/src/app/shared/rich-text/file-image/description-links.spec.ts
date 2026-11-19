import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { descriptionLinks } from './description-links';
import type { RichTextImageLinks } from './rich-text-image-links';

const IMAGE_ID = 'v1_AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA';
const IMAGE_URL = 'https://api/blob?sig=x';

async function setup(description: string | null): Promise<{
  links: ReturnType<typeof descriptionLinks>['links'];
  resolve: ReturnType<typeof vi.fn>;
}> {
  const resolve = vi.fn(() => of(new Map([[IMAGE_ID, IMAGE_URL]])));
  const source = signal(description);
  const result = TestBed.runInInjectionContext(() =>
    descriptionLinks(source, { resolve } as unknown as RichTextImageLinks),
  );
  TestBed.tick();
  await TestBed.inject(ApplicationRef).whenStable();
  return { links: result.links, resolve };
}

describe('descriptionLinks', () => {
  it('descriptionLinks_WithoutImages_SendsNoRequest', async () => {
    const { links, resolve } = await setup('<p>text</p>');

    expect(resolve).not.toHaveBeenCalled();
    expect(links().size).toBe(0);
  });

  it('descriptionLinks_WithImages_ResolvesTheirIds', async () => {
    const { links, resolve } = await setup(`<p><img data-file-id="${IMAGE_ID}" alt="x"></p>`);

    expect(resolve).toHaveBeenCalledWith([IMAGE_ID]);
    expect(links().get(IMAGE_ID)).toBe(IMAGE_URL);
  });
});
