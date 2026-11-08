import { test, expect, Page } from '@playwright/test';
import { signUpNewOwner } from './helpers';

async function createHostelWithCot(page: Page, hostelName: string, roomName: string, cotLabel: string) {
  await page.goto('/hostels');
  await page.getByLabel('Name').fill(hostelName);
  await page.getByLabel('Location').fill('Pune');
  await page.getByRole('button', { name: 'Add hostel' }).click();

  await page.getByRole('button', { name: new RegExp(hostelName) }).click();
  await page.getByLabel('Room name').fill(roomName);
  await page.getByRole('button', { name: 'Add room' }).click();

  await page.getByLabel('Cot label').fill(cotLabel);
  await page.getByRole('button', { name: 'Add cot' }).click();
  await expect(page.getByText(`${cotLabel} - Vacant`)).toBeVisible();
}

async function createOverdueResident(page: Page, hostelName: string, roomName: string, cotLabel: string, residentName: string) {
  await page.goto('/residents');
  await page.getByLabel('Name').fill(residentName);
  await page.getByLabel('Contact number').fill('9999999999');
  await page.getByLabel('Aadhaar number').fill('123412341234');
  await page.getByLabel('Move-in date').fill('2026-01-15');
  await page.getByRole('button', { name: 'Add resident' }).click();

  const residentRow = page.locator('li.resident', { hasText: residentName });
  await residentRow.getByLabel('Cot').selectOption({ label: `${hostelName} / ${roomName} / ${cotLabel}` });
  await residentRow.getByLabel('Rent amount').fill('5000');
  await residentRow.getByLabel('Deposit amount').fill('10000');
  await residentRow.getByLabel('Billing cycle').selectOption('Monthly');
  await residentRow.getByLabel('Move-in date').fill('2026-01-15');
  await residentRow.getByRole('button', { name: 'Assign cot' }).click();
  await expect(residentRow.getByText('Rent: 5000')).toBeVisible();
}

test('owner dashboard shows correct aggregates across both hostels', async ({ page }) => {
  await signUpNewOwner(page);

  await createHostelWithCot(page, 'Hostel Alpha', 'Room A1', 'Cot A1');
  await createOverdueResident(page, 'Hostel Alpha', 'Room A1', 'Cot A1', 'Resident Alpha');

  await createHostelWithCot(page, 'Hostel Beta', 'Room B1', 'Cot B1');
  await createOverdueResident(page, 'Hostel Beta', 'Room B1', 'Cot B1', 'Resident Beta');

  await page.goto('/hostels');
  await page.getByRole('button', { name: /Hostel Beta/ }).click();
  await page.getByLabel('Room name').fill('Room B2');
  await page.getByRole('button', { name: 'Add room' }).click();
  await page.getByLabel('Cot label').fill('Cot B2');
  await page.getByRole('button', { name: 'Add cot' }).click();
  await expect(page.getByText('Cot B2 - Vacant')).toBeVisible();

  const currentMonth = new Date().toISOString().slice(0, 7);
  await page.goto('/expenses');
  await page.getByRole('button', { name: 'Hostel Alpha' }).click();
  await page.getByLabel('Category').selectOption('Electricity');
  await page.getByLabel('Amount').fill('900');
  await page.getByLabel('Date').fill(`${currentMonth}-03`);
  await page.getByRole('button', { name: 'Add expense' }).click();

  // Confirm the expense was actually recorded (same-page check, so the
  // create request isn't aborted by an immediate navigation) before moving on.
  await page.getByLabel('From').fill(`${currentMonth}-01`);
  await page.getByLabel('To').fill(`${currentMonth}-28`);
  await page.getByRole('button', { name: 'View breakdown' }).click();
  // Scoped to the breakdown card: the entries table above also lists this expense.
  const breakdownCard = page.locator('.card', { has: page.getByRole('heading', { name: 'Category breakdown' }) });
  await expect(breakdownCard.locator('tbody tr', { hasText: 'Electricity' })).toContainText('900');

  await page.goto('/dashboard');

  await expect(page.getByText('Resident Alpha')).toBeVisible();
  await expect(page.getByText('Resident Beta')).toBeVisible();

  const occupancySection = page.locator('section', { has: page.getByRole('heading', { name: 'Occupancy' }) });
  await expect(occupancySection).toContainText('Hostel Alpha');
  await expect(occupancySection).toContainText('Hostel Beta');
  // Combined row: 3 total cots across both hostels (1 + 2), 2 occupied, 1 vacant.
  await expect(occupancySection.locator('tr', { hasText: 'Combined' })).toContainText('3');

  const expensesSection = page.locator('section', { has: page.getByRole('heading', { name: 'Expenses this month by category' }) });
  await expect(expensesSection).toContainText('Hostel Alpha');
  await expect(expensesSection).toContainText('Electricity');
  await expect(expensesSection).toContainText('900');
});
