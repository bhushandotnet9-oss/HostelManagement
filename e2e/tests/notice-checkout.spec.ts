import { test, expect } from '@playwright/test';
import { signUpNewOwner } from './helpers';

test('recording notice and completing checkout flips the cot back to Vacant', async ({ page }) => {
  await signUpNewOwner(page);

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

  await page.goto('/residents');
  await page.getByLabel('Name').fill('Ravi Kumar');
  await page.getByLabel('Contact number').fill('9999999999');
  await page.getByLabel('Aadhaar number').fill('123412341234');
  await page.getByLabel('Move-in date').fill('2026-01-15');
  await page.getByRole('button', { name: 'Add resident' }).click();

  const residentRow = page.locator('li.resident', { hasText: 'Ravi Kumar' });
  await residentRow.getByLabel('Cot').selectOption({ label: 'Sunrise Hostel / Room 101 / Cot A' });
  await residentRow.getByLabel('Rent amount').fill('5000');
  await residentRow.getByLabel('Deposit amount').fill('10000');
  await residentRow.getByLabel('Billing cycle').selectOption('Monthly');
  await residentRow.getByLabel('Move-in date').fill('2026-01-15');
  await residentRow.getByRole('button', { name: 'Assign cot' }).click();
  await expect(residentRow.getByText('Rent: 5000')).toBeVisible();

  await residentRow.getByLabel('Notice date').fill('2026-06-01');
  await residentRow.getByLabel('Notice period (days)').fill('30');
  await residentRow.getByRole('button', { name: 'Record notice' }).click();
  await expect(residentRow).toContainText('ServingNotice');

  await residentRow.getByLabel('Deposit refund amount').fill('10000');
  await residentRow.getByRole('button', { name: 'Complete checkout' }).click();
  await expect(residentRow).toContainText('CheckedOut');

  await page.goto('/hostels');
  await page.getByRole('button', { name: /Sunrise Hostel/ }).click();
  await expect(page.getByText('Cot A - Vacant')).toBeVisible();
});
