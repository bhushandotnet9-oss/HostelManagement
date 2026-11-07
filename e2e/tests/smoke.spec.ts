import { test, expect } from '@playwright/test';

test('app root loads', async ({ page }) => {
  await page.goto('/');
  await expect(page.locator('app-root')).toBeAttached();
  await expect(page).toHaveTitle('HostelmanagementClient');
});
