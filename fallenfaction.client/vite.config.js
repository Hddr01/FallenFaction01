import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-vue';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';
import vueDevTools from 'vite-plugin-vue-devtools';
import tailwindcss from '@tailwindcss/vite';
import { sentryVitePlugin } from '@sentry/vite-plugin'; // ← NEW

const baseFolder =
  env.APPDATA !== undefined && env.APPDATA !== ''
    ? `${env.APPDATA}/ASP.NET/https`
    : `${env.HOME}/.aspnet/https`;

const certificateName = "fallenfaction.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(baseFolder)) {
  fs.mkdirSync(baseFolder, { recursive: true });
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
  console.log('Generating frontend certificates...');
  child_process.spawnSync('dotnet', ['dev-certs', 'https', '--clean'], { stdio: 'inherit' });
  child_process.spawnSync('dotnet', ['dev-certs', 'https', '--trust'], { stdio: 'inherit' });

  if (0 !== child_process.spawnSync('dotnet', [
    'dev-certs',
    'https',
    '--export-path',
    certFilePath,
    '--format',
    'Pem',
    '--no-password',
  ], { stdio: 'inherit', }).status) {
    throw new Error("Could not create certificate.");
  }
}

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7217';

console.log(`Proxying API requests to: ${target}`);

export default defineConfig(({ command }) => ({
  plugins: [
    plugin(),
    tailwindcss(),
    vueDevTools({
      enabled: true,
      open: true,
    }),
    // ── Sentry source-map upload ──────────────────────────────────────────────
    // Only active during `npm run build` (SENTRY_AUTH_TOKEN must be set in env).
    // Uploads source maps so Sentry shows readable stack traces, and creates a
    // release so "regressed in release X" features work.
    sentryVitePlugin({
      org: 'o4511149751795712',           // your Sentry org slug (numeric id also works)
      project: 'javascript-vue',          // the Sentry project slug
      // Auth token is read from SENTRY_AUTH_TOKEN env var automatically.
      // Set it in your CI / Docker build args — never hard-code it here.
      authToken: env.SENTRY_AUTH_TOKEN,
      sourcemaps: {
        // Vite puts the built assets here; adjust if you changed `build.outDir`
        assets: './dist/**',
        // Delete source maps from the final bundle after upload so they're
        // not publicly accessible on your server.
        filesToDeleteAfterUpload: './dist/**/*.map',
      },
      release: {
        // Ties the upload to a specific release. Using the git SHA is the
        // simplest approach; pass it as a build arg: --build-arg COMMIT_SHA=$(git rev-parse HEAD)
        name: env.COMMIT_SHA ?? 'development',
      },
      // Don't fail the build if the upload fails (e.g. no auth token in dev)
      errorHandler: (err) => {
        console.warn('Sentry source map upload failed (non-fatal):', err.message);
      },
    }),
  ],
  esbuild: {
    // Strip all console.* calls and debugger statements in production builds.
    // Uses command === 'build' (reliable) instead of NODE_ENV (evaluated too early).
    drop: command === 'build' ? ['console', 'debugger'] : [],
  },
  build: {
    // Required for Sentry to generate source maps during `npm run build`
    sourcemap: true,
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    proxy: {
      '^/api': {
        target,
        secure: false,
        changeOrigin: true,
        logLevel: 'debug'
      },
      '^/auth': {
        target,
        secure: false,
        changeOrigin: true,
        logLevel: 'debug'
      },
      '^/uploads': {
        target,
        secure: false,
        changeOrigin: true,
        logLevel: 'debug'
      }
    },
    port: 49217,
    https: {
      key: fs.readFileSync(keyFilePath),
      cert: fs.readFileSync(certFilePath),
    },
    hmr: {
      clientPort: 49217,
      port: 49217
    },
    fs: {
      strict: false
    }
  }
}));
