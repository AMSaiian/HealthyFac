import { HttpErrorResponse, HttpStatusCode, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';

import { AuthStore } from '@shared/security/auth-store/auth.store';

import { EmployeeRole } from './employee';
import { EmployeeStore } from './employee.store';

const EMPLOYEE_URL = '/api/core/v1/employees/me';

describe('EmployeeStore', () => {
  const isAuthenticated = signal(true);

  function createStore(): { store: InstanceType<typeof EmployeeStore>; http: HttpTestingController } {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthStore, useValue: { isAuthenticated } },
      ],
    });
    return { store: TestBed.inject(EmployeeStore), http: TestBed.inject(HttpTestingController) };
  }

  beforeEach(() => {
    isAuthenticated.set(true);
  });

  it('employee_AfterSignIn_ExposesIdNameAndRole', async () => {
    const { store, http } = createStore();
    TestBed.tick();

    http.expectOne(EMPLOYEE_URL).flush({ id: 'e1', firstName: 'Ann', lastName: 'Lee', roleCode: 'worker' });
    await Promise.resolve();
    TestBed.tick();

    expect(store.id()).toBe('e1');
    expect(store.name()).toBe('Ann Lee');
    expect(store.role()).toBe(EmployeeRole.Worker);
    expect(store.hasEmployee()).toBe(true);
  });

  it('employee_With404_GivesNoEmployee', async () => {
    const { store, http } = createStore();
    TestBed.tick();

    http.expectOne(EMPLOYEE_URL).flush(null, { status: HttpStatusCode.NotFound, statusText: 'Not Found' });
    await Promise.resolve();
    TestBed.tick();

    expect(store.hasEmployee()).toBe(false);
    expect(store.id()).toBeNull();
    expect(store.employee.error()).toBeUndefined();
  });

  it('employee_With500_KeepsTheError', async () => {
    const { store, http } = createStore();
    TestBed.tick();

    http.expectOne(EMPLOYEE_URL).flush(null, { status: HttpStatusCode.InternalServerError, statusText: 'Error' });
    await Promise.resolve();
    TestBed.tick();

    expect(store.employee.error()).toBeInstanceOf(HttpErrorResponse);
    expect(store.hasEmployee()).toBe(false);
  });

  it('employee_WhenSignedOut_SendsNoRequest', () => {
    isAuthenticated.set(false);
    const { http } = createStore();
    TestBed.tick();

    http.expectNone(EMPLOYEE_URL);
  });
});
