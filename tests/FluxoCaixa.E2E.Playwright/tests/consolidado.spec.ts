import { test, expect, Page } from '@playwright/test';

async function login(page: Page) {
  await page.goto('/login');
  await page.fill('[data-testid="email"]', 'admin@fluxocaixa.com');
  await page.fill('[data-testid="password"]', 'Admin@123');
  await page.click('[data-testid="btn-login"]');
  await page.waitForURL(/dashboard|consolidado/, { timeout: 10_000 });
}

test.describe('Consolidado Diário', () => {
  test.beforeEach(async ({ page }) => {
    await login(page);
  });

  test('deve navegar para a página de consolidado', async ({ page }) => {
    await page.click('[data-testid="menu-consolidado"]');
    await expect(page).toHaveURL(/consolidado/);
    await expect(page.getByRole('heading', { name: /consolidado diário/i })).toBeVisible();
  });

  test('deve exibir saldo do dia atual', async ({ page }) => {
    await page.goto('/consolidado');

    const hoje = new Date().toISOString().split('T')[0];
    await expect(page.locator('[data-testid="data-consolidado"]')).toContainText(hoje.replace(/-/g, '/'));

    await expect(page.locator('[data-testid="saldo-final"]')).toBeVisible();
    await expect(page.locator('[data-testid="total-creditos"]')).toBeVisible();
    await expect(page.locator('[data-testid="total-debitos"]')).toBeVisible();
  });

  test('deve consultar consolidado por data específica', async ({ page }) => {
    await page.goto('/consolidado');

    await page.fill('[data-testid="filtro-data"]', '2024-01-15');
    await page.click('[data-testid="btn-consultar"]');

    await expect(page.locator('[data-testid="card-consolidado"]')).toBeVisible({ timeout: 10_000 });
  });

  test('deve exibir histórico de consolidados', async ({ page }) => {
    await page.goto('/consolidado/historico');

    const mesPassado = new Date();
    mesPassado.setMonth(mesPassado.getMonth() - 1);

    await page.fill('[data-testid="filtro-inicio"]', mesPassado.toISOString().split('T')[0]);
    await page.fill('[data-testid="filtro-fim"]', new Date().toISOString().split('T')[0]);
    await page.click('[data-testid="btn-filtrar"]');

    await expect(page.locator('[data-testid="tabela-historico"]')).toBeVisible({ timeout: 10_000 });
    await expect(page.locator('[data-testid="resumo-periodo"]')).toBeVisible();
  });

  test('deve exibir gráfico de saldo ao longo do tempo', async ({ page }) => {
    await page.goto('/consolidado/historico');

    await page.click('[data-testid="btn-exibir-grafico"]');
    await expect(page.locator('[data-testid="grafico-saldo"]')).toBeVisible();
  });

  test('deve carregar consolidado rapidamente (< 3s)', async ({ page }) => {
    const start = Date.now();
    await page.goto('/consolidado');
    await page.locator('[data-testid="card-consolidado"]').waitFor({ timeout: 3000 });
    const elapsed = Date.now() - start;

    expect(elapsed).toBeLessThan(3000);
  });

  test('deve funcionar em resolução mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto('/consolidado');

    await expect(page.locator('[data-testid="card-consolidado"]')).toBeVisible();
    await expect(page.locator('[data-testid="saldo-final"]')).toBeVisible();
  });
});
