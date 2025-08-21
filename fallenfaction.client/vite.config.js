import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-vue';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';
import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'

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

  // First, ensure we have fresh certificates
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

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [plugin(), tailwindcss(), vueDevTools({
    enabled: true,
    open: true,
  })],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    proxy: {
      // Proxy all API requests to the ASP.NET Core backend
      '^/api': {
        target,
        secure: false,
        changeOrigin: true,
        // Remove the rewrite - let the path pass through as-is
        logLevel: 'debug'
      },
      // Proxy authentication endpoints
      '^/auth': {
        target,
        secure: false,
        changeOrigin: true,
        logLevel: 'debug'
      },
      // Proxy backend uploaded files ONLY (not frontend static files)
      '^/uploads': {
        target,
        secure: false,
        changeOrigin: true,
        logLevel: 'debug'
      }
      // Remove '^/img' proxy - let frontend serve its own static images
    },
    port: 49217,
    // RE-ENABLE HTTPS - Certificates are now fixed!
    https: {
      key: fs.readFileSync(keyFilePath),
      cert: fs.readFileSync(certFilePath),
    },
    // Fix WebSocket/HMR connection issues
    hmr: {
      clientPort: 49217,
      port: 49217
    },
    // Allow serving static files
    fs: {
      strict: false
    }
  }
})
