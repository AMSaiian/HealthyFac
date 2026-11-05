import { NgComponentOutlet } from '@angular/common';
import { Component, inject, resource } from '@angular/core';

import { ACTION_HOSTS } from '@shared/actions/action-registry';
import { ActionStore } from '@shared/actions/action.store';

@Component({
  imports: [NgComponentOutlet],
  host: { class: 'contents' },
  selector: 'hf-action-host',
  templateUrl: './action-host.html',
})
export class ActionHost {
  protected readonly store = inject(ActionStore);

  private readonly _registrations = inject(ACTION_HOSTS);

  protected readonly component = resource({
    params: () => this.store.active()?.type,
    loader: ({ params }) => {
      const registration = this._registrations.find((candidate) => candidate.type === params);
      return registration === undefined
        ? Promise.reject(new Error(`No action host is registered for "${params}".`))
        : registration.load().then((module) => module.component);
    },
  });
}
