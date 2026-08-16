import { execFileSync } from 'node:child_process';
import path from 'node:path';

const root = path.resolve(import.meta.dirname, '../..');

export default async function globalTeardown() {
  execFileSync('docker', ['compose', '-f', 'docker-compose.lite.yml', 'down', '-v', '--remove-orphans'], {
    cwd: root,
    stdio: 'inherit',
  });
}
