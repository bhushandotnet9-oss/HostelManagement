import { test, expect } from '@playwright/test';
import { signUpNewOwner } from './helpers';

test('logging an expense is reflected in the hostel category breakdown', async ({ page }) => {
  await signUpNewOwner(page);

  await page.goto('/hostels');
  await page.getByLabel('Name').fill('Sunrise Hostel');
  await page.getByLabel('Location').fill('Pune');
  await page.getByRole('button', { name: 'Add hostel' }).click();
  await expect(page.getByRole('button', { name: /Sunrise Hostel/ })).toBeVisible();

  await page.goto('/expenses');
  await page.getByRole('button', { name: 'Sunrise Hostel' }).click();

  await page.getByLabel('Category').selectOption('Electricity');
  await page.getByLabel('Amount').fill('1200');
  await page.getByLabel('Date').fill('2026-01-10');
  await page.getByRole('button', { name: 'Add expense' }).click();

  await page.getByLabel('From').fill('2026-01-01');
  await page.getByLabel('To').fill('2026-01-31');
  await page.getByRole('button', { name: 'View breakdown' }).click();

  const row = page.locator('tbody tr', { hasText: 'Electricity' });
  await expect(row).toContainText('1200');
});
