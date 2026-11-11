import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Skeleton } from 'primeng/skeleton';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import { Temporal } from '@shared/time/temporal';
import { delayedLoading } from '@shared/loading/delayed-loading';

import { OverrideDialog } from '@employees/shifts/override-dialog/override-dialog';
import type { OverrideFormValue } from '@employees/shifts/override-dialog/override-dialog-schema';
import { ShiftsStore } from '@employees/shifts/shifts.store';
import { summaryOf, toVersionFormValue } from '@employees/shifts/shifts.store.utils';
import { VersionDialog } from '@employees/shifts/version-dialog/version-dialog';
import type { VersionFormValue } from '@employees/shifts/version-dialog/version-dialog-schema';
import type { ShiftOverride, ShiftVersion, Weekday } from '@employees/shifts/shift';

@Component({
  imports: [ActionDialog, ButtonDirective, OverrideDialog, RouterLink, Skeleton, TranslatePipe, VersionDialog],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 overflow-y-auto p-4' },
  selector: 'hf-shifts-page',
  templateUrl: './shifts-page.html',
})
export class ShiftsPage {
  readonly id = input.required<string>();

  protected readonly store = inject(ShiftsStore);
  protected readonly showVersionsSkeleton = delayedLoading(this.store.versions.isLoading);
  protected readonly showOverridesSkeleton = delayedLoading(this.store.overrides.isLoading);
  protected readonly summaryOf = summaryOf;

  protected readonly versions = computed(() => (this.store.versions.hasValue() ? this.store.versions.value() : []));
  protected readonly overrides = computed(() => (this.store.overrides.hasValue() ? this.store.overrides.value() : []));
  protected readonly tomorrow = computed(() => this.store.today().add({ days: 1 }));

  private readonly _version = signal<VersionFormValue | null>(null);
  private readonly _override = signal<OverrideFormValue | null>(null);
  private readonly _isValid = signal(false);

  protected readonly versionInitial = computed((): VersionFormValue => {
    const dialog = this.store.dialog();
    return toVersionFormValue(dialog?.kind === 'version' ? dialog.version : null, this.store.today());
  });
  protected readonly overrideInitial = computed((): OverrideFormValue => {
    const dialog = this.store.dialog();
    const override = dialog?.kind === 'override' ? dialog.override : null;
    return override === null
      ? { date: this.tomorrow(), blocks: [] }
      : {
          date: Temporal.PlainDate.from(override.date),
          blocks: override.blocks.map((block) => ({ ...block })),
        };
  });
  protected readonly saveReason = computed((): string | null => (this._isValid() ? null : 'SHIFTS.DIALOG.INVALID'));

  constructor() {
    effect(() => {
      this.store.setEmployee(this.id());
    });
  }

  protected weekdayKey(weekday: Weekday): string {
    return `SHIFTS.WEEKDAYS.${weekday.toUpperCase()}`;
  }

  protected onVersion(value: VersionFormValue): void {
    this._version.set(value);
  }

  protected onOverride(value: OverrideFormValue): void {
    this._override.set(value);
  }

  protected onValidity(isValid: boolean): void {
    this._isValid.set(isValid);
  }

  protected save(): void {
    const dialog = this.store.dialog();
    const version = this._version();
    const override = this._override();
    if (this.saveReason() !== null) {
      return;
    }
    if (dialog?.kind === 'version' && version !== null) {
      this.store.saveVersion(version);
    } else if (dialog?.kind === 'override' && override !== null) {
      this.store.saveOverride(override);
    }
  }

  protected editVersion(version: ShiftVersion): void {
    this.store.openVersionDialog(version);
  }

  protected editOverride(override: ShiftOverride): void {
    this.store.openOverrideDialog(override);
  }
}
