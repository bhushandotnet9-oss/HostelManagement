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

  // Scoped to the breakdown card: the entries table above also lists this expense.
  const breakdownCard = page.locator('.card', { has: page.getByRole('heading', { name: 'Category breakdown' }) });
  await expect(breakdownCard.locator('tbody tr', { hasText: 'Electricity' })).toContainText('1200');
});

test('editing then deleting an expense entry updates the list and breakdown', async ({ page }) => {
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

  const entryRow = page.locator('tr.expense-row', { hasText: 'Electricity' });
  await expect(entryRow).toContainText('1200');

  // Correct the amount. Labels are scoped to the row's inline edit form.
  await entryRow.getByRole('button', { name: 'Edit' }).click();
  await entryRow.getByLabel('Amount').fill('1500');
  await entryRow.getByRole('button', { name: 'Save' }).click();

  await expect(page.locator('tr.expense-row', { hasText: 'Electricity' })).toContainText('1500');
  const breakdownCard = page.locator('.card', { has: page.getByRole('heading', { name: 'Category breakdown' }) });
  await expect(breakdownCard.locator('tbody tr', { hasText: 'Electricity' })).toContainText('1500');

  // Then remove it entirely.
  page.on('dialog', (dialog) => dialog.accept());
  await page.locator('tr.expense-row', { hasText: 'Electricity' }).getByRole('button', { name: 'Delete' }).click();

  await expect(page.locator('tr.expense-row')).toHaveCount(0);
});
