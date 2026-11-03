// @ts-check
const eslint = require('@eslint/js');
const { defineConfig } = require('eslint/config');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');
const prettier = require('eslint-config-prettier/flat');

const ZONELESS_MESSAGE = 'The app is zoneless. Show state through signals.';
const ALIAS_MESSAGE = 'Use @app/, @shared/, or the feature alias.';
const FEATURE_ALIAS_MESSAGE = 'Use @shared/ or @<feature>/. Each new feature adds its @app/<feature>/* pattern.';
const LAYOUT_MESSAGE = 'Group by intent, not by technical type.';

const RESTRICTED_IMPORT_PATTERNS = [
  { group: ['rxjs/Rx'], message: 'Import from rxjs.' },
  { group: ['zone.js', 'zone.js/*'], message: ZONELESS_MESSAGE },
  { group: ['@angular/material'], message: 'Use PrimeNG.' },
  { group: ['../*'], message: ALIAS_MESSAGE },
  { group: ['@app/shared/*', '@app/home/*', '@app/fallback/*'], message: FEATURE_ALIAS_MESSAGE },
  {
    group: ['**/services/*', '**/models/*', '**/components/*', '**/utils/*', '**/helpers/*', '**/stores/*'],
    message: LAYOUT_MESSAGE,
  },
];

const RESTRICTED_SYNTAX = [
  {
    selector: 'BinaryExpression[operator=/^[!=]==?$/] > Literal[raw=/^[1-5][0-9][0-9]$/]',
    message: 'Use HttpStatusCode from @angular/common/http for an HTTP status.',
  },
  { selector: 'TSEnumDeclaration[const=true]', message: 'Use a string enum, not a const enum.' },
  {
    selector: "MemberExpression[object.name='ChangeDetectionStrategy'][property.name='Eager']",
    message: 'Components use the OnPush default.',
  },
  { selector: 'TSEnumMember > Literal[raw=/^-?\\d/]', message: 'Use a string enum, not a numeric enum.' },
];

const ZONELESS_PATH = {
  name: '@angular/core',
  importNames: ['provideZoneChangeDetection', 'NgZone'],
  message: ZONELESS_MESSAGE,
};

const TEMPORAL_PATH = {
  name: 'temporal-polyfill',
  message: 'Import Temporal from @shared/time/temporal.',
};

const FORMS_PATH = {
  name: '@angular/forms',
  importNames: ['ReactiveFormsModule', 'FormBuilder', 'FormGroup', 'FormControl', 'FormArray'],
  message: 'Use Signal Forms (@angular/forms/signals).',
};

const CLOCK_MESSAGE = 'Inject CLOCK.';
const CLOCK_SYNTAX = [
  { selector: "NewExpression[callee.name='Date'][arguments.length=0]", message: CLOCK_MESSAGE },
  { selector: "CallExpression[callee.object.name='Date'][callee.property.name='now']", message: CLOCK_MESSAGE },
  { selector: "MemberExpression[object.name='Temporal'][property.name='Now']", message: CLOCK_MESSAGE },
];

const PROVIDED_IN_SYNTAX = {
  selector: "Property[key.name='providedIn']",
  message: 'Provide the store on the route or the component.',
};

module.exports = defineConfig([
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
        { paths: [TEMPORAL_PATH, FORMS_PATH, ZONELESS_PATH], patterns: RESTRICTED_IMPORT_PATTERNS },
      ],
      'no-restricted-syntax': ['error', ...RESTRICTED_SYNTAX, ...CLOCK_SYNTAX],
      'no-restricted-globals': [
        'error',
        { name: 'localStorage', message: 'Inject BROWSER_STORAGE.' },
        { name: 'sessionStorage', message: 'Inject BROWSER_STORAGE.' },
      ],
    },
  },
  {
    files: ['src/app/**/*.ts'],
    ignores: ['src/app/shared/**'],
    rules: {
      'no-restricted-syntax': ['error', ...RESTRICTED_SYNTAX, ...CLOCK_SYNTAX, PROVIDED_IN_SYNTAX],
    },
  },
  {
    files: ['src/app/shared/time/temporal.ts'],
    rules: {
      'no-restricted-imports': ['error', { paths: [FORMS_PATH, ZONELESS_PATH], patterns: RESTRICTED_IMPORT_PATTERNS }],
    },
  },
  {
    files: ['src/app/shared/time/clock.ts'],
    rules: {
      'no-restricted-syntax': ['error', ...RESTRICTED_SYNTAX],
    },
  },
  {
    files: ['src/app/shared/storage/browser-storage.ts'],
    rules: {
      'no-restricted-globals': 'off',
    },
  },
  {
    files: ['**/*.html'],
    extends: [angular.configs.templateRecommended, angular.configs.templateAccessibility],
    rules: {},
  },
  prettier,
]);
