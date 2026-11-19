export enum MetricValueKind {
  Numeric = 'numeric',
  Boolean = 'boolean',
  StateSet = 'stateSet',
}

/** How the readings of a numeric metric behave: a level moves up and down, a counter only grows. */
export enum MetricBehavior {
  Level = 'level',
  Counter = 'counter',
}

/** The default limits of a numeric metric. An empty limit means no limit on that side. */
export interface MetricLimitValues {
  readonly warningLow: number | null;
  readonly warningHigh: number | null;
  readonly alarmLow: number | null;
  readonly alarmHigh: number | null;
}

export interface MetricUnitReference {
  readonly quantityKind: string;
  readonly name: string;
}

export interface MetricUnit extends MetricUnitReference {
  readonly abbreviation: string;
}

export interface MetricDefinition extends MetricLimitValues {
  readonly id: string;
  readonly key: string;
  readonly name: string;
  readonly description: string | null;
  readonly valueKind: MetricValueKind;
  readonly unit: MetricUnit;
  readonly min: number | null;
  readonly max: number | null;
  readonly states: readonly string[] | null;
  readonly behavior: MetricBehavior;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface MetricDefinitionsResponse {
  readonly revision: number;
  readonly definitions: readonly MetricDefinition[];
}

export interface CreatedMetricDefinition extends MetricLimitValues {
  readonly key: string;
  readonly name: string;
  readonly description: string | null;
  readonly valueKind: MetricValueKind;
  readonly unit: MetricUnitReference | null;
  readonly min: number | null;
  readonly max: number | null;
  readonly states: readonly string[] | null;
  /** The behavior of a numeric metric, or `null` for another kind. */
  readonly behavior: MetricBehavior | null;
  /** The metric draft that proposed this change. Telemetry marks it applied in the same save. */
  readonly draftId: string | null;
}

export interface UpdatedMetricDefinition extends MetricLimitValues {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly min: number | null;
  readonly max: number | null;
  readonly states: readonly string[] | null;
  /** The behavior of a numeric metric, or `null` for another kind. */
  readonly behavior: MetricBehavior | null;
  /** The metric draft that proposed this change. Telemetry marks it applied in the same save. */
  readonly draftId: string | null;
}

export interface ApplyMetricDefinitionsBody {
  readonly expectedRevision: number;
  readonly created: readonly CreatedMetricDefinition[];
  readonly updated: readonly UpdatedMetricDefinition[];
  readonly deleted: readonly string[];
}

export interface ApplyMetricDefinitionsResponse {
  readonly revision: number;
}

export interface MeasurementUnit {
  readonly name: string;
  readonly abbreviation: string;
}

export interface QuantityKind {
  readonly name: string;
  readonly units: readonly MeasurementUnit[];
}

export interface MetricUnitsResponse {
  readonly kinds: readonly QuantityKind[];
}
