import { test, expect } from '@playwright/test';

test.describe('Autenticação', () => {
  test('deve exibir tela de login', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/Fluxo de Caixa/);
    await expect(page.getByRole('heading', { name: /entrar|login/i })).toBeVisible();
  });

  test('deve exibir erro com credenciais inválidas', async ({ page }) => {
    await page.goto('/login');
    await page.fill('[data-testid="email"]', 'usuario-inexistente@test.com');
    await page.fill('[data-testid="password"]', 'senha-errada');
    await page.click('[data-testid="btn-login"]');

    await expect(page.getByText(/credenciais inválidas|usuário ou senha/i)).toBeVisible({
      timeout: 5000
    });
  });

  test('deve fazer login e redirecionar ao dashboard', async ({ page }) => {
    await page.goto('/login');
    await page.fill('[data-testid="email"]', 'admin@fluxocaixa.com');
    await page.fill('[data-testid="password"]', 'Admin@123');
    await page.click('[data-testid="btn-login"]');

    await expect(page).toHaveURL(/dashboard|home/);
    await expect(page.getByText(/Fluxo de Caixa|Dashboard/i)).toBeVisible();
  });

  test('deve fazer logout', async ({ page }) => {
    await page.goto('/login');
    await page.fill('[data-testid="email"]', 'admin@fluxocaixa.com');
    await page.fill('[data-testid="password"]', 'Admin@123');
    await page.click('[data-testid="btn-login"]');

    await page.click('[data-testid="btn-logout"]');
    await expect(page).toHaveURL(/login/);
  });
});
