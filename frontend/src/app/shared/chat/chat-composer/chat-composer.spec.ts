import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import { ChatComposer } from '@shared/chat/chat-composer/chat-composer';

function buttonLabels(canStop: boolean): string[] {
  TestBed.configureTestingModule({ providers: [provideTranslateService()] });
  const fixture = TestBed.createComponent(ChatComposer);
  fixture.componentRef.setInput('canStop', canStop);
  fixture.detectChanges();
  const buttons: HTMLButtonElement[] = Array.from(fixture.nativeElement.querySelectorAll('button'));
  return buttons.map((button) => button.textContent?.trim() ?? '');
}

describe('ChatComposer', { tags: ['Chat'] }, () => {
  it('composer_WhileThisTabSendsTheTurn_ShowsStopAndNoSend', () => {
    const labels = buttonLabels(true);

    expect(labels.some((label) => label.includes('CHAT.COMPOSER.STOP'))).toBe(true);
    expect(labels.some((label) => label.includes('CHAT.COMPOSER.SEND'))).toBe(false);
  });

  it('composer_WhenNoTurnCanStop_ShowsSendAndNoStop', () => {
    const labels = buttonLabels(false);

    expect(labels.some((label) => label.includes('CHAT.COMPOSER.SEND'))).toBe(true);
    expect(labels.some((label) => label.includes('CHAT.COMPOSER.STOP'))).toBe(false);
  });
});
