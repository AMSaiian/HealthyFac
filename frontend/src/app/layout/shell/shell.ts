import { UpperCasePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { Drawer } from 'primeng/drawer';
import { Select } from 'primeng/select';
import { Toast } from 'primeng/toast';
import { Toolbar } from 'primeng/toolbar';

import { ActionHost } from '@shared/actions/action-host/action-host';
import { Language } from '@shared/i18n/i18n.constants';
import { LanguageStore } from '@shared/i18n/language.store';
import { AuthStore } from '@shared/security/auth.store';
import { ThemeStore } from '@shared/theme/theme.store';

import { ShellStore } from './shell.store';

@Component({
  imports: [
    ActionHost,
    FormsModule,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    TranslatePipe,
    UpperCasePipe,
    ButtonDirective,
    ConfirmDialog,
    Drawer,
    Select,
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
  protected readonly authStore = inject(AuthStore);
  protected readonly languages = Object.values(Language);
}
