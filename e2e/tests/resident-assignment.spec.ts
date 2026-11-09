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

test('assigning a resident to a cot flips it to Occupied', async ({ page }) => {
  await signUpNewOwner(page);
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

  await page.goto('/hostels');
  await page.getByRole('button', { name: /Sunrise Hostel/ }).click();
  await expect(page.getByText('Cot A - Occupied')).toBeVisible();
});

test('correcting a resident updates their details', async ({ page }) => {
  await signUpNewOwner(page);

  await page.goto('/residents');
  await page.getByLabel('Name').fill('Ravi Kumar');
  await page.getByLabel('Contact number').fill('9999999999');
  await page.getByLabel('Aadhaar number').fill('123412341234');
  await page.getByLabel('Move-in date').fill('2026-01-15');
  await page.getByRole('button', { name: 'Add resident' }).click();

  // Scoped to the resident card: the add form above shares these labels.
  const residentRow = page.locator('li.resident', { hasText: 'Ravi Kumar' });
  await residentRow.getByRole('button', { name: 'Edit' }).click();
  await residentRow.getByLabel('Name').fill('Ravi K Kumar');
  await residentRow.getByLabel('Contact number').fill('8888888888');
  await residentRow.getByRole('button', { name: 'Save' }).click();

  await expect(page.locator('li.resident', { hasText: 'Ravi K Kumar' })).toBeVisible();
});

test('deleting a resident with no payments removes them', async ({ page }) => {
  await signUpNewOwner(page);

  await page.goto('/residents');
  await page.getByLabel('Name').fill('Ravi Kumar');
  await page.getByLabel('Contact number').fill('9999999999');
  await page.getByLabel('Aadhaar number').fill('123412341234');
  await page.getByLabel('Move-in date').fill('2026-01-15');
  await page.getByRole('button', { name: 'Add resident' }).click();

  const residentRow = page.locator('li.resident', { hasText: 'Ravi Kumar' });
  await expect(residentRow).toBeVisible();

  page.on('dialog', (dialog) => dialog.accept());
  await residentRow.getByRole('button', { name: 'Delete' }).click();

  await expect(page.locator('li.resident', { hasText: 'Ravi Kumar' })).toHaveCount(0);
});
