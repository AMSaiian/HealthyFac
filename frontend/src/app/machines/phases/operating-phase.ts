/** The operators of a phase condition. The backend emits the same literals. */
export enum PhaseConditionOperator {
  Active = 'active',
  Inactive = 'inactive',
  GreaterThan = 'greaterThan',
  GreaterOrEqual = 'greaterOrEqual',
  LessThan = 'lessThan',
  LessOrEqual = 'lessOrEqual',
  Between = 'between',
  IsTrue = 'isTrue',
  IsFalse = 'isFalse',
  In = 'in',
}

/** What a condition reads: a status (active or not) or a metric of a value kind. */
export enum ConditionSourceKind {
  Status = 'status',
  Numeric = 'numeric',
  Boolean = 'boolean',
  StateSet = 'stateSet',
}

export interface PhaseCondition {
  readonly metricDefinitionId: string | null;
  readonly statusDefinitionId: string | null;
  readonly operator: PhaseConditionOperator;
  readonly value: number | null;
  readonly upperValue: number | null;
  readonly states: readonly string[] | null;
}

/** The limits of one metric inside a phase. A `null` limit is not set. */
export interface PhaseLimit {
  readonly metricDefinitionId: string;
  readonly warningLow: number | null;
  readonly warningHigh: number | null;
  readonly alarmLow: number | null;
  readonly alarmHigh: number | null;
}

export interface OperatingPhase {
  readonly id: string;
  readonly code: string;
  readonly name: string;
  readonly description: string | null;
  readonly priority: number;
  readonly isTracked: boolean;
  readonly conditions: readonly PhaseCondition[];
  readonly limits: readonly PhaseLimit[];
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface OperatingPhasesResponse {
  readonly items: readonly OperatingPhase[];
}

/** The body of a create or an update. The priority of a new phase is the next free one. */
export interface OperatingPhaseBody {
  readonly code: string;
  readonly name: string;
  readonly description: string | null;
  readonly priority: number;
  readonly isTracked: boolean;
  readonly conditions: readonly PhaseCondition[];
  readonly limits: readonly PhaseLimit[];
}

export interface CreateOperatingPhaseResponse {
  readonly id: string;
}

export interface ReorderOperatingPhasesBody {
  readonly phaseIds: readonly string[];
}
