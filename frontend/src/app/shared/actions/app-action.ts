import type { Type } from '@angular/core';

export interface AppAction<TPayload = unknown> {
  readonly type: string;
  readonly payload: TPayload;
}

export interface ActionResult {
  readonly type: string;
  readonly sequence: number;
  /** What the action host tells the readers of the result (e.g., the status of a new work order). */
  readonly detail?: unknown;
}

export interface PayloadValidationResult {
  readonly issues?: readonly unknown[] | undefined;
}

/** The part of the Standard Schema contract that the dispatcher uses. A `zod/mini` schema satisfies it. */
export interface PayloadSchema {
  readonly '~standard': {
    readonly validate: (value: unknown) => PayloadValidationResult | Promise<PayloadValidationResult>;
  };
}

/** The lazily loaded part of an action host: the component and the schema of the payload. */
export interface ActionHostModule {
  readonly component: Type<unknown>;
  readonly schema: PayloadSchema;
}

export interface ActionHostRegistration {
  readonly type: string;
  readonly load: () => Promise<ActionHostModule>;
}
