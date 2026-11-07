import { test, expect } from '@playwright/test';

test('owner can sign up and log in end-to-end', async ({ page }) => {
  const uniqueEmail = `owner-${Date.now()}@example.com`;

  await page.goto('/setup');
  await page.getByLabel('Organization Name').fill('Pune Hostels E2E');
  await page.getByLabel('Email').fill(uniqueEmail);
  await page.getByLabel('Password').fill('P@ssw0rd!');
  await page.getByRole('button', { name: 'Create account' }).click();

  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByRole('heading', { level: 1 })).toContainText(uniqueEmail);

  await page.getByRole('button', { name: 'Log out' }).click();
  await expect(page).toHaveURL(/\/login$/);

  await page.getByLabel('Email').fill(uniqueEmail);
  await page.getByLabel('Password').fill('P@ssw0rd!');
  await page.getByRole('button', { name: 'Log in' }).click();

  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(page.getByRole('heading', { level: 1 })).toContainText(uniqueEmail);
});

test('invalid login credentials are rejected', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('Email').fill('no-such-owner@example.com');
  await page.getByLabel('Password').fill('WrongPassword');
  await page.getByRole('button', { name: 'Log in' }).click();

  await expect(page.locator('.toast')).toHaveText('Invalid email or password.');
  await expect(page).toHaveURL(/\/login$/);
});
