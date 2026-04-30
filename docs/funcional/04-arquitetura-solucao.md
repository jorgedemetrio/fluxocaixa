# Arquitetura da Solução - Fluxo de Caixa

## 1. Visão Arquitetural

A solução adota uma arquitetura de **microsserviços** com comunicação **síncrona** (REST) para operações de leitura e **assíncrona** (eventos via broker) para propagação de lançamentos ao consolidado. Isso garante que o serviço de lançamentos permaneça disponível independentemente do consolidado.

```mermaid
graph TB
    subgraph "Cliente"
        FE[Angular Frontend]
        MOB[Mobile / PWA]
    end

    subgraph "Edge"
        R53[Route 53 DNS]
        CF[CloudFront CDN]
        WAF[AWS WAF]
        SHIELD[AWS Shield]
    end

    subgraph "API Layer"
        APIGW[AWS API Gateway\nRate Limit + Auth]
    end

    subgraph "VPC - Microsserviços"
        subgraph "ECS Cluster - Lançamentos"
            SL1[Svc Lançamentos :5001]
            SL2[Svc Lançamentos :5001]
        end
        subgraph "ECS Cluster - Consolidado"
            SC1[Svc Consolidado :5002]
            SC2[Svc Consolidado :5002]
        end
        ALB[Application Load Balancer]
    end

    subgraph "Mensageria"
        MQ[RabbitMQ / AWS SQS\nFila: lancamentos.eventos]
        DLQ[Dead Letter Queue]
    end

    subgraph "Persistência"
        DB1[(PostgreSQL\nLançamentos)]
        DB2[(PostgreSQL\nConsolidado)]
        REDIS[(Redis ElastiCache\nCache Consolidado)]
    end

    subgraph "Segurança / Auth"
        IDP[AWS Cognito\nIdentity Provider]
        KMS[AWS KMS\nEncryption Keys]
        SM[Secrets Manager]
    end

    FE --> R53
    MOB --> R53
    R53 --> CF
    CF --> WAF
    WAF --> SHIELD
    SHIELD --> APIGW
    APIGW --> ALB
    ALB --> SL1
    ALB --> SL2
    ALB --> SC1
    ALB --> SC2
    SL1 --> DB1
    SL2 --> DB1
    SL1 --> MQ
    SL2 --> MQ
    MQ --> DLQ
    SC1 --> MQ
    SC2 --> MQ
    SC1 --> DB2
    SC2 --> DB2
    SC1 --> REDIS
    SC2 --> REDIS
    APIGW --> IDP
    DB1 --> KMS
    DB2 --> KMS
    SM --> SL1
    SM --> SC1
```

---

## 2. Padrão de Comunicação

```mermaid
flowchart LR
    subgraph Síncrono
        A[Cliente] -->|REST/HTTP| B[API Gateway]
        B -->|REST/HTTP| C[Microsserviço]
        C -->|Response| B
        B -->|Response| A
    end

    subgraph Assíncrono
        D[Svc Lançamentos] -->|Publica Evento| E[Message Broker]
        E -->|Consome Evento| F[Svc Consolidado]
        F -->|UPSERT| G[(DB Consolidado)]
        F -->|Invalida| H[(Redis Cache)]
    end
```

### Justificativa
- **Síncrono** para lançamentos: o usuário precisa de confirmação imediata do registro
- **Assíncrono** para consolidado: desacopla os serviços; se o consolidado cair, lançamentos continuam funcionando
- Essa decisão atende diretamente ao requisito não-funcional crítico: *"O serviço de lançamento não deve ficar indisponível se o consolidado cair"*

---

## 3. Estrutura do Projeto

```
fluxocaixa/
├── src/
│   ├── FluxoCaixa.Lancamentos/
│   │   ├── FluxoCaixa.Lancamentos.API/          # Controllers, Middleware, DI
│   │   ├── FluxoCaixa.Lancamentos.Application/  # CQRS: Commands, Queries, Handlers
│   │   ├── FluxoCaixa.Lancamentos.Domain/       # Entidades, Value Objects, Events
│   │   └── FluxoCaixa.Lancamentos.Infrastructure/ # EF Core, RabbitMQ, Repositories
│   ├── FluxoCaixa.Consolidado/
│   │   ├── FluxoCaixa.Consolidado.API/
│   │   ├── FluxoCaixa.Consolidado.Application/
│   │   ├── FluxoCaixa.Consolidado.Domain/
│   │   └── FluxoCaixa.Consolidado.Infrastructure/
│   └── FluxoCaixa.Shared/
│       └── FluxoCaixa.Shared.Kernel/            # Result, PagedResult, Base Entities
├── tests/
│   ├── FluxoCaixa.Lancamentos.UnitTests/
│   ├── FluxoCaixa.Consolidado.UnitTests/
│   └── FluxoCaixa.Integration.Tests/
├── frontend/                                     # Angular 17
├── docs/
└── docker-compose.yml
```

---

## 4. Camadas por Microsserviço (DDD)

```mermaid
graph TB
    subgraph "Serviço de Lançamentos"
        API1[API Layer\nControllers, Middleware, Auth]
        APP1[Application Layer\nCQRS Handlers, Validators, DTOs]
        DOM1[Domain Layer\nEntities, Value Objects, Domain Events]
        INF1[Infrastructure Layer\nEF Core, RabbitMQ Publisher, Repositories]
    end

    API1 --> APP1
    APP1 --> DOM1
    INF1 --> DOM1
    API1 --> INF1
```

---

## 5. Decisões Arquiteturais (ADRs Resumidos)

| # | Decisão | Justificativa | Trade-off |
|---|---------|---------------|-----------|
| ADR-001 | **Microsserviços** em vez de Monolito | Requisito de independência entre Lançamentos e Consolidado | Complexidade operacional maior |
| ADR-002 | **CQRS** com MediatR | Separação clara de leitura/escrita, testabilidade, escalabilidade independente | Mais código boilerplate |
| ADR-003 | **RabbitMQ / SQS** para eventos | Desacopla serviços, garante entrega mesmo com falhas | Eventual consistency no consolidado |
| ADR-004 | **Redis** para cache do Consolidado | Suporta 50 req/s com < 5% perda sem pressão no banco | Dados com até 5min de defasagem |
| ADR-005 | **PostgreSQL** como banco relacional | ACID, JSON support, excelente suporte a .NET via EF Core | Não é nativo de cloud como DynamoDB |
| ADR-006 | **JWT** com AWS Cognito | Stateless, escalável, integração nativa com API Gateway | Necessita validação de issuer |
| ADR-007 | **Result Pattern** em vez de exceções | Controle de fluxo explícito, sem try/catch espalhados | Mais verboso |
| ADR-008 | **ECS Fargate** em vez de Lambda | Latência previsível, sem cold start, controle de recursos | Custo fixo mesmo em idle |

---

## 6. Fluxo de Resiliência

```mermaid
flowchart TD
    A[Lançamento Criado] --> B[Publica no Broker]
    B --> C{Broker disponível?}
    C -->|Sim| D[Evento na Fila]
    C -->|Não| E[Retry com Polly\n3 tentativas, backoff]
    E --> F{Sucesso?}
    F -->|Sim| D
    F -->|Não| G[Lançamento salvo no DB\nEventos pendentes em OutBox]

    D --> H[Consumer Consolidado]
    H --> I{Consolidado disponível?}
    I -->|Sim| J[Atualiza Consolidado]
    I -->|Não| K[Mensagem volta à fila\nDead Letter Queue após 3 tentativas]

    G --> L[Outbox Processor\nJob periódico reprocessa]
    L --> B
```

**Padrão Outbox**: Garante que eventos sejam publicados mesmo se o broker estiver indisponível no momento do lançamento. O lançamento é salvo + evento gravado na tabela `outbox` em uma única transação. Um job periódico publica os eventos pendentes.
