namespace FluxoCaixa.Shared.Kernel;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CriadoEm { get; protected set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; protected set; } = DateTime.UtcNow;

    protected void SetAtualizado() => AtualizadoEm = DateTime.UtcNow;

    public override bool Equals(object? obj) =>
        obj is Entity entity && Id == entity.Id;

    public override int GetHashCode() => Id.GetHashCode();
}

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OcorridoEm { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OcorridoEm { get; } = DateTime.UtcNow;
}
