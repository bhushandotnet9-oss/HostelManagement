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

test('renaming a hostel updates the list', async ({ page }) => {
  await signUpNewOwner(page);

  await page.goto('/hostels');
  await page.getByLabel('Name').fill('Sunrise Hostel');
  await page.getByLabel('Location').fill('Pune');
  await page.getByRole('button', { name: 'Add hostel' }).click();

  // Located by position, not by text: in edit mode the hostel name lives in an
  // input value rather than the row's text, so a hasText filter stops matching.
  const hostelRow = page.locator('li.list-group-item').first();
  await expect(hostelRow).toContainText('Sunrise Hostel');

  // Scoped to the row: the add-hostel form above has Name/Location labels too.
  await hostelRow.getByRole('button', { name: 'Edit' }).click();
  await hostelRow.getByLabel('Name').fill('Sunset Hostel');
  await hostelRow.getByLabel('Location').fill('Mumbai');
  await hostelRow.getByRole('button', { name: 'Save' }).click();

  await expect(page.getByRole('button', { name: /Sunset Hostel - Mumbai/ })).toBeVisible();
  await expect(page.getByRole('button', { name: /Sunrise Hostel/ })).toHaveCount(0);
});

test('deleting a vacant cot drops the occupancy total', async ({ page }) => {
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
  await expect(page.getByText('Total cots: 1 | Occupied: 0 | Vacant: 1')).toBeVisible();

  // The delete handlers go through window.confirm, so accept it.
  page.on('dialog', (dialog) => dialog.accept());

  const cotRow = page.locator('li.list-group-item', { hasText: 'Cot A - Vacant' });
  await cotRow.getByRole('button', { name: 'Delete' }).click();

  await expect(page.getByText('Cot A - Vacant')).toHaveCount(0);
  await expect(page.getByText('Total cots: 0 | Occupied: 0 | Vacant: 0')).toBeVisible();
});

test('deleting a hostel that still has rooms is refused with a reason', async ({ page }) => {
  await signUpNewOwner(page);

  await page.goto('/hostels');
  await page.getByLabel('Name').fill('Sunrise Hostel');
  await page.getByLabel('Location').fill('Pune');
  await page.getByRole('button', { name: 'Add hostel' }).click();

  await page.getByRole('button', { name: /Sunrise Hostel/ }).click();
  await page.getByLabel('Room name').fill('Room 101');
  await page.getByRole('button', { name: 'Add room' }).click();
  await expect(page.getByRole('heading', { name: 'Room 101' })).toBeVisible();

  page.on('dialog', (dialog) => dialog.accept());

  const hostelRow = page.locator('li.list-group-item').first();
  await hostelRow.getByRole('button', { name: 'Delete' }).click();

  await expect(page.getByText('This hostel still has rooms. Delete them first.')).toBeVisible();
  await expect(page.getByRole('button', { name: /Sunrise Hostel/ })).toBeVisible();
});
