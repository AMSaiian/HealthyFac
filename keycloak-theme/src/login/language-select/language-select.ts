import { NgOptimizedImage } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { ButtonDirective } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { LANGUAGE_FLAGS } from '@frontend-flags';
import { languageMenuItems, type LanguageOption, toLanguageLabel } from './language-select.utils';

export type { LanguageOption };

@Component({
  selector: 'hf-language-select',
  imports: [ButtonDirective, MenuModule, NgOptimizedImage],
  templateUrl: './language-select.html',
  host: { class: 'block' },
})
export class LanguageSelect {
  readonly languages = input.required<readonly LanguageOption[]>();
  readonly currentTag = input.required<string>();
  readonly flagBaseUrl = input.required<string>();
  readonly picked = output<string>();

  protected readonly items = computed(() =>
    languageMenuItems(
      this.languages(),
      this.currentTag(),
      (tag) => this._flagUrl(tag),
      (href) => this.picked.emit(href),
    ),
  );
  protected readonly currentFlagUrl = computed(() => this._flagUrl(this.currentTag()));
  protected readonly currentLabel = computed(() => {
    const current = this.languages().find((language) => language.languageTag === this.currentTag());
    return current === undefined ? '' : toLanguageLabel(current.label, current.languageTag);
  });

  private _flagUrl(languageTag: string): string | null {
    const flagCode = (LANGUAGE_FLAGS as Readonly<Record<string, string | undefined>>)[languageTag];
    return flagCode ? `${this.flagBaseUrl()}/${flagCode}.svg` : null;
  }
}
