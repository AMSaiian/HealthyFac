import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { of, Subject } from 'rxjs';

import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';

import { RichTextEditor } from './rich-text-editor';

const FILE_ID = 'v1.AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA';
const HTML = `<p>text</p><p><img data-file-id="${FILE_ID}" alt="pump"></p>`;
const LINK = 'https://api/blob?sig=x';

interface Internals {
  _quill: {
    root: HTMLElement;
    getModule(name: 'uploader'): { upload(range: { index: number }, files: readonly File[]): void };
  } | null;
  _upload(range: { index: number } | null, files: readonly File[]): void;
}

function file(name: string, size: number, type = ''): File {
  return new File([new Uint8Array(size)], name, { type });
}

describe('RichTextEditor', () => {
  async function setup(): Promise<{
    editor: RichTextEditor;
    internals: Internals;
    resolve: ReturnType<typeof vi.fn>;
    upload: ReturnType<typeof vi.fn>;
    messages: { add: ReturnType<typeof vi.fn> };
    fixture: ReturnType<typeof TestBed.createComponent<RichTextEditor>>;
  }> {
    const resolve = vi.fn(() => of(new Map([[FILE_ID, LINK]])));
    const upload = vi.fn(() => of(FILE_ID));
    const messages = { add: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        { provide: RichTextImageLinks, useValue: { resolve, upload } },
        { provide: MessageService, useValue: messages },
        provideTranslateService(),
      ],
    });
    const fixture = TestBed.createComponent(RichTextEditor);
    fixture.detectChanges();
    const internals = fixture.componentInstance as unknown as Internals;
    await vi.waitFor(() => expect(internals._quill).not.toBeNull());
    return { editor: fixture.componentInstance, internals, resolve, upload, messages, fixture };
  }

  it('upload_WithRefusedPdfAndLargePng_NeverCallTheApi', async () => {
    const { internals, upload, messages } = await setup();

    internals._upload(null, [file('a.pdf', 10), file('big.png', 5_242_881)]);

    expect(upload).not.toHaveBeenCalled();
    expect(messages.add).toHaveBeenCalledTimes(2);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'RICH_TEXT.IMAGE_NOT_ALLOWED' }));
  });

  it('upload_WhileTheImageIsInFlight_CountsOnePendingUpload', async () => {
    const { editor, internals, upload } = await setup();
    const response = new Subject<string>();
    upload.mockReturnValueOnce(response);
    const pendingUploads = (editor as unknown as { pendingUploads: () => number }).pendingUploads;

    internals._upload(null, [file('pump.png', 10, 'image/png')]);

    expect(pendingUploads()).toBe(1);

    response.next(FILE_ID);
    response.complete();

    expect(pendingUploads()).toBe(0);
  });

  it('quillUploader_WithGifWebpAndPdf_UploadsTheImagesAndRefusesThePdfWithTheToast', async () => {
    const { internals, upload, messages } = await setup();

    internals._quill
      ?.getModule('uploader')
      .upload({ index: 0 }, [
        file('anim.gif', 10, 'image/gif'),
        file('photo.webp', 10, 'image/webp'),
        file('manual.pdf', 10, 'application/pdf'),
      ]);

    expect(upload).toHaveBeenCalledTimes(2);
    expect(messages.add).toHaveBeenCalledTimes(1);
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'RICH_TEXT.IMAGE_NOT_ALLOWED' }));
  });

  it('upload_WithPng_UploadsAndEndsWithTheImageInTheValue', async () => {
    const { editor, internals, upload, fixture } = await setup();

    internals._upload(null, [file('scheme.png', 10)]);
    fixture.detectChanges();

    expect(upload).toHaveBeenCalledTimes(1);
    await vi.waitFor(() => expect(editor.value()).toContain(`data-file-id="${FILE_ID}"`));
    expect(editor.value()).toContain('alt="scheme"');
    expect(editor.value()).not.toContain('src');
  });

  it('setValue_WithImage_ShowsTheLinkOnTheDomNodeAndKeepsTheValueFreeOfSrc', async () => {
    const { editor, internals, resolve, fixture } = await setup();

    fixture.componentRef.setInput('value', HTML);
    fixture.detectChanges();
    await fixture.whenStable();

    await vi.waitFor(() => expect(internals._quill?.root.querySelector('img')?.getAttribute('src')).toBe(LINK));
    expect(resolve).toHaveBeenCalledWith([FILE_ID]);
    expect(editor.value()).toBe(HTML);
  });
});
