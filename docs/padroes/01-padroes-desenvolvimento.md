# Padrões de Desenvolvimento - Fluxo de Caixa

## 1. CQRS — Command Query Responsibility Segregation

### Por que usar?
Separar operações de escrita (Commands) de leitura (Queries) permite escalar cada lado independentemente, facilita a aplicação de validações diferentes e melhora a testabilidade.

### Implementação com MediatR
```csharp
// COMMAND: Cria lançamento (write)
public record CreateLancamentoCommand(
    TipoLancamento Tipo,
    decimal Valor,
    string Descricao,
    DateTime Data,
    string? Categoria
) : IRequest<Result<LancamentoDto>>;

// QUERY: Consulta lançamentos (read)
public record GetLancamentosQuery(
    DateTime DataInicio,
    DateTime? DataFim,
    TipoLancamento? Tipo,
    int Page,
    int PageSize
) : IRequest<Result<PagedResult<LancamentoDto>>>;

// HANDLER
public class CreateLancamentoHandler(
    ILancamentoRepository repo,
    IMessagePublisher publisher
) : IRequestHandler<CreateLancamentoCommand, Result<LancamentoDto>>
{
    public async Task<Result<LancamentoDto>> Handle(
        CreateLancamentoCommand cmd, CancellationToken ct)
    {
        var lancamento = Lancamento.Criar(cmd.Tipo, cmd.Valor, cmd.Descricao, cmd.Data);
        await repo.SaveAsync(lancamento, ct);
        await publisher.PublishAsync(new LancamentoCriadoEvent(lancamento));
        return Result.Success(LancamentoDto.From(lancamento));
    }
}
```

### Trade-offs
| Prós | Contras |
|------|---------|
| Modelos de leitura/escrita otimizados separadamente | Mais código (handlers, DTOs separados) |
| Testabilidade excelente (handlers isolados) | Curva de aprendizado inicial |
| Escalabilidade independente | Possível duplicação de lógica se não bem organizado |

---

## 2. DDD — Domain-Driven Design (Tático)

### Entidades e Aggregate Root

```csharp
// AGGREGATE ROOT
public class Lancamento : AggregateRoot
{
    public Guid Id { get; private set; }
    public TipoLancamento Tipo { get; private set; }
    public Dinheiro Valor { get; private set; }         // Value Object
    public string Descricao { get; private set; }
    public DateTime Data { get; private set; }
    public StatusLancamento Status { get; private set; }

    private Lancamento() { } // EF Core

    public static Lancamento Criar(TipoLancamento tipo, decimal valor,
        string descricao, DateTime data)
    {
        // Invariantes de domínio aqui
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        if (valor <= 0) throw new DomainException("Valor deve ser positivo");

        var l = new Lancamento
        {
            Id = Guid.NewGuid(),
            Tipo = tipo,
            Valor = new Dinheiro(valor),
            Descricao = descricao,
            Data = data,
            Status = StatusLancamento.Ativo
        };
        l.AddDomainEvent(new LancamentoCriadoDomainEvent(l));
        return l;
    }

    public void Cancelar(string motivo)
    {
        if (Status == StatusLancamento.Cancelado)
            throw new DomainException("Lançamento já está cancelado");
        Status = StatusLancamento.Cancelado;
        AddDomainEvent(new LancamentoCanceladoDomainEvent(this, motivo));
    }
}
```

### Value Objects
```csharp
public record Dinheiro
{
    public decimal Valor { get; }
    public string Moeda { get; } = "BRL";

    public Dinheiro(decimal valor)
    {
        if (valor < 0) throw new DomainException("Valor monetário não pode ser negativo");
        if (valor > 9_999_999.99m) throw new DomainException("Valor excede limite máximo");
        Valor = Math.Round(valor, 2);
    }

    public Dinheiro Somar(Dinheiro outro) => new(Valor + outro.Valor);
    public Dinheiro Subtrair(Dinheiro outro) => new(Valor - outro.Valor);
}
```

---

## 3. Result Pattern

Substitui exceções para controle de fluxo esperado:

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ErrorType? ErrorType { get; }

    private Result(bool isSuccess, T? value, string? error, ErrorType? errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);
    public static Result<T> Failure(string error, ErrorType type = ErrorType.BusinessRule)
        => new(false, default, error, type);
    public static Result<T> NotFound(string entity)
        => new(false, default, $"{entity} não encontrado", ErrorType.NotFound);
}

// USO NO CONTROLLER
var result = await mediator.Send(command);
return result.IsSuccess
    ? Ok(result.Value)
    : result.ErrorType switch
    {
        ErrorType.NotFound => NotFound(result.Error),
        ErrorType.Validation => UnprocessableEntity(result.Error),
        _ => BadRequest(result.Error)
    };
```

---

## 4. Repository Pattern + Unit of Work

```csharp
// INTERFACE (Domain layer)
public interface ILancamentoRepository
{
    Task<Lancamento?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<Lancamento>> GetByPeriodoAsync(DateTime inicio, DateTime fim,
        TipoLancamento? tipo, int page, int pageSize, CancellationToken ct = default);
    Task SaveAsync(Lancamento lancamento, CancellationToken ct = default);
    Task UpdateAsync(Lancamento lancamento, CancellationToken ct = default);
}

// IMPLEMENTAÇÃO (Infrastructure layer)
public class LancamentoRepository(LancamentosDbContext db) : ILancamentoRepository
{
    public async Task SaveAsync(Lancamento lancamento, CancellationToken ct = default)
    {
        await db.Lancamentos.AddAsync(lancamento, ct);
        await db.SaveChangesAsync(ct);
    }
    // ... outros métodos
}
```

---

## 5. Mediator Pattern (via MediatR)

Desacopla controllers de handlers:
```csharp
// Controller não conhece handlers
[ApiController, Route("api/[controller]")]
public class LancamentosController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CreateLancamentoRequest req)
    {
        var cmd = new CreateLancamentoCommand(req.Tipo, req.Valor, req.Descricao, req.Data);
        var result = await mediator.Send(cmd);
        return result.IsSuccess ? CreatedAtAction(nameof(ObterPorId), new { id = result.Value!.Id }, result.Value) 
                                : HandleError(result);
    }
}
```

---

## 6. Dependency Injection com Assembly Scanning

```csharp
// Program.cs — registro automático por convenção
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
    typeof(CreateLancamentoHandler).Assembly));

builder.Services.AddValidatorsFromAssembly(
    typeof(CreateLancamentoValidator).Assembly);

// Registro por interface (Assembly Scanning com Scrutor)
builder.Services.Scan(scan => scan
    .FromAssemblyOf<LancamentoRepository>()
    .AddClasses(c => c.AssignableTo(typeof(IRepository<>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

---

## 7. Padrão Singleton para Configurações

```csharp
// Configurações imutáveis como singletons
builder.Services.AddSingleton<JwtSettings>(
    builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!);
builder.Services.AddSingleton<RedisSettings>(
    builder.Configuration.GetSection("Redis").Get<RedisSettings>()!);
```

---

## 8. Facade para Serviços Externos

```csharp
// Facade que esconde complexidade do SQS
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}

public class SqsMessagePublisher(IAmazonSQS sqsClient, SqsSettings settings) 
    : IMessagePublisher
{
    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        var body = JsonSerializer.Serialize(message);
        await sqsClient.SendMessageAsync(settings.QueueUrl, body, ct);
    }
}
```

---

## 9. Padrões de Código

### Convenções de Nomenclatura
| Elemento | Convenção | Exemplo |
|----------|-----------|---------|
| Classes | PascalCase | `LancamentoService` |
| Métodos | PascalCase | `CriarLancamentoAsync` |
| Parâmetros | camelCase | `tipoLancamento` |
| Constants | PascalCase | `MaximoValorLancamento` |
| Interfaces | IPascalCase | `ILancamentoRepository` |
| Records (CQRS) | PascalCase | `CreateLancamentoCommand` |

### SOLID aplicado
- **S**: Cada handler tem uma única responsabilidade
- **O**: Novos tipos de lançamento sem modificar existentes (via polimorfismo)
- **L**: Implementações de `IRepository<T>` substituíveis
- **I**: `ILancamentoRepository` separado de `IConsolidadoRepository`
- **D**: Controllers dependem de `IMediator`, não de implementações

---

## 10. Siglas Aplicadas no Projeto

| Sigla | Aplicação no Projeto |
|-------|---------------------|
| **DDD** | Entidades, Aggregate Root, Value Objects, Domain Events |
| **CQRS** | Commands (escrita) e Queries (leitura) separados via MediatR |
| **EDA** | Event-Driven Architecture via SQS/RabbitMQ |
| **BFF** | Backend-for-Frontend (API Gateway como BFF para o Angular) |
| **HTTP** | Comunicação REST entre cliente e API |
| **AMQP** | Protocolo de mensageria (RabbitMQ) |
| **JSON** | Formato de serialização entre serviços |
| **MVC** | Estrutura da API ASP.NET Core |
| **MVP** | Padrão de apresentação no frontend Angular |
