import Quill from 'quill';

import { keepFileImagesOnly, registerFileImageBlot } from './file-image-blot';

const FILE_ID = 'v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA';

function createQuill(): Quill {
  registerFileImageBlot();
  const host = document.createElement('div');
  document.body.append(host);
  return new Quill(host, { modules: { clipboard: { matchers: [['img', keepFileImagesOnly]] } } });
}

async function settle(): Promise<void> {
  await new Promise((resolve) => setTimeout(resolve, 0));
}

describe('FileImageBlot', () => {
  it('value_OfAnInsertedImage_ReturnsTheIdAndTheAltWithoutSrc', () => {
    const quill = createQuill();

    quill.insertEmbed(0, 'fileImage', { fileId: FILE_ID, alt: 'pump' });

    const image = quill.root.querySelector('img') as HTMLElement;
    expect(quill.getContents().ops[0].insert).toEqual({ fileImage: { fileId: FILE_ID, alt: 'pump' } });
    expect(image.hasAttribute('src')).toBe(false);
  });

  it('getSemanticHTML_WithAnImage_HoldsTheFileIdAndAltAndNoSrc', () => {
    const quill = createQuill();

    quill.insertEmbed(0, 'fileImage', { fileId: FILE_ID, alt: 'a "pump"' });

    const html = quill.getSemanticHTML();
    expect(html).toContain(`data-file-id="${FILE_ID}"`);
    expect(html).toContain('alt="a &quot;pump&quot;"');
    expect(html).not.toContain('src');
  });

  it('getSemanticHTML_WithAnUploadingImage_HoldsNoImage', () => {
    const quill = createQuill();

    quill.insertEmbed(0, 'fileImage', { fileId: '', alt: 'pump' });

    expect(quill.getSemanticHTML()).not.toContain('<img');
  });

  it('src_SetOnTheDomNodeAfterTheLoad_ChangesNeitherTheHtmlNorEmitsAChange', async () => {
    const quill = createQuill();
    quill.insertEmbed(0, 'fileImage', { fileId: FILE_ID, alt: 'pump' });
    await settle();
    const onChange = vi.fn();
    quill.on('text-change', onChange);
    const before = quill.getSemanticHTML();

    (quill.root.querySelector('img') as HTMLElement).setAttribute('src', 'https://api/blob?sig=x');
    await settle();

    expect(onChange).not.toHaveBeenCalled();
    expect(quill.getSemanticHTML()).toBe(before);
  });

  it('convert_WithExternalImage_DropsIt', () => {
    const quill = createQuill();

    const delta = quill.clipboard.convert({ html: '<p>a<img src="https://example.com/x.png"></p>' });

    expect(JSON.stringify(delta.ops)).not.toContain('image');
    expect(JSON.stringify(delta.ops)).not.toContain('fileImage');
  });

  it('convert_WithFileImage_KeepsIt', () => {
    const quill = createQuill();

    const delta = quill.clipboard.convert({ html: `<p><img data-file-id="${FILE_ID}" alt="pump"></p>` });

    expect(delta.ops[0].insert).toEqual({ fileImage: { fileId: FILE_ID, alt: 'pump' } });
  });
});
