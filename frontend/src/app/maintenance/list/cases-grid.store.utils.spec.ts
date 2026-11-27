import { AgentRunState, CaseColumn, CaseState, SignalSeverity } from '@maintenance/case';
import { INITIAL_FILTER, INITIAL_GRID, toSearchBody } from '@maintenance/list/cases-grid.store.utils';
import {
  AI_UNAVAILABLE_OPTION,
  FILTER_READERS,
  FILTER_WRITERS,
  toLastRunFilter,
} from '@maintenance/list/cases-grid/cases-grid.utils';

describe('cases grid store utils', { tags: ['Cases'] }, () => {
  it('toSearchBody_NoFilter_LeavesTheFilterEmptyAndKeepsTheSort', () => {
    const body = toSearchBody({ offset: 0, limit: 25, sort: INITIAL_GRID.sort, filter: INITIAL_FILTER });

    expect(body.filter).toEqual({
      machineIds: undefined,
      states: undefined,
      severities: undefined,
      lastRunStates: undefined,
      isAiUnavailable: undefined,
    });
    expect(body.sort).toEqual({ column: 'openedAt', direction: 'desc' });
  });

  it('toSearchBody_StatesAndNoSort_SendsTheStatesAndOmitsTheSort', () => {
    const body = toSearchBody({
      offset: 50,
      limit: 25,
      sort: null,
      filter: { ...INITIAL_FILTER, states: [CaseState.Open] },
    });

    expect(body).toEqual({
      offset: 50,
      limit: 25,
      filter: {
        states: { values: ['open'] },
        severities: undefined,
        lastRunStates: undefined,
        isAiUnavailable: undefined,
      },
    });
  });

  it('toSearchBody_SeveritiesRunStatesAndAiUnavailable_SendsAllThree', () => {
    const body = toSearchBody({
      offset: 0,
      limit: 25,
      sort: null,
      filter: {
        ...INITIAL_FILTER,
        severities: [SignalSeverity.Alarm],
        lastRunStates: [AgentRunState.Skipped, AgentRunState.Failed],
        isAiUnavailable: true,
      },
    });

    expect(body.filter).toMatchObject({
      severities: { values: ['alarm'] },
      lastRunStates: { values: ['skipped', 'failed'] },
      isAiUnavailable: true,
    });
  });

  it.each([
    ['NoValue', [], [], null],
    ['OneRunState', ['completed'], [AgentRunState.Completed], null],
    ['TheAiUnavailableOption', [AI_UNAVAILABLE_OPTION], [], true],
    ['StateAndOption', ['skipped', AI_UNAVAILABLE_OPTION], [AgentRunState.Skipped], true],
    ['UnknownValue', ['other'], [], null],
  ])('toLastRunFilter_With%s_SplitsTheStatesAndTheFlag', (_name, values, states, flag) => {
    expect(toLastRunFilter(values)).toEqual({ lastRunStates: states, isAiUnavailable: flag });
  });

  it('filterReaders_SeverityAndLastRunValues_MapToTheirFilterFields', () => {
    expect(FILTER_READERS[CaseColumn.Severity](['warning'])).toEqual({ severities: ['warning'] });
    expect(FILTER_READERS[CaseColumn.LastRun](['failed', AI_UNAVAILABLE_OPTION])).toEqual({
      lastRunStates: [AgentRunState.Failed],
      isAiUnavailable: true,
    });
  });

  it('filterWriters_AppliedFilter_GiveTheMenuValuesAndNullWhenNoFilter', () => {
    const applied = {
      ...INITIAL_FILTER,
      severities: [SignalSeverity.Warning],
      lastRunStates: [AgentRunState.Skipped],
      isAiUnavailable: true as const,
    };

    expect(FILTER_WRITERS[CaseColumn.Severity](applied)).toEqual(['warning']);
    expect(FILTER_WRITERS[CaseColumn.LastRun](applied)).toEqual(['skipped', AI_UNAVAILABLE_OPTION]);
    expect(FILTER_WRITERS[CaseColumn.Severity](INITIAL_FILTER)).toBeNull();
    expect(FILTER_WRITERS[CaseColumn.LastRun](INITIAL_FILTER)).toBeNull();
  });

  it('toSearchBody_WithPickedMachines_SendsTheirIdsAsASetFilter', () => {
    const filter = { ...INITIAL_FILTER, machineIds: ['m1', 'm2'] };

    const body = toSearchBody({ offset: 0, limit: 25, sort: null, filter });

    expect(body.filter?.machineIds).toEqual({ ids: ['m1', 'm2'] });
  });

  it('filterReadersAndWriters_ForTheMachineColumn_MapThePickedIdsBothWays', () => {
    const patch = FILTER_READERS[CaseColumn.Machine](['m1']);

    expect(patch).toEqual({ machineIds: ['m1'] });
    expect(FILTER_WRITERS[CaseColumn.Machine]({ ...INITIAL_FILTER, ...patch })).toEqual(['m1']);
    expect(FILTER_WRITERS[CaseColumn.Machine](INITIAL_FILTER)).toBeNull();
  });
});
