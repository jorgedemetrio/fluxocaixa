# Regras de Negócio - Fluxo de Caixa

## Lançamentos

| ID | Categoria | Regra | Validação |
|----|-----------|-------|-----------|
| RN001 | Tipo | Todo lançamento deve ser CREDITO ou DEBITO | Enum validation |
| RN002 | Valor | Valor mínimo: R$ 0,01 | valor > 0 |
| RN003 | Valor | Valor máximo: R$ 9.999.999,99 | valor <= 9999999.99 |
| RN004 | Descrição | Obrigatória, mínimo 3 caracteres, máximo 500 | NotEmpty, Length |
| RN005 | Data | Obrigatória, formato ISO 8601 (YYYY-MM-DD) | NotEmpty, IsDate |
| RN006 | Data Futura | Lançamentos futuros permitidos, retornam flag `dataFutura: true` | Aviso, não bloqueio |
| RN007 | Categoria | Opcional, máximo 100 caracteres | MaxLength |
| RN008 | Cancelamento | Lançamento CANCELADO não pode ser reativado | Status imutável |
| RN009 | Cancelamento | Motivo de cancelamento obrigatório (mín. 10 chars) | NotEmpty, MinLength |
| RN010 | Cancelamento | Somente o criador ou gestor pode cancelar | Autorização por claim |
| RN011 | Saldo | Saldo pode ser negativo (sem bloqueio) | Sem validação de saldo |
| RN012 | Usuário | Todo lançamento é vinculado ao usuário autenticado | Claim do JWT |

## Consolidado Diário

| ID | Categoria | Regra |
|----|-----------|-------|
| RN013 | Cálculo | `SaldoFinal = TotalCreditos - TotalDebitos` do dia |
| RN014 | Cálculo | `SaldoAcumulado = SaldoAnterior + SaldoFinal` |
| RN015 | Consistência | Eventual consistency — máximo 30s de defasagem |
| RN016 | Cache | Consolidado cacheado por 5 minutos no Redis |
| RN017 | Cache Invalidação | Cache invalidado ao receber novo evento do dia |
| RN018 | Dias sem mov. | Dias sem lançamentos têm saldo herdado do dia anterior |
| RN019 | Independência | Serviço de Lançamentos funciona mesmo se Consolidado estiver fora |
| RN020 | Reprocessamento | Eventos perdidos são reprocessados (DLQ + retry) |

## Segurança

| ID | Categoria | Regra |
|----|-----------|-------|
| RN021 | Auth | Todas as rotas exigem JWT válido (exceto /health, /auth) |
| RN022 | Auth | Token expira em 1 hora; refresh token em 7 dias |
| RN023 | Auth | Issuer do JWT deve ser validado (evitar tokens de outros sistemas) |
| RN024 | Rate Limit | Máximo 100 req/min por usuário no serviço de Lançamentos |
| RN025 | Rate Limit | Máximo 500 req/min global no serviço de Consolidado |
| RN026 | CORS | Apenas origens configuradas são permitidas |
| RN027 | Dados | Valores financeiros armazenados como DECIMAL(18,2) |
| RN028 | Auditoria | Todo CREATE/UPDATE/DELETE gera log de auditoria |

## Exemplos Concretos

### Exemplo 1: Dia com múltiplos lançamentos
```
Data: 2024-01-15

Lançamentos:
  09:00 - CREDITO R$ 500,00 (Venda produto A)
  10:30 - CREDITO R$ 1.200,00 (Venda produto B)
  11:00 - DEBITO  R$ 300,00 (Compra material)
  14:00 - CREDITO R$ 800,00 (Venda serviço)
  15:30 - DEBITO  R$ 150,00 (Taxa bancária)

Consolidado 2024-01-15:
  TotalCreditos: R$ 2.500,00
  TotalDebitos:  R$ 450,00
  SaldoFinal:    R$ 2.050,00
  QtdLancamentos: 5
```

### Exemplo 2: Saldo acumulado
```
Data         | Créditos  | Débitos   | Saldo Dia | Saldo Acumulado
-------------|-----------|-----------|-----------|----------------
2024-01-13   | R$ 1.000  | R$ 600    | R$ 400    | R$ 400
2024-01-14   | R$ 500    | R$ 800    | -R$ 300   | R$ 100
2024-01-15   | R$ 2.500  | R$ 450    | R$ 2.050  | R$ 2.150
```

### Exemplo 3: Cancelamento e re-consolidado
```
Lançamento original: CREDITO R$ 500,00 (status: ATIVO)
Ação: Cancelar com motivo "Venda devolvida pelo cliente"
Resultado:
  - Lançamento status → CANCELADO
  - Evento LancamentoCanceladoEvent publicado
  - Consolidado do dia: TotalCreditos -= 500,00
  - Cache Redis invalidado
```
