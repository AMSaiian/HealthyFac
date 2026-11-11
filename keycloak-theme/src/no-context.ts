import { Component } from '@angular/core';

@Component({
  selector: 'hf-root',
  template: `<p>This page needs a Keycloak context.</p>`,
  host: { class: 'block' },
})
export class NoContextComponent {}
