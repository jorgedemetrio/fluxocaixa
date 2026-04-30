import { test, expect, Page } from '@playwright/test';

async function loginAs(page: Page, email = 'admin@fluxocaixa.com', password = 'Admin@123') {
  await page.goto('/login');
  await page.fill('[data-testid="email"]', email);
  await page.fill('[data-testid="password"]', password);
  await page.click('[data-testid="btn-login"]');
  await page.waitForURL(/dashboard|lancamentos/, { timeout: 10_000 });
}

test.describe('Lançamentos - Fluxo Principal', () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page);
  });

  test('deve navegar para página de novo lançamento', async ({ page }) => {
    await page.click('[data-testid="btn-novo-lancamento"]');
    await expect(page).toHaveURL(/lancamentos\/novo/);
    await expect(page.getByRole('heading', { name: /novo lançamento/i })).toBeVisible();
  });

  test('deve registrar um crédito com sucesso', async ({ page }) => {
    await page.goto('/lancamentos/novo');

    await page.click('[data-testid="tipo-credito"]');
    await page.fill('[data-testid="valor"]', '1500.00');
    await page.fill('[data-testid="descricao"]', 'Venda de produto via teste E2E');
    await page.fill('[data-testid="data"]', new Date().toISOString().split('T')[0]);

    await page.click('[data-testid="btn-salvar"]');

    await expect(page.getByText(/lançamento.*criado|registrado com sucesso/i)).toBeVisible({
      timeout: 10_000
    });
  });

  test('deve registrar um débito com sucesso', async ({ page }) => {
    await page.goto('/lancamentos/novo');

    await page.click('[data-testid="tipo-debito"]');
    await page.fill('[data-testid="valor"]', '300.00');
    await page.fill('[data-testid="descricao"]', 'Compra de material via teste E2E');
    await page.fill('[data-testid="data"]', new Date().toISOString().split('T')[0]);

    await page.click('[data-testid="btn-salvar"]');

    await expect(page.getByText(/lançamento.*criado|registrado com sucesso/i)).toBeVisible({
      timeout: 10_000
    });
  });

  test('deve exibir erro ao tentar criar lançamento com valor inválido', async ({ page }) => {
    await page.goto('/lancamentos/novo');

    await page.click('[data-testid="tipo-credito"]');
    await page.fill('[data-testid="valor"]', '0');
    await page.fill('[data-testid="descricao"]', 'Teste valor inválido');
    await page.fill('[data-testid="data"]', new Date().toISOString().split('T')[0]);

    await page.click('[data-testid="btn-salvar"]');

    await expect(page.getByText(/valor.*maior|inválido/i)).toBeVisible();
  });

  test('deve exibir erro ao tentar criar lançamento sem descrição', async ({ page }) => {
    await page.goto('/lancamentos/novo');

    await page.click('[data-testid="tipo-credito"]');
    await page.fill('[data-testid="valor"]', '100.00');
    await page.fill('[data-testid="data"]', new Date().toISOString().split('T')[0]);

    await page.click('[data-testid="btn-salvar"]');

    await expect(page.getByText(/descrição.*obrigatória/i)).toBeVisible();
  });

  test('deve listar lançamentos do dia', async ({ page }) => {
    await page.goto('/lancamentos');

    const hoje = new Date().toISOString().split('T')[0];
    await page.fill('[data-testid="filtro-data-inicio"]', hoje);
    await page.click('[data-testid="btn-filtrar"]');

    await expect(page.locator('[data-testid="tabela-lancamentos"]')).toBeVisible();
  });

  test('deve cancelar um lançamento', async ({ page }) => {
    await page.goto('/lancamentos');

    // Clica no primeiro lançamento ativo
    const primeiroLancamento = page.locator('[data-testid="lancamento-row"]').first();
    await primeiroLancamento.locator('[data-testid="btn-cancelar"]').click();

    // Preenche motivo
    await page.fill('[data-testid="motivo-cancelamento"]', 'Cancelado durante teste E2E automatizado');
    await page.click('[data-testid="btn-confirmar-cancelamento"]');

    await expect(page.getByText(/cancelado com sucesso/i)).toBeVisible({ timeout: 10_000 });
  });

  test('deve paginar a lista de lançamentos', async ({ page }) => {
    await page.goto('/lancamentos');

    const paginacao = page.locator('[data-testid="paginacao"]');
    await expect(paginacao).toBeVisible();

    const totalItems = await page.locator('[data-testid="total-count"]').textContent();
    expect(totalItems).toBeTruthy();
  });
});
