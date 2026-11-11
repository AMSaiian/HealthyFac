import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { z } from 'zod/mini';

import { provideActionHosts } from './action-registry';
import { ActionStore } from './action.store';
import type { ActionHostRegistration } from './app-action';

const CREATE_TYPE = 'things.create';
const SAVED_TYPE = 'things.saved';
const REVIEW_TYPE = 'things.review';
const PARENT = { type: CREATE_TYPE, payload: { name: 'Lathe' } };
const CHILD = { type: REVIEW_TYPE, payload: { name: 'Review' } };
const PAGE_URL = '/files';
const TARGET_PREFIX = '/machines/m1/metrics';
const TARGET_URL = `${TARGET_PREFIX}?draft=1`;

class Blank {}

const registration: ActionHostRegistration = {
  type: CREATE_TYPE,
  load: () => Promise.resolve({ component: class TestHost {}, schema: z.object({ name: z.string() }) }),
};
const reviewRegistration: ActionHostRegistration = {
  type: REVIEW_TYPE,
  load: () => Promise.resolve({ component: class ReviewHost {}, schema: z.object({ name: z.string() }) }),
};

describe('ActionStore', () => {
  function createStore(): InstanceType<typeof ActionStore> {
    TestBed.configureTestingModule({
      providers: [
        provideActionHosts([registration, reviewRegistration]),
        provideRouter([
          { path: 'files', component: Blank },
          { path: 'machines/:id/metrics', component: Blank },
          { path: 'other', component: Blank },
        ]),
      ],
    });
    return TestBed.inject(ActionStore);
  }

  it('dispatch_WithRegisteredType_SetsActive', async () => {
    const store = createStore();
    const action = { type: CREATE_TYPE, payload: { name: 'Lathe' } };

    await store.dispatch(action);

    expect(store.active()).toEqual(action);
  });

  it('dispatch_WithInvalidPayload_DoesNotOpen', async () => {
    const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const store = createStore();

    await store.dispatch({ type: CREATE_TYPE, payload: { name: 42 } });

    expect(store.active()).toBeNull();
    expect(logged).toHaveBeenCalledTimes(1);
  });

  it('dispatch_WithUnknownType_DoesNotOpen', async () => {
    const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const store = createStore();

    await store.dispatch({ type: 'things.unknown', payload: {} });

    expect(store.active()).toBeNull();
    expect(logged).toHaveBeenCalledTimes(1);
  });

  it('complete_Twice_IncreasesSequence', async () => {
    const store = createStore();
    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Lathe' } });

    store.complete(SAVED_TYPE);
    const first = store.lastResult();
    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Press' } });
    store.complete(SAVED_TYPE);
    const second = store.lastResult();

    expect(first).toEqual({ type: SAVED_TYPE, sequence: 1 });
    expect(second).toEqual({ type: SAVED_TYPE, sequence: 2 });
    expect(store.active()).toBeNull();
  });

  it('cancel_Always_ClearsActiveWithoutResult', async () => {
    const store = createStore();
    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Lathe' } });

    store.cancel();

    expect(store.active()).toBeNull();
    expect(store.lastResult()).toBeNull();
  });

  it('complete_WhenTheActionIsAChild_OpensTheParentAgainWithAResult', async () => {
    const store = createStore();
    await store.dispatch(PARENT);
    await store.dispatchChild(CHILD);

    store.complete(SAVED_TYPE);

    expect(store.active()).toEqual(PARENT);
    expect(store.parent()).toBeNull();
    expect(store.lastResult()).toEqual({ type: SAVED_TYPE, sequence: 1 });
  });

  it('cancel_WhenTheActionIsAChild_OpensTheParentAgain', async () => {
    const store = createStore();
    await store.dispatch(PARENT);
    await store.dispatchChild(CHILD);

    store.cancel();

    expect(store.active()).toEqual(PARENT);
    expect(store.parent()).toBeNull();
    expect(store.lastResult()).toBeNull();
  });

  it('dispatchChild_WithInvalidPayload_KeepsTheParentOpen', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const store = createStore();
    await store.dispatch(PARENT);

    await store.dispatchChild({ type: REVIEW_TYPE, payload: { name: 42 } });

    expect(store.active()).toEqual(PARENT);
    expect(store.parent()).toBeNull();
  });

  it('dispatch_WhileAChildIsOpen_DropsTheParent', async () => {
    const store = createStore();
    await store.dispatch(PARENT);
    await store.dispatchChild(CHILD);

    await store.dispatch(PARENT);
    store.cancel();

    expect(store.active()).toBeNull();
  });

  it('navigation_ToTheStoredUrl_OpensTheStoredActionAndClearsThePath', async () => {
    const store = createStore();
    const router = TestBed.inject(Router);
    await router.navigateByUrl(PAGE_URL);
    store.rememberReturn(PAGE_URL, PARENT, TARGET_PREFIX);
    await router.navigateByUrl(TARGET_URL);

    await router.navigateByUrl(PAGE_URL);

    expect(store.active()).toEqual(PARENT);
    expect(store.returnPath()).toBeNull();
  });

  it('navigation_ToAThirdUrl_DropsThePathAndOpensNothing', async () => {
    const store = createStore();
    const router = TestBed.inject(Router);
    await router.navigateByUrl(PAGE_URL);
    store.rememberReturn(PAGE_URL, PARENT, TARGET_PREFIX);
    await router.navigateByUrl(TARGET_URL);

    await router.navigateByUrl('/other');
    await router.navigateByUrl(PAGE_URL);

    expect(store.returnPath()).toBeNull();
    expect(store.active()).toBeNull();
  });

  it('navigation_UnderTheTarget_KeepsThePath', async () => {
    const store = createStore();
    const router = TestBed.inject(Router);
    await router.navigateByUrl(PAGE_URL);
    store.rememberReturn(PAGE_URL, PARENT, TARGET_PREFIX);

    await router.navigateByUrl(TARGET_URL);
    await router.navigateByUrl(`${TARGET_PREFIX}?draft=2`);

    expect(store.returnPath()).toEqual({ url: PAGE_URL, action: PARENT, targetPrefix: TARGET_PREFIX });
    expect(store.active()).toBeNull();
  });

  it('complete_WithAReturnAction_ReopensTheCallingAction', async () => {
    const store = createStore();
    const caller = { type: CREATE_TYPE, payload: { name: 'Lathe' } };
    await store.dispatch(caller);

    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Press' } }, caller);
    store.complete(SAVED_TYPE);

    expect(store.active()).toEqual(caller);
    expect(store.lastResult()?.type).toBe(SAVED_TYPE);
  });

  it('cancel_WithAReturnAction_ReopensTheCallingAction', async () => {
    const store = createStore();
    const caller = { type: CREATE_TYPE, payload: { name: 'Lathe' } };

    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Press' } }, caller);
    store.cancel();

    expect(store.active()).toEqual(caller);
  });
});
