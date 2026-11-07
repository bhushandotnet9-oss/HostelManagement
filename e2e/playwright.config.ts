import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:5048',
  },
  webServer: {
    // Run without the dev launch profile so the SpaProxy hosting startup
    // assembly (which proxies to a cold-starting `ng serve` and shows an
    // interstitial "Launching the SPA proxy..." page) never activates;
    // instead the server serves the Angular app's production build via
    // static web assets, which is both faster and deterministic for e2e.
    command: 'dotnet run --no-launch-profile',
    cwd: '../HostelManagement.Server',
    url: 'http://localhost:5048/api/health',
    reuseExistingServer: true,
    timeout: 180_000,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Production',
      ASPNETCORE_URLS: 'http://localhost:5048',
    },
  },
});
