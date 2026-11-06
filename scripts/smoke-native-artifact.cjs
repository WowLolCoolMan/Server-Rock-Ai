#!/usr/bin/env node

/* Launch the exact published native executable and require it to stay alive
 * for a short bounded window. This is a startup smoke check, not a claim that
 * every UI workflow or platform dependency has passed. */

const { spawn, execFileSync } = require('node:child_process');
const path = require('node:path');

const executable = process.argv[2];
const holdSeconds = Number(process.env.NATIVE_SMOKE_SECONDS || '8');
const requireWindow = process.env.NATIVE_SMOKE_REQUIRE_WINDOW === '1';

if (!executable) {
  console.error('Usage: node scripts/smoke-native-artifact.cjs <published-executable>');
  process.exit(2);
}
if (!Number.isFinite(holdSeconds) || holdSeconds < 2 || holdSeconds > 60) {
  console.error('NATIVE_SMOKE_SECONDS must be between 2 and 60 seconds.');
  process.exit(2);
}

const resolved = path.resolve(executable);
const child = spawn(resolved, [], {
  cwd: path.dirname(resolved),
  stdio: ['ignore', 'pipe', 'pipe'],
  windowsHide: true
});

let output = '';
const capture = chunk => {
  output += chunk.toString();
  if (output.length > 16_000) output = output.slice(-16_000);
};
child.stdout.on('data', capture);
child.stderr.on('data', capture);

let exited = false;
let smokePassed = false;

const stop = () => {
  if (exited) return;
  if (process.platform === 'win32') {
    try { execFileSync('taskkill', ['/pid', String(child.pid), '/t', '/f'], { stdio: 'ignore' }); }
    catch { child.kill(); }
  } else {
    child.kill('SIGTERM');
  }
};

const timer = setTimeout(() => {
  if (requireWindow && process.platform === 'win32') {
    try {
      const handle = execFileSync('powershell.exe', ['-NoProfile', '-Command', `(Get-Process -Id ${child.pid} -ErrorAction Stop).MainWindowHandle`], { encoding: 'utf8' }).trim();
      if (!handle || handle === '0') {
        console.error(`Native artifact stayed alive but did not expose a top-level window: ${resolved}`);
        stop();
        process.exit(1);
      }
    } catch (error) {
      console.error(`Native artifact window check failed: ${error.message}`);
      stop();
      process.exit(1);
    }
  }
  smokePassed = true;
  stop();
  console.log(`Native artifact passed startup${requireWindow ? ' and window' : ''} smoke for ${holdSeconds}s: ${resolved}`);
  process.exit(0);
}, holdSeconds * 1000);

child.once('error', error => {
  clearTimeout(timer);
  console.error(`Native artifact could not start: ${error.message}`);
  process.exit(1);
});

child.once('exit', (code, signal) => {
  exited = true;
  if (smokePassed) return;
  clearTimeout(timer);
  console.error(`Native artifact exited before the smoke window (code=${code}, signal=${signal}).`);
  if (output.trim()) console.error(output.trim());
  process.exit(1);
});
