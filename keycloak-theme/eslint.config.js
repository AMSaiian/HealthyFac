// @ts-check
import eslint from '@eslint/js';
import { defineConfig } from 'eslint/config';
import tseslint from 'typescript-eslint';
import angular from 'angular-eslint';
import prettier from 'eslint-config-prettier/flat';

const ZONELESS_MESSAGE = 'The theme is zoneless. Show state through signals.';
const HTTP_STATUS_MESSAGE = 'Use HttpStatusCode from @angular/common/http for an HTTP status.';

const HOST_DISPLAY_SYNTAX = {
  selector:
    "Decorator[expression.callee.name='Component'] > CallExpression > ObjectExpression:not(:has(Property[key.name='host'] > ObjectExpression > Property[key.name='class'][value.value=/(^|\\s)(flex|inline-flex|grid|block|contents|hidden)(\\s|$)/]))",
  message: 'Set the host display in host: { class }.',
};

const RESTRICTED_SYNTAX = [
  {
    selector: 'BinaryExpression[operator=/^[!=]==?$/] > Literal[raw=/^[1-5][0-9][0-9]$/]',
    message: HTTP_STATUS_MESSAGE,
  },
  { selector: 'TSEnumDeclaration[const=true]', message: 'Use a string enum, not a const enum.' },
  {
    selector: "MemberExpression[object.name='ChangeDetectionStrategy'][property.name='Eager']",
    message: 'Components use the OnPush default.',
  },
  { selector: 'TSEnumMember > Literal[raw=/^-?\\d/]', message: 'Use a string enum, not a numeric enum.' },
  HOST_DISPLAY_SYNTAX,
];

export default defineConfig([
  {
    ignores: ['dist/**', 'build_keycloak/**', 'src/kc.gen.ts'],
  },
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      tseslint.configs.recommended,
      tseslint.configs.stylistic,
      angular.configs.tsRecommended,
    ],
    processor: angular.processInlineTemplates,
    languageOptions: { parserOptions: { projectService: true } },
    rules: {
      '@angular-eslint/directive-selector': ['error', { type: 'attribute', prefix: 'hf', style: 'camelCase' }],
      '@angular-eslint/component-selector': ['error', { type: 'element', prefix: 'hf', style: 'kebab-case' }],
      '@angular-eslint/prefer-inject': 'error',
      curly: ['error', 'all'],
      '@typescript-eslint/no-deprecated': 'error',
      '@typescript-eslint/naming-convention': [
        'error',
        { selector: 'memberLike', modifiers: ['private'], format: ['camelCase'], leadingUnderscore: 'require' },
      ],
      '@typescript-eslint/consistent-type-assertions': ['error', { assertionStyle: 'as' }],
      '@typescript-eslint/explicit-function-return-type': [
        'error',
        { allowExpressions: true, allowTypedFunctionExpressions: true },
      ],
      '@typescript-eslint/consistent-type-imports': [
        'error',
        { prefer: 'type-imports', fixStyle: 'separate-type-imports' },
      ],
      '@typescript-eslint/prefer-enum-initializers': 'error',
      '@typescript-eslint/no-mixed-enums': 'error',
      'sort-imports': ['error', { ignoreDeclarationSort: true, ignoreCase: true }],
      'no-restricted-imports': [
        'error',
        {
          paths: [
            { name: 'rxjs/Rx', message: 'Import from rxjs.' },
            { name: 'zone.js', message: ZONELESS_MESSAGE },
            { name: '@angular/material', message: 'Use PrimeNG.' },
            {
              name: '@angular/core',
              importNames: ['provideZoneChangeDetection', 'NgZone'],
              message: ZONELESS_MESSAGE,
            },
            {
              name: 'primeng/button',
              importNames: ['Button', 'ButtonModule', 'ButtonLabel', 'ButtonIcon'],
              message: 'Use the pButton directive (ButtonDirective).',
            },
          ],
        },
      ],
      'no-restricted-syntax': ['error', ...RESTRICTED_SYNTAX],
      'no-restricted-globals': [
        'error',
        { name: 'localStorage', message: 'The theme keeps no browser storage.' },
        { name: 'sessionStorage', message: 'The theme keeps no browser storage.' },
      ],
    },
  },
  {
    files: ['src/**/*.spec.ts'],
    rules: {
      'no-restricted-syntax': 'off',
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {
      '@angular-eslint/template/prefer-ngsrc': 'error',
    },
  },
  prettier,
]);
