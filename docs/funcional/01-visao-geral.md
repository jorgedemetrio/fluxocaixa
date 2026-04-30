# Visão Geral do Sistema - Fluxo de Caixa

## 1. Objetivo

O sistema de **Fluxo de Caixa** permite que comerciantes controlem seus lançamentos financeiros diários (débitos e créditos) e consultem relatórios de saldo consolidado por período. O sistema é projetado com foco em **alta disponibilidade**, **resiliência** e **desempenho**.

---

## 2. Diagrama de Contexto (C4 - Nível 1)

```mermaid
C4Context
    title Sistema de Fluxo de Caixa - Contexto

    Person(comerciante, "Comerciante", "Usuário principal que registra lançamentos e consulta relatórios")
    Person(contador, "Contador/Gestor", "Consulta relatórios consolidados para análise financeira")

    System(fluxoCaixa, "Sistema Fluxo de Caixa", "Controla lançamentos de débito/crédito e gera consolidado diário")

    System_Ext(idp, "Identity Provider (AWS Cognito / Keycloak)", "Autenticação e autorização via JWT")
    System_Ext(email, "Serviço de E-mail (AWS SES)", "Notificações e relatórios por e-mail")
    System_Ext(audit, "Sistema de Auditoria", "Registro de eventos para compliance")

    Rel(comerciante, fluxoCaixa, "Registra lançamentos, consulta saldo", "HTTPS/REST")
    Rel(contador, fluxoCaixa, "Consulta consolidado diário, exporta relatórios", "HTTPS/REST")
    Rel(fluxoCaixa, idp, "Valida tokens JWT", "HTTPS")
    Rel(fluxoCaixa, email, "Envia notificações e relatórios", "HTTPS")
    Rel(fluxoCaixa, audit, "Publica eventos de auditoria", "AMQP")
```

---

## 3. Diagrama de Containers (C4 - Nível 2)

```mermaid
C4Container
    title Sistema de Fluxo de Caixa - Containers

    Person(usuario, "Usuário", "Comerciante ou Gestor")

    Container(frontend, "Frontend Web", "Angular 17", "Interface para lançamentos e relatórios")
    Container(apigw, "API Gateway", "AWS API Gateway", "Roteamento, rate limiting, autenticação")

    Container(svcLancamentos, "Serviço de Lançamentos", "ASP.NET Core 8 / C#", "Registra e gerencia lançamentos de débito e crédito")
    Container(svcConsolidado, "Serviço de Consolidado", "ASP.NET Core 8 / C#", "Calcula e disponibiliza saldo consolidado diário")

    ContainerDb(dbLancamentos, "DB Lançamentos", "PostgreSQL (RDS)", "Armazena lançamentos financeiros")
    ContainerDb(dbConsolidado, "DB Consolidado", "PostgreSQL (RDS)", "Armazena saldos consolidados")
    ContainerDb(cache, "Cache", "Redis (ElastiCache)", "Cache de consultas de consolidado")
    Container(broker, "Message Broker", "RabbitMQ / AWS SQS", "Comunicação assíncrona entre serviços")

    Rel(usuario, frontend, "Acessa via browser", "HTTPS")
    Rel(frontend, apigw, "Requisições API", "HTTPS/REST")
    Rel(apigw, svcLancamentos, "Roteia lançamentos", "HTTP/REST")
    Rel(apigw, svcConsolidado, "Roteia consultas", "HTTP/REST")
    Rel(svcLancamentos, dbLancamentos, "Persiste lançamentos", "TCP/5432")
    Rel(svcLancamentos, broker, "Publica eventos de lançamento", "AMQP")
    Rel(svcConsolidado, broker, "Consome eventos de lançamento", "AMQP")
    Rel(svcConsolidado, dbConsolidado, "Persiste consolidados", "TCP/5432")
    Rel(svcConsolidado, cache, "Cache de consultas", "TCP/6379")
```

---

## 4. Atores do Sistema

| Ator | Descrição | Permissões |
|------|-----------|------------|
| **Comerciante** | Usuário operacional que registra lançamentos no dia a dia | Criar/cancelar lançamentos, consultar próprios lançamentos e saldo |
| **Gestor/Contador** | Usuário gerencial que analisa dados financeiros | Consultar todos os lançamentos, relatórios consolidados, exportar dados |
| **Sistema de Consolidado** | Serviço interno que processa eventos de lançamento | Consumir eventos via broker, atualizar saldo consolidado |
| **Administrador** | Responsável pela configuração e manutenção do sistema | Acesso total, configurações, auditoria |

---

## 5. Funcionalidades Principais

### 5.1 Serviço de Lançamentos
- Registrar crédito (entrada de dinheiro)
- Registrar débito (saída de dinheiro)
- Cancelar lançamento (soft delete com motivo)
- Listar lançamentos por data com paginação
- Consultar lançamento por ID
- Exportar lançamentos em CSV/PDF

### 5.2 Serviço de Consolidado Diário
- Calcular saldo consolidado por data
- Consultar histórico de saldos (período)
- Saldo em tempo real (eventual consistency via eventos)
- Cache de 5 minutos para alta disponibilidade sob carga

---

## 6. Fluxo Principal - Registro de Lançamento

```mermaid
sequenceDiagram
    actor U as Usuário
    participant FE as Frontend
    participant GW as API Gateway
    participant SL as Svc Lançamentos
    participant DB as PostgreSQL
    participant MQ as RabbitMQ/SQS
    participant SC as Svc Consolidado
    participant CA as Redis Cache

    U->>FE: Preenche formulário (tipo, valor, descrição, categoria)
    FE->>GW: POST /api/lancamentos (JWT Bearer Token)
    GW->>GW: Valida JWT, aplica Rate Limit (100 req/min por usuário)
    GW->>SL: POST /lancamentos (forwarded)
    SL->>SL: Valida command (FluentValidation)
    SL->>SL: Cria entidade Lancamento (DDD Aggregate)
    SL->>DB: INSERT lancamentos (transação ACID)
    SL->>MQ: Publica LancamentoCriadoEvent (Fire and Forget)
    SL-->>GW: 201 Created { id, tipo, valor, data, status }
    GW-->>FE: 201 Created
    FE-->>U: Lançamento registrado com sucesso

    Note over MQ,SC: Processamento assíncrono (desacoplado)
    MQ->>SC: LancamentoCriadoEvent
    SC->>SC: Atualiza consolidado do dia
    SC->>DB: UPSERT consolidado_diario
    SC->>CA: Invalida cache do dia
```

---

## 7. Fluxo - Consulta de Consolidado Diário

```mermaid
sequenceDiagram
    actor U as Usuário
    participant FE as Frontend
    participant GW as API Gateway
    participant SC as Svc Consolidado
    participant CA as Redis Cache
    participant DB as PostgreSQL

    U->>FE: Seleciona data para ver saldo
    FE->>GW: GET /api/consolidado?data=2024-01-15
    GW->>GW: Valida JWT, Rate Limit (500 req/min global)
    GW->>SC: GET /consolidado?data=2024-01-15
    SC->>CA: GET consolidado:2024-01-15

    alt Cache HIT (TTL 5 min)
        CA-->>SC: { data, saldo, totalCreditos, totalDebitos }
        SC-->>GW: 200 OK (X-Cache: HIT)
    else Cache MISS
        SC->>DB: SELECT FROM consolidado_diario WHERE data = '2024-01-15'
        DB-->>SC: Registro consolidado
        SC->>CA: SET consolidado:2024-01-15 TTL=300
        SC-->>GW: 200 OK (X-Cache: MISS)
    end

    GW-->>FE: 200 OK { data, saldoFinal, totalCreditos, totalDebitos, qtdLancamentos }
    FE-->>U: Exibe saldo consolidado do dia
```

---

## 8. Regras de Negócio Resumidas

| ID | Regra |
|----|-------|
| RN001 | Todo lançamento deve ter tipo (CREDITO ou DEBITO), valor > 0 e data |
| RN002 | Valor mínimo: R$ 0,01 — Valor máximo: R$ 9.999.999,99 |
| RN003 | Um lançamento cancelado não pode ser reativado |
| RN004 | O saldo pode ser negativo (sem bloqueio operacional) |
| RN005 | O consolidado reflete o saldo acumulado até a data solicitada |
| RN006 | Lançamentos de datas futuras são permitidos mas sinalizados |
| RN007 | O serviço de Lançamentos funciona independentemente do Consolidado |
| RN008 | Consolidado com eventual consistency (máximo 30s de defasagem) |

---

## 9. Requisitos Não-Funcionais

| Requisito | Meta | Estratégia |
|-----------|------|-----------|
| Disponibilidade | 99,9% (8,7h downtime/ano) | Multi-AZ, health checks, circuit breaker |
| Lançamentos - Latência | P99 < 200ms | Cache de validações, índices otimizados |
| Consolidado - Throughput | 50 req/s com < 5% de perda | Redis cache, auto-scaling ECS |
| Consolidado - Latência | P99 < 100ms (cache hit) | TTL 5min no Redis |
| Resiliência | Lançamentos independente do Consolidado | Mensageria assíncrona (SQS/RabbitMQ) |
| Segurança | JWT + Rate Limit + WAF + TLS 1.2+ | AWS WAF, API Gateway, AWS Shield |
| LGPD | Dados sensíveis criptografados | KMS at-rest, TLS in-transit |
