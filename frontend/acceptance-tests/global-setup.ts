import { request } from '@playwright/test';
import { execFileSync } from 'node:child_process';
import path from 'node:path';

const root = path.resolve(import.meta.dirname, '../..');

async function waitFor(url: string) {
  const api = await request.newContext();
  const deadline = Date.now() + 60_000;
  while (Date.now() < deadline) {
    try {
      const response = await api.get(url, { timeout: 2000 });
      if (response.ok()) {
        await api.dispose();
        return;
      }
    } catch {
      await new Promise(resolve => setTimeout(resolve, 1000));
    }
  }
  await api.dispose();
  throw new Error(`Timed out waiting for ${url}`);
}

export default async function globalSetup() {
  execFileSync('docker', ['compose', '-f', 'docker-compose.lite.yml', 'down', '-v', '--remove-orphans'], {
    cwd: root,
    stdio: 'inherit',
  });
  execFileSync('docker', ['compose', '-f', 'docker-compose.lite.yml', 'up', '-d'], {
    cwd: root,
    stdio: 'inherit',
    env: process.env,
  });

  await waitFor('http://localhost:5051/server/health');
  await waitFor('http://localhost:3000');
}
