import { UpperCasePipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { Toast } from 'primeng/toast';
import { Toolbar } from 'primeng/toolbar';

import { Language } from '@shared/i18n/i18n.constants';
import { LanguageStore } from '@shared/i18n/language.store';
import { AuthStore } from '@shared/security/auth.store';
import { ThemeStore } from '@shared/theme/theme.store';

import { ShellStore } from './shell.store';

@Component({
  imports: [FormsModule, RouterOutlet, TranslatePipe, UpperCasePipe, Button, Select, Toast, Toolbar],
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
