import { storybookTest } from '@storybook/addon-vitest/vitest-plugin';
import { playwright } from '@vitest/browser-playwright';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig, mergeConfig } from 'vitest/config';
import { createViteConfig } from './vite.config';

const dirname = path.dirname(fileURLToPath(import.meta.url));

export default mergeConfig(
  createViteConfig(),
  defineConfig({
    test: {
      projects: [
        {
          extends: true,
          test: {
            name: 'unit',
            environment: 'node',
            include: ['src/**/*.test.{ts,tsx}'],
            setupFiles: ['./src/test/setup.ts'],
          },
        },
        {
          extends: true,
          plugins: [
            storybookTest({
              configDir: path.join(dirname, '.storybook'),
              storybookScript: 'npm run storybook -- --no-open',
            }),
          ],
          optimizeDeps: {
            entries: ['src/**/*.stories.tsx'],
            include: ['react-router-dom', 'zustand', 'aria-query', 'lz-string', 'pretty-format'],
          },
          test: {
            name: 'storybook',
            testTimeout: 120000,
            retry: 2,
            isolate: false,
            browser: {
              enabled: true,
              provider: playwright({}),
              headless: true,
              instances: [{ browser: 'chromium' }],
            },
            setupFiles: ['./.storybook/vitest.setup.ts'],
          },
        },
      ],
      coverage: {
        provider: 'v8',
        include: ['src/**/*.{ts,tsx}'],
        reporter: [['text-summary', { file: 'summary.txt' }], 'json-summary', 'html'],
        reportsDirectory: './coverage',
      },
    },
  }),
);
