export enum MetricValueKind {
  Numeric = 'numeric',
  Boolean = 'boolean',
  StateSet = 'stateSet',
}

export interface MetricUnitReference {
  readonly quantityKind: string;
  readonly name: string;
}

export interface MetricUnit extends MetricUnitReference {
  readonly abbreviation: string;
}

export interface MetricDefinition {
  readonly id: string;
  readonly key: string;
  readonly name: string;
  readonly description: string | null;
  readonly valueKind: MetricValueKind;
  readonly unit: MetricUnit;
  readonly min: number | null;
  readonly max: number | null;
  readonly states: readonly string[] | null;
  readonly createdAt: string;
  readonly updatedAt: string | null;
}

export interface MetricDefinitionsResponse {
  readonly revision: number;
  readonly definitions: readonly MetricDefinition[];
}

export interface CreatedMetricDefinition {
  readonly key: string;
  readonly name: string;
  readonly description: string | null;
  readonly valueKind: MetricValueKind;
  readonly unit: MetricUnitReference | null;
  readonly min: number | null;
  readonly max: number | null;
  readonly states: readonly string[] | null;
}

export interface UpdatedMetricDefinition {
  readonly id: string;
  readonly name: string;
  readonly description: string | null;
  readonly min: number | null;
  readonly max: number | null;
  readonly states: readonly string[] | null;
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
