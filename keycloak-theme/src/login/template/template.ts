import { NgTemplateOutlet } from '@angular/common';
import type { ElementRef } from '@angular/core';
import {
  Component,
  computed,
  DOCUMENT,
  effect,
  inject,
  input,
  signal,
  type Type,
  viewChild,
  ViewContainerRef,
} from '@angular/core';
import { Title } from '@angular/platform-browser';
import { KcSanitizePipe } from '@keycloakify/angular/lib/pipes/kc-sanitize';
import type { TemplateSlots } from '@keycloakify/angular/login/classes/template-slots';
import type { KcContext } from '@keycloakify/angular/login/KcContext';
import { LOGIN_I18N } from '@keycloakify/angular/login/tokens/i18n';
import { KC_LOGIN_CONTEXT } from '@keycloakify/angular/login/tokens/kc-context';
import { MessageModule } from 'primeng/message';
import type { I18n } from '../i18n';
import { LanguageSelect } from '../language-select/language-select';
import {
  BACKGROUND_VIDEO_FILE,
  FAVICON_LINKS,
  FLAGS_DIRECTORY,
  MESSAGE_SEVERITIES,
  REDUCED_MOTION_QUERY,
  RESOURCES_DIRECTORY,
} from './template.constants';
import { applyMotionPreference, isReducedMotion, shownMessageOf } from './template.utils';

@Component({
  selector: 'hf-root',
  imports: [NgTemplateOutlet, KcSanitizePipe, MessageModule, LanguageSelect],
  templateUrl: './template.html',
  host: { class: 'block' },
})
export class TemplateComponent {
  readonly page = input<Type<unknown>>();
  readonly userProfileFormFields = input<Type<unknown>>();

  protected readonly i18n = inject<I18n>(LOGIN_I18N);
  protected readonly kcContext = inject<KcContext>(KC_LOGIN_CONTEXT);

  protected readonly slots = signal<TemplateSlots | undefined>(undefined);
  protected readonly messageSeverities = MESSAGE_SEVERITIES;
  protected readonly isMotionReduced = signal(false);
  protected readonly videoUrl = `${this.kcContext.url.resourcesPath}/${RESOURCES_DIRECTORY}/${BACKGROUND_VIDEO_FILE}`;
  protected readonly flagsUrl = `${this.kcContext.url.resourcesPath}/${RESOURCES_DIRECTORY}/${FLAGS_DIRECTORY}`;

  protected readonly shownMessage = computed(() =>
    shownMessageOf(this.kcContext.message, this.slots(), this.kcContext.isAppInitiatedAction),
  );

  private readonly _document = inject(DOCUMENT);
  private readonly _title = inject(Title);
  private readonly _pageRef = viewChild('pageRef', { read: ViewContainerRef });
  private readonly _video = viewChild<ElementRef<HTMLVideoElement>>('video');

  constructor() {
    this._replaceFavicon();

    effect(() => {
      const page = this.page();
      const pageRef = this._pageRef();
      if (!page || !pageRef || this.slots()) {
        return;
      }

      const componentRef = pageRef.createComponent(page);
      const userProfileFormFields = this.userProfileFormFields();
      if ('userProfileFormFields' in (componentRef.instance as object) && userProfileFormFields) {
        componentRef.setInput('userProfileFormFields', userProfileFormFields);
      }

      const instance = componentRef.instance as TemplateSlots;
      this._title.setTitle(instance.documentTitle ?? this.i18n.msgStr('loginTitle', this.kcContext.realm.displayName));
      this.slots.set(instance);
    });

    const view = this._document.defaultView;
    this.isMotionReduced.set(isReducedMotion(view));
    view
      ?.matchMedia(REDUCED_MOTION_QUERY)
      .addEventListener('change', (event) => this.isMotionReduced.set(event.matches));

    effect(() => {
      const video = this._video()?.nativeElement;
      if (!video) {
        return;
      }

      void applyMotionPreference(video, this.isMotionReduced());
    });
  }

  private _replaceFavicon(): void {
    const head = this._document.head;
    head.querySelectorAll('link[rel~="icon"], link[rel="apple-touch-icon"]').forEach((link) => link.remove());
    FAVICON_LINKS.forEach(({ rel, type, sizes, file }) => {
      const link = this._document.createElement('link');
      link.rel = rel;
      link.href = `${this.kcContext.url.resourcesPath}/${RESOURCES_DIRECTORY}/${file}`;
      if (type) {
        link.type = type;
      }
      if (sizes) {
        link.setAttribute('sizes', sizes);
      }
      head.appendChild(link);
    });
  }

  protected openLanguage(href: string): void {
    this._document.location.assign(href);
  }
}
