# Infraestrutura AWS - Fluxo de Caixa

## Diagrama Completo da Infraestrutura

```mermaid
graph TB
    subgraph Internet
        USER[Usuários / Clientes]
    end

    subgraph DNS_CDN["DNS & CDN"]
        R53[Route 53\nDNS com health check]
        CF[CloudFront\nCDN + Cache estático]
    end

    subgraph Security["Segurança Perimetral"]
        SHIELD[AWS Shield Advanced\nDDoS Protection]
        WAF[AWS WAF\nSQLi, XSS, Rate Limit]
    end

    subgraph API_GW["API Layer"]
        APIGW[AWS API Gateway\nREST API + Rate Limiting\nCognito Authorizer]
    end

    subgraph VPC["VPC (10.0.0.0/16)"]
        subgraph PUB_SUBNET["Subnets Públicas (Multi-AZ)"]
            ALB[Application Load Balancer\nHTTPS + Health Checks]
        end

        subgraph PRIV_SUBNET["Subnets Privadas - Aplicação"]
            subgraph ECS_LANC["ECS Fargate - Lançamentos"]
                TASK_L1[Task: Lancamentos\nAZ-1a :5001]
                TASK_L2[Task: Lancamentos\nAZ-1b :5001]
            end
            subgraph ECS_CONS["ECS Fargate - Consolidado"]
                TASK_C1[Task: Consolidado\nAZ-1a :5002]
                TASK_C2[Task: Consolidado\nAZ-1b :5002]
            end
        end

        subgraph DATA_SUBNET["Subnets Privadas - Dados"]
            subgraph RDS_L["RDS PostgreSQL - Lançamentos"]
                RDS_L1[(Primary\nAZ-1a)]
                RDS_L2[(Read Replica\nAZ-1b)]
            end
            subgraph RDS_C["RDS PostgreSQL - Consolidado"]
                RDS_C1[(Primary\nAZ-1a)]
                RDS_C2[(Read Replica\nAZ-1b)]
            end
            REDIS[(ElastiCache Redis\nCluster Mode\nAZ-1a + AZ-1b)]
        end

        subgraph MSG["Mensageria"]
            SQS[AWS SQS\nFila: lancamentos-eventos\nFIFO + DLQ]
        end
    end

    subgraph AUTH["Identidade & Segredos"]
        COGNITO[AWS Cognito\nUser Pool + App Client]
        SM[Secrets Manager\nDB credentials, API keys]
        KMS[AWS KMS\nChaves de criptografia]
    end

    subgraph OBS["Observabilidade"]
        CW[CloudWatch\nLogs, Metrics, Alarms]
        XRAY[AWS X-Ray\nDistributed Tracing]
    end

    subgraph STORAGE["Armazenamento"]
        S3[S3 Bucket\nRelatorios exportados]
        ECR[ECR\nContainer Registry]
    end

    USER --> R53
    R53 --> CF
    CF --> SHIELD
    SHIELD --> WAF
    WAF --> APIGW
    APIGW --> COGNITO
    APIGW --> ALB
    ALB --> TASK_L1
    ALB --> TASK_L2
    ALB --> TASK_C1
    ALB --> TASK_C2
    TASK_L1 --> RDS_L1
    TASK_L2 --> RDS_L1
    TASK_L1 --> SQS
    TASK_L2 --> SQS
    TASK_C1 --> SQS
    TASK_C2 --> SQS
    TASK_C1 --> RDS_C1
    TASK_C2 --> RDS_C1
    TASK_C1 --> REDIS
    TASK_C2 --> REDIS
    RDS_L1 --> RDS_L2
    RDS_C1 --> RDS_C2
    SM --> TASK_L1
    SM --> TASK_C1
    KMS --> RDS_L1
    KMS --> RDS_C1
    KMS --> REDIS
    ECR --> ECS_LANC
    ECR --> ECS_CONS
    CW --> ECS_LANC
    CW --> ECS_CONS
    XRAY --> ECS_LANC
    XRAY --> ECS_CONS
    TASK_C1 --> S3
```

---

## Componentes Detalhados

### Route 53
- **Tipo**: Public Hosted Zone
- **Registros**: `api.fluxocaixa.com.br` → CloudFront
- **Health Checks**: Verifica `/health` dos serviços a cada 30s
- **Failover**: Automático para região secundária se health check falhar

### CloudFront
- **Origem**: API Gateway (API) e S3 (frontend estático)
- **Cache**: TTL 0 para API, TTL 86400 para assets estáticos
- **Behaviors**: `/api/*` sem cache; `/*` com cache longo
- **Geo-restriction**: Apenas Brasil (opcional)
- **TLS**: Certificate Manager (ACM) — TLS 1.2 mínimo

### AWS Shield
- **Nível**: Shield Advanced (proteção L3/L4/L7)
- **DDoS**: Mitigação automática de SYN floods, UDP floods
- **Response Team**: AWS DRT disponível 24/7

### AWS WAF
- **Rules**: AWSManagedRulesCommonRuleSet (SQLi, XSS, CSRF)
- **Rate Limit**: 2000 req/5min por IP
- **Custom Rules**: Block países de alto risco, block user-agents conhecidos
- **Logs**: Enviados para S3 + CloudWatch

### API Gateway
- **Tipo**: REST API (Regional)
- **Auth**: Cognito Authorizer + JWT validation (incluindo Issuer)
- **Rate Limit**: 10.000 req/s (burst: 5.000)
- **Throttling por rota**: `/lancamentos` 100 req/s; `/consolidado` 500 req/s
- **CORS**: Configurado via API Gateway
- **WAF**: Integrado ao WAF

### ECS Fargate
- **Lançamentos**: 2 tasks mínimo, 10 máximo; CPU 512, Memory 1024
- **Consolidado**: 2 tasks mínimo, 20 máximo; CPU 256, Memory 512
- **Auto Scaling**: Target Tracking (CPU 70%, Memory 70%)
- **Health Check**: `/health` a cada 30s, 2 falhas = replace
- **Deploy**: Rolling update (maxSurge: 100%, maxUnavailable: 0%)

### RDS PostgreSQL
- **Engine**: PostgreSQL 16
- **Multi-AZ**: Sim (failover automático < 60s)
- **Instance**: db.t3.medium (prod), db.t3.micro (dev)
- **Storage**: 100 GB gp3, autoscaling até 1TB
- **Backup**: Diário às 03:00 UTC, retenção 7 dias
- **Encryption**: KMS at-rest
- **Performance Insights**: Habilitado

### ElastiCache Redis
- **Engine**: Redis 7.x
- **Cluster Mode**: Habilitado (3 shards, 2 réplicas)
- **Instance**: cache.t3.medium
- **TLS**: In-transit encryption
- **Auth**: Redis AUTH token
- **Eviction**: allkeys-lru
- **TTL Padrão**: 300s (5 minutos)

### SQS (Mensageria)
- **Tipo**: FIFO Queue (garantia de ordem por dia)
- **DLQ**: Dead Letter Queue após 3 tentativas
- **Visibilidade**: 30 segundos
- **Retenção**: 7 dias
- **Encryption**: SSE-SQS
