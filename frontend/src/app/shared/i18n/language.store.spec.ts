import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { PrimeNG } from 'primeng/config';
import { of, throwError } from 'rxjs';

import { AuthStore } from '@shared/security/auth.store';
import { BROWSER_STORAGE } from '@shared/storage/browser-storage';

import { Language } from './i18n.constants';
import { LanguageStore } from './language.store';
import { LocaleApi } from './locale-api';

describe('LanguageStore', () => {
  const isAuthenticated = signal(true);
  const signInLocale = signal<string | null>(null);
  const setUiLocale = vi.fn();
  const save = vi.fn();

  function createStore(): InstanceType<typeof LanguageStore> {
    TestBed.configureTestingModule({
      providers: [
        { provide: BROWSER_STORAGE, useValue: null },
        { provide: AuthStore, useValue: { isAuthenticated, signInLocale, setUiLocale } },
        { provide: LocaleApi, useValue: { save } },
        { provide: TranslateService, useValue: { use: () => of({}), instant: () => ({}) } },
        { provide: PrimeNG, useValue: { setTranslation: vi.fn() } },
      ],
    });
    return TestBed.inject(LanguageStore);
  }

  beforeEach(() => {
    isAuthenticated.set(true);
    signInLocale.set(null);
    setUiLocale.mockReset();
    save.mockReset().mockReturnValue(of(undefined));
  });

  it('use_WhenSignedIn_SavesTheLocale', () => {
    const store = createStore();

    store.use(Language.Ukrainian);

    expect(save).toHaveBeenCalledWith(Language.Ukrainian);
    expect(store.language()).toBe(Language.Ukrainian);
  });

  it('use_WhenSavingFails_KeepsTheLanguage', () => {
    save.mockReturnValue(throwError(() => new Error('down')));
    const store = createStore();

    store.use(Language.Ukrainian);

    expect(store.language()).toBe(Language.Ukrainian);
  });

  it('use_WhenSignedOut_CallsNoEndpoint', () => {
    isAuthenticated.set(false);
    const store = createStore();

    store.use(Language.Ukrainian);

    expect(save).not.toHaveBeenCalled();
  });

  it('language_WhenSignInLocaleDiffers_AdoptsItWithoutSaving', () => {
    const store = createStore();
    signInLocale.set('uk');
    TestBed.tick();

    expect(store.language()).toBe(Language.Ukrainian);
    expect(save).not.toHaveBeenCalled();
  });

  it('language_WhenSignInLocaleIsUnknown_KeepsTheLanguage', () => {
    const store = createStore();
    signInLocale.set('de');
    TestBed.tick();

    expect(store.language()).toBe(Language.English);
  });

  it('language_Always_PassesTheLanguageAsUiLocale', () => {
    const store = createStore();
    store.use(Language.Ukrainian);
    TestBed.tick();

    expect(setUiLocale).toHaveBeenLastCalledWith(Language.Ukrainian);
  });
});
