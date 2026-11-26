import { TestBed } from '@angular/core/testing';
import { signalStore, withMethods } from '@ngrx/signals';

import { DetailsMode, withDetailsMode } from './with-details-mode';

const TestStore = signalStore(
  withDetailsMode(),
  withMethods((store) => ({
    start(isUpdate: boolean): void {
      store._startMode(isUpdate);
    },
  })),
);

function setup(): InstanceType<typeof TestStore> {
  TestBed.configureTestingModule({ providers: [TestStore] });
  return TestBed.inject(TestStore);
}

describe('withDetailsMode', { tags: ['Platform'] }, () => {
  it('startMode_ForUpdate_IsView', () => {
    const store = setup();

    store.start(true);

    expect(store.mode()).toBe(DetailsMode.View);
    expect(store.isViewMode()).toBe(true);
    expect(store.isEditMode()).toBe(false);
  });

  it('startMode_ForCreate_IsEdit', () => {
    const store = setup();
    store.start(true);

    store.start(false);

    expect(store.mode()).toBe(DetailsMode.Edit);
    expect(store.isEditMode()).toBe(true);
    expect(store.isViewMode()).toBe(false);
  });

  it('edit_ThenView_ReturnsToView', () => {
    const store = setup();
    store.start(true);

    store.edit();
    const editMode = store.mode();
    store.view();

    expect(editMode).toBe(DetailsMode.Edit);
    expect(store.mode()).toBe(DetailsMode.View);
  });
});
