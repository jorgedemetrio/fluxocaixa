# Fluxo de Caixa — Sistema de Controle Financeiro

Sistema de controle de fluxo de caixa diário com arquitetura de microsserviços, desenvolvido em **C# .NET 8**, aplicando **DDD**, **CQRS**, **Event-Driven Architecture** e padrões de mercado para alta disponibilidade e resiliência.

---

## Índice

- [Sobre o Projeto](#sobre-o-projeto)
- [Arquitetura](#arquitetura)
- [Tecnologias](#tecnologias)
- [Como Rodar Localmente](#como-rodar-localmente)
- [Testes](#testes)
- [Documentação](#documentação)
- [Decisões Arquiteturais](#decisões-arquiteturais)
- [Evoluções Futuras](#evoluções-futuras)

---

## Sobre o Projeto

O sistema permite que comerciantes:
- **Registrem lançamentos** de débito e crédito com validações de negócio
- **Consultem o saldo consolidado** diário e histórico de períodos
- Operem com **alta disponibilidade**: o serviço de lançamentos funciona mesmo que o consolidado esteja fora do ar

### Requisitos Não-Funcionais Atendidos
| Requisito | Solução |
|-----------|---------|
| Lançamentos independentes do Consolidado | Mensageria assíncrona (SQS/RabbitMQ) |
| 50 req/s no Consolidado com < 5% perda | Redis cache com TTL 5min |
| 99,9% de disponibilidade | Multi-AZ, auto-scaling ECS |
| Segurança | JWT + Rate Limit + WAF + TLS 1.2+ |

---

## Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                        Usuário                              │
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTPS
                    Route 53 → CloudFront → WAF → API Gateway
                           │
              ┌────────────┴────────────┐
              │                         │
    ┌─────────▼────────┐    ┌──────────▼────────┐
    │  Svc Lançamentos │    │  Svc Consolidado  │
    │   (ASP.NET 8)    │    │   (ASP.NET 8)     │
    │   PORT: 5001     │    │   PORT: 5002      │
    └─────────┬────────┘    └──────────┬────────┘
              │ CQRS/DDD              │ CQRS + Cache
              │                      │
    ┌─────────▼────────┐    ┌─────────▼────────┐
    │   PostgreSQL     │    │   PostgreSQL +   │
    │  (Lançamentos)   │    │     Redis Cache  │
    └─────────┬────────┘    └──────────────────┘
              │ Evento
    ┌─────────▼────────┐
    │  RabbitMQ/SQS    │ ────► Consolidado Consumer
    │   (Mensageria)   │
    └──────────────────┘
```

### Padrões Aplicados
- **CQRS** com MediatR (Commands e Queries separados)
- **DDD** tático: Aggregate Root, Value Objects, Domain Events
- **Event-Driven Architecture** via SQS/RabbitMQ
- **Outbox Pattern** para garantia de entrega de eventos
- **Result Pattern** para controle de fluxo explícito
- **Repository + Unit of Work** para persistência
- **Facade** para abstração do SQS
- **Circuit Breaker** com Polly

---

## Tecnologias

| Camada | Tecnologia |
|--------|-----------|
| Runtime | .NET 8 / C# 12 |
| API | ASP.NET Core Web API |
| ORM | Entity Framework Core 8 + Npgsql |
| CQRS | MediatR 12 |
| Validação | FluentValidation |
| Mensageria | AWS SQS (ElasticMQ para dev) |
| Cache | Redis (ElastiCache na AWS) |
| Auth | JWT Bearer + AWS Cognito |
| Rate Limiting | AspNetCoreRateLimit |
| Logging | Serilog |
| Testes | xUnit + Moq + FluentAssertions |
| E2E | Playwright |
| Containers | Docker + Docker Compose |
| CI/CD | GitHub Actions |
| Cloud | AWS (ECS Fargate, RDS, ElastiCache, SQS, WAF) |

---

## Como Rodar Localmente

### Pré-requisitos
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (ou Docker + Docker Compose)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (opcional, para desenvolvimento)

### 1. Clone o repositório
```bash
git clone https://github.com/jorgedemetrio/fluxocaixa.git
cd fluxocaixa
```

### 2. Suba todos os serviços com Docker Compose
```bash
docker compose up -d
```

Isso irá subir:
| Serviço | URL |
|---------|-----|
| **API Lançamentos** | http://localhost:5001 |
| **API Consolidado** | http://localhost:5002 |
| **Swagger Lançamentos** | http://localhost:5001/swagger |
| **Swagger Consolidado** | http://localhost:5002/swagger |
| **Frontend** | http://localhost:4200 |
| **PostgreSQL Lançamentos** | localhost:5432 |
| **PostgreSQL Consolidado** | localhost:5433 |
| **Redis** | localhost:6379 |
| **ElasticMQ (SQS local)** | http://localhost:9324 |

### 3. Verificar se está saudável
```bash
curl http://localhost:5001/health
curl http://localhost:5002/health
```

### 4. Exemplos de uso da API

#### Registrar um crédito
```bash
curl -X POST http://localhost:5001/api/lancamentos \
  -H "Authorization: Bearer {seu-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "tipo": "Credito",
    "valor": 1500.00,
    "descricao": "Venda de produto X",
    "data": "2024-01-15"
  }'
```

#### Registrar um débito
```bash
curl -X POST http://localhost:5001/api/lancamentos \
  -H "Authorization: Bearer {seu-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "tipo": "Debito",
    "valor": 300.00,
    "descricao": "Compra de insumos",
    "data": "2024-01-15"
  }'
```

#### Consultar consolidado do dia
```bash
curl http://localhost:5002/api/consolidado/2024-01-15 \
  -H "Authorization: Bearer {seu-token}"
```

#### Listar lançamentos
```bash
curl "http://localhost:5001/api/lancamentos?dataInicio=2024-01-15&page=1&pageSize=20" \
  -H "Authorization: Bearer {seu-token}"
```

### 5. Rodar sem Docker (desenvolvimento local)

```bash
# Sobe apenas as dependências (banco, redis, mensageria)
docker compose up postgres-lancamentos postgres-consolidado redis elasticmq -d

# Terminal 1 - Lançamentos API
cd src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.API
dotnet run

# Terminal 2 - Consolidado API
cd src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.API
dotnet run
```

---

## Testes

### Testes Unitários
```bash
dotnet test tests/FluxoCaixa.Lancamentos.UnitTests/
dotnet test tests/FluxoCaixa.Consolidado.UnitTests/

# Com cobertura de código
dotnet test FluxoCaixa.sln --collect:"XPlat Code Coverage"
```

### Testes de Integração
```bash
# Requer PostgreSQL e Redis rodando
docker compose up postgres-lancamentos postgres-consolidado redis -d
dotnet test tests/FluxoCaixa.Integration.Tests/
```

### Testes E2E (Playwright)
```bash
cd tests/FluxoCaixa.E2E.Playwright
npm install
npx playwright install
npx playwright test

# Interface visual
npx playwright test --ui

# Relatório HTML
npx playwright show-report
```

---

## Documentação

| Documento | Descrição |
|-----------|-----------|
| [Visão Geral](docs/funcional/01-visao-geral.md) | Diagrama C4, atores e fluxos principais |
| [Casos de Uso](docs/funcional/02-casos-de-uso.md) | UC001-UC010 com diagramas de sequência |
| [Regras de Negócio](docs/funcional/03-regras-negocio.md) | RN001-RN028 detalhadas |
| [Arquitetura](docs/funcional/04-arquitetura-solucao.md) | Decisões e trade-offs arquiteturais |
| [Infraestrutura AWS](docs/devops/01-infraestrutura-aws.md) | Diagrama completo AWS |
| [Segurança](docs/devops/02-seguranca.md) | WAF, IAM, criptografia |
| [Escalabilidade](docs/devops/03-escalabilidade-resiliencia.md) | Auto-scaling, SLAs, DR |
| [CI/CD Pipeline](docs/devops/04-cicd-pipeline.md) | Blue/Green deploy, aprovações |
| [Padrões de Desenvolvimento](docs/padroes/01-padroes-desenvolvimento.md) | CQRS, DDD, Result Pattern |
| [Modelo de Dados (MER)](docs/mer.md) | ER com todos os campos e índices |

---

## Decisões Arquiteturais

### Por que Microsserviços?
O requisito não-funcional crítico determina que o serviço de lançamentos **deve permanecer disponível** mesmo se o consolidado estiver fora do ar. Microsserviços com mensageria assíncrona é a solução natural para esse tipo de isolamento de falhas.

### Por que CQRS?
Lançamentos têm throughput de escrita muito diferente do Consolidado (leitura intensiva com 50 req/s). CQRS permite otimizar cada lado independentemente.

### Por que Redis para Consolidado?
Com 50 req/s e cache de 5 minutos, apenas ~2,5 req/s chegam ao banco (95% de cache hit). Isso garante a meta de < 5% de perda facilmente.

### Por que Outbox Pattern?
Garante que eventos sejam publicados exatamente uma vez, mesmo em caso de falha do broker, sem quebrar a transação do lançamento.

---

## Evoluções Futuras

| Evolução | Impacto | Complexidade |
|----------|---------|--------------|
| **Event Sourcing** para Lançamentos | Auditoria completa | Alta |
| **GraphQL** como BFF | Flexibilidade frontend | Média |
| **Testes de mutação** com Stryker | Qualidade de testes | Baixa |
| **Saga Pattern** para lançamentos compostos | Transações distribuídas | Alta |
| **gRPC** para comunicação interna | Performance | Média |
| **Feature Flags** (AWS AppConfig) | Deploys gradativos | Baixa |
| **Multi-tenancy** por empresa | SaaS multi-cliente | Alta |

---

## Estrutura do Projeto

```
fluxocaixa/
├── src/
│   ├── FluxoCaixa.Shared/Kernel/          # Result, PagedResult, AggregateRoot
│   ├── FluxoCaixa.Lancamentos/            # Domain, Application, Infrastructure, API
│   └── FluxoCaixa.Consolidado/            # Domain, Application, Infrastructure, API
├── tests/
│   ├── FluxoCaixa.Lancamentos.UnitTests/
│   ├── FluxoCaixa.Consolidado.UnitTests/
│   ├── FluxoCaixa.Integration.Tests/
│   └── FluxoCaixa.E2E.Playwright/
├── docs/funcional/ devops/ padroes/ mer.md
├── infrastructure/                         # ElasticMQ config
├── .github/workflows/                      # CI/CD pipelines
├── docker-compose.yml
└── FluxoCaixa.sln
```

**Stack**: C# .NET 8 | DDD | CQRS | EDA | AWS | Docker | PostgreSQL | Redis | SQS

---

## Como compilar, rodar e fazer deploy

### Compilar a solução (.NET 8)

```bash
dotnet build FluxoCaixa.sln
```

### Rodar todos os testes

```bash
dotnet test FluxoCaixa.sln
```

### Rodar localmente (modo desenvolvimento)

1. Suba as dependências (banco, redis, mensageria):
  ```bash
  docker compose up postgres-lancamentos postgres-consolidado redis elasticmq -d
  ```
2. Rode as APIs em terminais separados:
  ```bash
  # Terminal 1 - Lançamentos
  cd src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.API
  dotnet run

  # Terminal 2 - Consolidado
  cd src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.API
  dotnet run
  ```

### Rodar tudo com Docker Compose (recomendado para ambiente completo)

```bash
docker compose up -d
```

URLs de acesso:

| Serviço                  | URL                           |
|--------------------------|-------------------------------|
| API Lançamentos          | http://localhost:5001         |
| API Consolidado          | http://localhost:5002         |
| Swagger Lançamentos      | http://localhost:5001/swagger |
| Swagger Consolidado      | http://localhost:5002/swagger |
| Frontend                 | http://localhost:4200         |
| PostgreSQL Lançamentos   | localhost:5432                |
| PostgreSQL Consolidado   | localhost:5433                |
| Redis                    | localhost:6379                |
| ElasticMQ (SQS local)    | http://localhost:9324         |

### Parar todos os serviços Docker

```bash
docker compose down
```

### Deploy (produção)

O deploy é realizado via Docker (imagens) e CI/CD no GitHub Actions. Para buildar manualmente as imagens:

```bash
# Exemplo para API de Lançamentos
docker build -t fluxo-caixa-lancamentos-api ./src/FluxoCaixa.Lancamentos/FluxoCaixa.Lancamentos.API

# Exemplo para API de Consolidado
docker build -t fluxo-caixa-consolidado-api ./src/FluxoCaixa.Consolidado/FluxoCaixa.Consolidado.API
```

O pipeline de deploy está definido em `.github/workflows/`. Para produção, utilize as imagens geradas e o pipeline CI/CD.
