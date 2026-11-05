import { Component } from '@angular/core';
import { ProgressSpinner } from 'primeng/progressspinner';

@Component({
  imports: [ProgressSpinner],
  host: { class: 'flex flex-1 items-center justify-center' },
  selector: 'hf-loading-spinner',
  templateUrl: './loading-spinner.html',
})
export class LoadingSpinner {}
