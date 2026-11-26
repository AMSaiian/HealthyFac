import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import { EMPTY_VALUE } from '@work-orders/drawer/work-order-drawer-store/work-order-drawer.store.utils';
import { WorkOrderForm } from '@work-orders/drawer/work-order-form/work-order-form';

const ESTIMATE_INPUT = '#work-order-estimate';

class FakeResizeObserver {
  readonly observe = vi.fn();
  readonly unobserve = vi.fn();
  readonly disconnect = vi.fn();
}

function setup(isEstimateReadOnly: boolean): { estimateReadOnly: () => boolean; estimateInput: HTMLInputElement } {
  vi.stubGlobal('ResizeObserver', FakeResizeObserver);
  TestBed.configureTestingModule({
    providers: [provideTranslateService(), provideHttpClient(), provideHttpClientTesting(), MessageService],
  });
  const fixture = TestBed.createComponent(WorkOrderForm);
  fixture.componentRef.setInput('initialValue', EMPTY_VALUE);
  fixture.componentRef.setInput('isEstimateReadOnly', isEstimateReadOnly);
  fixture.detectChanges();
  const form = (
    fixture.componentInstance as unknown as {
      workOrderForm: { estimateMinutes: () => { readonly: () => boolean } };
    }
  ).workOrderForm;
  return {
    estimateReadOnly: () => form.estimateMinutes().readonly(),
    estimateInput: (fixture.nativeElement as HTMLElement).querySelector(ESTIMATE_INPUT) as HTMLInputElement,
  };
}

describe('WorkOrderForm', { tags: ['WorkOrders'] }, () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('estimate_WithoutManageRight_IsReadOnly', () => {
    const { estimateReadOnly, estimateInput } = setup(true);

    expect(estimateReadOnly()).toBe(true);
    expect(estimateInput.hasAttribute('readonly')).toBe(true);
  });

  it('estimate_WithManageRight_IsEditable', () => {
    const { estimateReadOnly, estimateInput } = setup(false);

    expect(estimateReadOnly()).toBe(false);
    expect(estimateInput.hasAttribute('readonly')).toBe(false);
  });
});
