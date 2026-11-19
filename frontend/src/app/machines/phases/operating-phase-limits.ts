import { DESCRIPTION_MAX_LENGTH, KEY_MAX_LENGTH, KEY_PATTERN, NAME_MAX_LENGTH } from '@machines/metrics/metric-limits';

/** The limits of a phase mirror the backend constants of the metric definitions (`OperatingPhaseValidationConstants`). */
export const PHASE_CODE_MAX_LENGTH = KEY_MAX_LENGTH;
export const PHASE_CODE_PATTERN = KEY_PATTERN;
export const PHASE_NAME_MAX_LENGTH = NAME_MAX_LENGTH;
export const PHASE_DESCRIPTION_MAX_LENGTH = DESCRIPTION_MAX_LENGTH;
