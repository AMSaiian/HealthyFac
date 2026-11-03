import { inject, Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { type RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';

const APP_NAME_KEY = 'APP.NAME';
const TITLE_SEPARATOR = ' · ';

@Injectable()
export class TranslatedTitleStrategy extends TitleStrategy {
  private readonly _title = inject(Title);
  private readonly _translate = inject(TranslateService);
  private _titleKey: string | undefined;

  constructor() {
    super();
    this._translate.onLangChange.subscribe(() => {
      this._applyTitle();
    });
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    this._titleKey = this.buildTitle(snapshot);
    this._applyTitle();
  }

  private _applyTitle(): void {
    const appName: string = this._translate.instant(APP_NAME_KEY);
    const pageTitle = this._titleKey === undefined ? undefined : this._translate.instant(this._titleKey);
    this._title.setTitle(pageTitle === undefined ? appName : `${pageTitle}${TITLE_SEPARATOR}${appName}`);
  }
}
