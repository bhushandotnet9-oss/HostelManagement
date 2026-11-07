import { Page, expect } from '@playwright/test';

export async function signUpNewOwner(page: Page, orgName = 'Pune Hostels E2E'): Promise<string> {
  const uniqueEmail = `owner-${Date.now()}-${Math.floor(Math.random() * 100000)}@example.com`;

  await page.goto('/setup');
  await page.getByLabel('Organization Name').fill(orgName);
  await page.getByLabel('Email').fill(uniqueEmail);
  await page.getByLabel('Password').fill('P@ssw0rd!');
  await page.getByRole('button', { name: 'Create account' }).click();

  await expect(page).toHaveURL(/\/dashboard$/);

  return uniqueEmail;
}
