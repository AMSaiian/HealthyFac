import { readFileSync } from 'node:fs';
import { defineConfig } from 'vitest/config';

const featureFile = JSON.parse(readFileSync(new URL('../test-features.json', import.meta.url), 'utf-8')) as {
  features: { name: string }[];
};

export default defineConfig({
  test: {
    env: { TZ: 'UTC' },
    tags: featureFile.features.map(({ name }) => ({ name })),
    strictTags: true,
    tagsFilter: process.env['HF_TEST_TAGS'] ? [process.env['HF_TEST_TAGS']] : undefined,
  },
});
