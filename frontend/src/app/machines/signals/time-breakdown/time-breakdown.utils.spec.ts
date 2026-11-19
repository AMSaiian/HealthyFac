import type { MachineStateTimesResponse, PhaseTimesResponse } from '@machines/signals/signal-episode';
import {
  colorTokensOf,
  durationParts,
  SEGMENT_COLOR_TOKENS,
  sharesOf,
  TimeRowKind,
  toPhaseRows,
  toStateRows,
} from '@machines/signals/time-breakdown/time-breakdown.utils';

const PHASE_LABELS = { unknownMode: 'Unknown mode', noData: 'No data', deleted: 'deleted' };
const PHASES: PhaseTimesResponse = {
  phases: [
    { phaseId: 'p1', code: 'running', name: 'Running', isTracked: true, isDeleted: false, seconds: 1800 },
    { phaseId: 'p2', code: 'stopped', name: 'Stopped', isTracked: false, isDeleted: false, seconds: 900 },
    { phaseId: 'p3', code: 'old', name: 'Old phase', isTracked: true, isDeleted: true, seconds: 300 },
  ],
  unknownModeSeconds: 300,
  noDataSeconds: 300,
};

describe('time breakdown utils', () => {
  it.each([
    ['three equal parts', [1, 1, 1], [34, 33, 33]],
    ['an uneven split', [1800, 900, 300, 300, 300], [50, 25, 9, 8, 8]],
    ['all zeros', [0, 0, 0], [0, 0, 0]],
    ['one value', [42], [100]],
  ])('sharesOf_With_%s_AddsUpToOneHundred', (_name, values, expected) => {
    const shares = sharesOf(values);

    expect(shares).toEqual(expected);
    if (values.some((value) => value > 0)) {
      expect(shares.reduce((sum, share) => sum + share, 0)).toBe(100);
    }
  });

  it.each([
    [0, 0, 0],
    [59, 0, 1],
    [3600, 1, 0],
    [5400, 1, 30],
    [7199, 2, 0],
  ])('durationParts_Of%fSeconds_Gives%iHoursAnd%iMinutes', (seconds, hours, minutes) => {
    expect(durationParts(seconds)).toEqual({ hours, minutes });
  });

  it('toPhaseRows_WithPhasesUnknownModeAndNoData_GivesTheRowsWithSharesThatAddUpTo100', () => {
    const rows = toPhaseRows(PHASES, PHASE_LABELS);

    expect(rows.map((row) => [row.label, row.seconds, row.share])).toEqual([
      ['Running', 1800, 50],
      ['Stopped', 900, 25],
      ['Old phase (deleted)', 300, 9],
      ['Unknown mode', 300, 8],
      ['No data', 300, 8],
    ]);
    expect(rows.reduce((sum, row) => sum + row.share, 0)).toBe(100);
  });

  it('toPhaseRows_WithAnUntrackedPhase_MarksItAsNotTracked', () => {
    const rows = toPhaseRows(PHASES, PHASE_LABELS);

    expect(rows.filter((row) => row.isUntracked).map((row) => row.key)).toEqual(['p2']);
  });

  it('toStateRows_WithStatesAndTheTimeBeforeTheCreation_MapsTheStateValuesToTheirLabels', () => {
    const response: MachineStateTimesResponse = {
      states: [
        { state: 'operational', seconds: 2400 },
        { state: 'inMaintenance', seconds: 600 },
        { state: 'outOfService', seconds: 0 },
      ],
      beforeCreationSeconds: 600,
    };

    const rows = toStateRows(response, {
      stateOf: (value) => `label:${value}`,
      beforeCreation: 'Before creation',
    });

    expect(rows.map((row) => [row.label, row.share])).toEqual([
      ['label:operational', 67],
      ['label:inMaintenance', 17],
      ['label:outOfService', 0],
      ['Before creation', 16],
    ]);
    expect(rows.reduce((sum, row) => sum + row.share, 0)).toBe(100);
  });

  it('colorTokensOf_WithItemsAndRestRows_GivesTheItemsThePaletteInOrderAndTheRestRowsNeutralTokens', () => {
    const rows = toPhaseRows(PHASES, PHASE_LABELS);

    const tokens = colorTokensOf(rows);

    expect(tokens.slice(0, 3)).toEqual(SEGMENT_COLOR_TOKENS.slice(0, 3));
    expect(tokens[3]).not.toBe(tokens[4]);
    expect(rows[3].kind).toBe(TimeRowKind.UnknownMode);
  });
});
