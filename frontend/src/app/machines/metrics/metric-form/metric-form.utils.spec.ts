import { quantityKindKey, toUnitGroups } from '@machines/metrics/metric-form/metric-form.utils';

describe('toUnitGroups', () => {
  it('toUnitGroups_WithUnitWithoutAbbreviation_MarksItUnitless', () => {
    const groups = toUnitGroups({
      kinds: [
        {
          name: 'Scalar',
          units: [{ name: 'Amount', abbreviation: '' }],
        },
        {
          name: 'Temperature',
          units: [{ name: 'DegreeCelsius', abbreviation: '\u00B0C' }],
        },
      ],
    });

    expect(groups[0].items).toEqual([{ label: '', value: 'Scalar|Amount', isUnitless: true }]);
    expect(groups[1].items).toEqual([{ label: '\u00B0C', value: 'Temperature|DegreeCelsius', isUnitless: false }]);
  });
});

describe('quantityKindKey', () => {
  it('quantityKindKey_WithCamelCaseName_ReturnsUpperSnakeKey', () => {
    expect(quantityKindKey('RotationalSpeed')).toBe('METRICS.QUANTITY_KINDS.ROTATIONAL_SPEED');
    expect(quantityKindKey('Temperature')).toBe('METRICS.QUANTITY_KINDS.TEMPERATURE');
  });
});
