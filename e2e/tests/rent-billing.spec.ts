import { test, expect } from '@playwright/test';
import { signUpNewOwner } from './helpers';

async function createVacantCot(page: import('@playwright/test').Page) {
  await page.goto('/hostels');
  await page.getByLabel('Name').fill('Sunrise Hostel');
  await page.getByLabel('Location').fill('Pune');
  await page.getByRole('button', { name: 'Add hostel' }).click();

  await page.getByRole('button', { name: /Sunrise Hostel/ }).click();
  await page.getByLabel('Room name').fill('Room 101');
  await page.getByRole('button', { name: 'Add room' }).click();

  await page.getByLabel('Cot label').fill('Cot A');
  await page.getByRole('button', { name: 'Add cot' }).click();
  await expect(page.getByText('Cot A - Vacant')).toBeVisible();
}

async function createResidentAssignedToCot(page: import('@playwright/test').Page) {
  await createVacantCot(page);

  await page.goto('/residents');
  await page.getByLabel('Name').fill('Ravi Kumar');
  await page.getByLabel('Contact number').fill('9999999999');
  await page.getByLabel('Aadhaar number').fill('123412341234');
  await page.getByLabel('Move-in date').fill('2026-01-15');
  await page.getByRole('button', { name: 'Add resident' }).click();

  const residentRow = page.locator('li.resident', { hasText: 'Ravi Kumar' });
  await expect(residentRow).toBeVisible();

  await residentRow.getByLabel('Cot').selectOption({ label: 'Sunrise Hostel / Room 101 / Cot A' });
  await residentRow.getByLabel('Rent amount').fill('5000');
  await residentRow.getByLabel('Agreed deposit amount').fill('10000');
  await residentRow.getByLabel('Billing cycle').selectOption('Monthly');
  await residentRow.getByLabel('Move-in date').fill('2026-01-15');
  await residentRow.getByRole('button', { name: 'Assign cot' }).click();
  await expect(residentRow.getByText('Rent: 5000')).toBeVisible();

  return residentRow;
}

test('logging a rent payment marks the due period paid', async ({ page }) => {
  await signUpNewOwner(page);
  const residentRow = await createResidentAssignedToCot(page);

  await residentRow.getByRole('button', { name: 'View dues' }).click();

  const firstDueRow = residentRow.locator('tbody tr').first();
  await expect(firstDueRow).not.toContainText('Paid');

  await firstDueRow.getByPlaceholder('Amount').fill('5000');
  await firstDueRow.locator('input[type="date"]').fill('2026-01-16');
  await firstDueRow.getByRole('button', { name: 'Log payment' }).click();

  await expect(residentRow.locator('tbody tr').first()).toContainText('Paid');
});
