import { test, expect } from '@playwright/test';
import { signUpNewOwner } from './helpers';

test('creating a hostel, room, and cot is reflected in the occupancy overview', async ({ page }) => {
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
  await expect(page.getByText('Total cots: 1 | Occupied: 0 | Vacant: 1')).toBeVisible();
});
