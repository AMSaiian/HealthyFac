import { Component } from '@angular/core';

import { Shell } from '@app/layout/shell/shell';

@Component({
  imports: [Shell],
  selector: 'hf-root',
  templateUrl: './app.html',
})
export class App {}
