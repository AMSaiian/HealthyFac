import { NgOptimizedImage, UpperCasePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { Drawer } from 'primeng/drawer';
import { Menu } from 'primeng/menu';
import { Toast } from 'primeng/toast';
import { Toolbar } from 'primeng/toolbar';
import { Tooltip } from 'primeng/tooltip';

import { ActionHost } from '@shared/actions/action-host/action-host';
import { Language } from '@shared/i18n/i18n.constants';
import { languageFlagUrl } from '@shared/i18n/language-flags';
import { LanguageStore } from '@shared/i18n/language.store';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';
import { NavigationStore } from '@shared/loading/navigation.store';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { HIDDEN_CLOSE_BUTTON_PT } from '@shared/theme/square-button-props';
import { ThemeStore } from '@shared/theme/theme.store';

import { ShellStore } from './shell.store';
import { languageMenuItems } from './shell.store.utils';

@Component({
  imports: [
    NgOptimizedImage,
    ActionHost,
    FormsModule,
    LoadingSpinner,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    Tooltip,
    TranslatePipe,
    UpperCasePipe,
    ButtonDirective,
    ConfirmDialog,
    Drawer,
    Menu,
    Toast,
    Toolbar,
  ],
  host: { class: 'flex min-h-0 min-w-5xl flex-1 flex-col' },
  providers: [ShellStore],
  selector: 'hf-shell',
  templateUrl: './shell.html',
})
export class Shell {
  protected readonly shellStore = inject(ShellStore);
  protected readonly languageStore = inject(LanguageStore);
  protected readonly themeStore = inject(ThemeStore);
  protected readonly hiddenClosePt = HIDDEN_CLOSE_BUTTON_PT;
  protected readonly authStore = inject(AuthStore);
  protected readonly navigationStore = inject(NavigationStore);
  protected readonly languages = Object.values(Language);
  protected readonly flagUrl = languageFlagUrl;
  protected readonly languageItems = computed(() =>
    languageMenuItems(this.languages, this.languageStore.language(), languageFlagUrl, (language) =>
      this.languageStore.use(language),
    ),
  );
}
