import { computed } from '@angular/core';
import { patchState, signalStoreFeature, withComputed, withMethods, withState } from '@ngrx/signals';

/** The two states of a details dialog: the read-only details, and the form. */
export enum DetailsMode {
  View = 'view',
  Edit = 'edit',
}

/**
 * The mode of a details dialog. The store calls `_startMode` when the dialog opens: an existing item starts in view
 * mode, a new item starts in edit mode. The pencil calls `edit`; a save or a cancel of an edit calls `view`.
 */
export function withDetailsMode() {
  return signalStoreFeature(
    withState<{ mode: DetailsMode }>({ mode: DetailsMode.Edit }),
    withComputed((store) => ({
      isViewMode: computed(() => store.mode() === DetailsMode.View),
      isEditMode: computed(() => store.mode() === DetailsMode.Edit),
    })),
    withMethods((store) => ({
      _startMode(isUpdate: boolean): void {
        patchState(store, { mode: isUpdate ? DetailsMode.View : DetailsMode.Edit });
      },
      edit(): void {
        patchState(store, { mode: DetailsMode.Edit });
      },
      view(): void {
        patchState(store, { mode: DetailsMode.View });
      },
    })),
  );
}
