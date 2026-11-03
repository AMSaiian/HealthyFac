import { definePreset } from '@primeuix/themes';
import Lara from '@primeuix/themes/lara';

export const HealthyFacPreset = definePreset(Lara, {
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
