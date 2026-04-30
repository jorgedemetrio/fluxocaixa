# Segurança - Fluxo de Caixa AWS

## Modelo de Ameaças

```mermaid
flowchart LR
    subgraph Ameaças
        A1[DDoS Attack]
        A2[SQL Injection]
        A3[XSS Attack]
        A4[MITM]
        A5[Token Hijacking]
        A6[Credential Theft]
    end

    subgraph Mitigações
        M1[AWS Shield + WAF]
        M2[Parameterized Queries + EF Core]
        M3[CSP Headers + WAF XSS Rules]
        M4[TLS 1.2+ Everywhere]
        M5[JWT Short TTL + Refresh Rotation]
        M6[Secrets Manager + IAM Roles]
    end

    A1 --> M1
    A2 --> M2
    A3 --> M3
    A4 --> M4
    A5 --> M5
    A6 --> M6
```

## Camadas de Segurança

### Camada 1: Perímetro
- **Route 53**: DNSSEC habilitado
- **CloudFront**: HTTPS obrigatório, TLS 1.2 mínimo, HSTS habilitado
- **AWS Shield Advanced**: Proteção DDoS L3/L4/L7
- **AWS WAF Rules**:
  ```
  AWSManagedRulesCommonRuleSet     → SQLi, XSS, LFI, RFI
  AWSManagedRulesSQLiRuleSet       → SQL Injection avançado
  AWSManagedRulesKnownBadInputsRuleSet → Log4j, Spring4Shell
  RateLimitingRule                 → 2000 req/5min por IP
  GeoBlockRule                     → Bloqueia GEOs de alto risco
  ```

### Camada 2: API Gateway
- **Cognito Authorizer**: Valida JWT (assinatura, expiração, **issuer**)
- **Rate Throttling**: Por rota e por usuário
- **Request Validation**: Schema validation automática
- **Resource Policy**: Permite apenas CloudFront como origem
- **TLS**: Certificado ACM, mTLS opcional

### Camada 3: Aplicação
- **JWT Claims**:
  - `iss`: Validado (deve ser o Cognito User Pool)
  - `aud`: Validado (deve ser o App Client ID)
  - `exp`: Validado (token não expirado)
  - `sub`: Extraído como UserId
  - `cognito:groups`: Extraído como Roles
- **CORS**: Lista branca de origens por ambiente
- **Rate Limiting**: AspNetCoreRateLimit por IP e por usuário
- **Input Validation**: FluentValidation em todas as commands
- **Output Sanitization**: Serialização controlada (sem circular refs, sem dados sensíveis)

### Camada 4: Rede (VPC)
```mermaid
graph TB
    subgraph VPC["VPC 10.0.0.0/16"]
        subgraph PUB["Subnet Pública 10.0.1.0/24 e 10.0.2.0/24"]
            ALB[Application Load Balancer]
            NAT[NAT Gateway]
        end
        subgraph APP["Subnet Privada App 10.0.11.0/24 e 10.0.12.0/24"]
            ECS[ECS Tasks]
        end
        subgraph DATA["Subnet Privada Data 10.0.21.0/24 e 10.0.22.0/24"]
            RDS[RDS PostgreSQL]
            REDIS[ElastiCache Redis]
        end
    end

    IGW[Internet Gateway] --> ALB
    ALB --> ECS
    ECS --> NAT
    ECS --> RDS
    ECS --> REDIS
```

**Security Groups**:
| SG | Inbound | Outbound |
|----|---------|----------|
| sg-alb | 443 de 0.0.0.0/0 | 5001,5002 para sg-ecs |
| sg-ecs | 5001,5002 de sg-alb | 5432 para sg-rds; 6379 para sg-redis; 443 para 0.0.0.0/0 |
| sg-rds | 5432 de sg-ecs | Nenhum |
| sg-redis | 6379 de sg-ecs | Nenhum |

### Camada 5: Dados
- **RDS**: Encryption at-rest com KMS; TLS in-transit
- **Redis**: AUTH token + TLS in-transit + encryption at-rest
- **S3**: SSE-S3 ou SSE-KMS; Block Public Access habilitado
- **Secrets Manager**: Credenciais de banco, Redis AUTH, API keys
- **KMS**: Customer Managed Keys (CMK) rotacionadas anualmente

### Camada 6: Identidade (IAM)
- **ECS Task Role** (Princípio do mínimo privilégio):
  ```json
  {
    "Lançamentos Task Role": ["sqs:SendMessage", "secretsmanager:GetSecretValue", "kms:Decrypt"],
    "Consolidado Task Role": ["sqs:ReceiveMessage", "sqs:DeleteMessage", "secretsmanager:GetSecretValue", "s3:PutObject"]
  }
  ```
- **GitHub Actions Role**: Apenas `ecr:GetAuthorizationToken`, `ecr:BatchGetImage`, `ecs:UpdateService`

## Observabilidade de Segurança
- **CloudTrail**: Auditoria de todas as chamadas de API AWS
- **GuardDuty**: Detecção de ameaças ML-based
- **Security Hub**: Visão unificada de postura de segurança
- **VPC Flow Logs**: Tráfego de rede para análise
- **WAF Logs**: Requests bloqueados em S3 + Athena para queries
