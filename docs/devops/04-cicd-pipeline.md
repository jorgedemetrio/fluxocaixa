# Pipeline CI/CD - Fluxo de Caixa

## Visão Geral do Pipeline

```mermaid
flowchart LR
    subgraph DEV["Desenvolvimento"]
        CODE[Push / PR]
        LINT[Lint + Format]
        TEST_U[Unit Tests]
        TEST_I[Integration Tests]
        SONAR[SonarCloud\nCode Quality]
    end

    subgraph BUILD["Build & Package"]
        DOCKER[Docker Build]
        ECR[Push para ECR]
        SCAN[Trivy Security Scan]
    end

    subgraph STAGING["Staging"]
        DEP_STG[Deploy ECS Staging]
        SMOKE[Smoke Tests]
        PERF[k6 Performance Tests]
    end

    subgraph APPROVAL["Aprovação"]
        MANUAL[Aprovação Manual\nTécnico Responsável]
    end

    subgraph PROD["Produção"]
        DEP_PROD[Blue/Green Deploy ECS]
        HEALTH[Health Checks]
        MONITOR[Monitoramento 30min]
        ROLLBACK{Rollback\nnecessário?}
    end

    CODE --> LINT --> TEST_U --> TEST_I --> SONAR
    SONAR --> DOCKER --> ECR --> SCAN
    SCAN --> DEP_STG --> SMOKE --> PERF
    PERF --> MANUAL --> DEP_PROD --> HEALTH --> MONITOR --> ROLLBACK
    ROLLBACK -->|Sim| PREV[Versão anterior]
    ROLLBACK -->|Não| DONE[Deploy completo]
```

## Ambientes

| Ambiente | Branch | Deploy | Aprovação |
|----------|--------|--------|-----------|
| Development | `feature/*`, `develop` | Automático (PR) | Não |
| Staging | `main` | Automático | Não |
| Production | `main` (tag v*) | Automático após aprovação | Sim |

## Estratégia Blue/Green

```mermaid
flowchart TB
    subgraph BEFORE["Antes do Deploy"]
        ALB1[ALB 100% → Blue\nv1.2.0]
    end

    subgraph DURING["Durante Deploy"]
        ALB2[ALB 100% → Blue\nv1.2.0]
        GREEN[Green criado\nv1.3.0 aguardando]
        ALB2 -.-> GREEN
    end

    subgraph AFTER["Após Health Check OK"]
        ALB3[ALB 100% → Green\nv1.3.0]
        BLUE_OLD[Blue v1.2.0\naguarda 10min para rollback]
    end

    subgraph CLEANUP["Limpeza"]
        ALB4[ALB 100% → Green v1.3.0]
        DEL[Blue v1.2.0 removido]
    end

    BEFORE --> DURING --> AFTER --> CLEANUP
```

## Configuração dos Ambientes

```mermaid
flowchart LR
    subgraph GH["GitHub"]
        SECRETS[Secrets:\nAWS_ACCESS_KEY_ID\nAWS_SECRET_ACCESS_KEY\nCOGNITO_CLIENT_SECRET]
        VARS[Variables:\nAWS_REGION=us-east-1\nECR_REPO_URL\nECS_CLUSTER]
    end

    subgraph AWS_DEV["AWS Dev"]
        ECS_DEV[ECS: dev-cluster\n1 task mínimo]
        RDS_DEV[RDS: db.t3.micro\n20GB]
    end

    subgraph AWS_STG["AWS Staging"]
        ECS_STG[ECS: staging-cluster\n2 tasks mínimo]
        RDS_STG[RDS: db.t3.small\n50GB]
    end

    subgraph AWS_PRD["AWS Production"]
        ECS_PRD[ECS: prod-cluster\n2 tasks mínimo\n20 máximo]
        RDS_PRD[RDS: db.t3.medium\nMulti-AZ\n100GB]
    end

    GH --> AWS_DEV
    GH --> AWS_STG
    GH --> AWS_PRD
```

## Versionamento

- **SemVer**: `MAJOR.MINOR.PATCH` (ex: `v1.3.2`)
- **Tags de release**: Trigger automático de deploy para produção
- **Container tags**: `{version}` + `{commit-sha}` + `latest`
- **Rollback**: Basta re-taggear a versão anterior no ECR e re-deployar

## Monitoramento Pós-Deploy

Após deploy em produção, pipeline monitora por 30 minutos:
- Error rate < 1% (CloudWatch)
- P99 latency < 300ms (X-Ray)
- Health checks verdes (ECS)
- Zero alarmes críticos (CloudWatch)

Se qualquer métrica falhar → rollback automático para versão anterior.
