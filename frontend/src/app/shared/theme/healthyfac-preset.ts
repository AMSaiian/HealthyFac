import { definePreset } from '@primeuix/themes';
import Lara from '@primeuix/themes/lara';

export const HealthyFacPreset = definePreset(Lara, {
  components: {
    tag: {
      root: { fontSize: '{text.size.xs}', padding: '0.125rem 0.5rem' },
      colorScheme: {
        light: { secondary: { background: '{surface.200}', color: '{surface.600}' } },
        dark: { secondary: { background: '{surface.700}', color: '{surface.300}' } },
      },
    },
    tabs: {
      tablist: { borderWidth: '0 0 1px 0', background: '{content.background}' },
      tab: {
        borderWidth: '0 0 2px 0',
        margin: '0 0 -1px 0',
        background: '{content.background}',
        hoverBackground: '{content.background}',
        activeBackground: '{content.background}',
      },
      tabpanel: { padding: '0.75rem 0 0 0' },
    },
    datepicker: {
      panel: { padding: '0.5rem' },
      header: { padding: '0 0 0.375rem 0' },
      title: { fontSize: '0.875rem' },
      selectMonth: { padding: '0.25rem 0.375rem', fontSize: '0.875rem' },
      selectYear: { padding: '0.25rem 0.375rem', fontSize: '0.875rem' },
      dayView: { margin: '0.375rem 0 0 0' },
      weekDay: { padding: '0.25rem', fontSize: '0.75rem' },
      date: { width: '1.75rem', height: '1.75rem', padding: '0.25rem', fontSize: '0.875rem' },
    },
  },
  extend: {
    textSize: { xs: '0.6875rem' },
    scheduleBoard: {
      headerBackground: 'light-dark({surface.50}, {surface.800})',
      headerFontWeight: '700',
      borderColor: 'light-dark({content.border.color}, {surface.800})',
    },
    workOrderPriority: {
      low: '{sky.500}',
      medium: '{amber.500}',
      high: '{orange.600}',
      critical: '{rose.700}',
    },
    workOrderStatus: {
      backlog: '{slate.500}',
      staged: '{blue.500}',
      inProgress: '{primary.500}',
      blocked: '{red.500}',
      completed: '{green.500}',
      rejected: '{zinc.400}',
    },
  },
  semantic: {
    primary: {
      50: '#FDF5EF',
      100: '#FAE6D6',
      200: '#F4C9A8',
      300: '#EBA673',
      400: '#E0823F',
      500: '#D2691E',
      600: '#B5561A',
      700: '#934417',
      800: '#763716',
      900: '#602E15',
      950: '#35160A',
    },
    colorScheme: {
      light: {
        primary: {
          color: '{primary.600}',
          contrastColor: '#FFFFFF',
          hoverColor: '{primary.700}',
          activeColor: '{primary.800}',
        },
        surface: {
          0: '#FFFFFF',
          50: '{slate.50}',
          100: '{slate.100}',
          200: '{slate.200}',
          300: '{slate.300}',
          400: '{slate.400}',
          500: '{slate.500}',
          600: '{slate.600}',
          700: '{slate.700}',
          800: '{slate.800}',
          900: '{slate.900}',
          950: '{slate.950}',
        },
      },
      dark: {
        primary: {
          color: '{primary.400}',
          contrastColor: '{surface.950}',
          hoverColor: '{primary.300}',
          activeColor: '{primary.200}',
        },
        surface: {
          0: '#FFFFFF',
          50: '{zinc.50}',
          100: '{zinc.100}',
          200: '{zinc.200}',
          300: '{zinc.300}',
          400: '{zinc.400}',
          500: '{zinc.500}',
          600: '{zinc.600}',
          700: '{zinc.700}',
          800: '{zinc.800}',
          900: '{zinc.900}',
          950: '{zinc.950}',
        },
      },
    },
  },
});
