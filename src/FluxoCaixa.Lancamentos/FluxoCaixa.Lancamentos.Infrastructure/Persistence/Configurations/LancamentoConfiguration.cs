using FluxoCaixa.Lancamentos.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxoCaixa.Lancamentos.Infrastructure.Persistence.Configurations;

public class LancamentoConfiguration : IEntityTypeConfiguration<Lancamento>
{
    public void Configure(EntityTypeBuilder<Lancamento> builder)
    {
        builder.ToTable("lancamentos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.Tipo)
            .HasColumnName("tipo")
            .HasConversion<string>()
            .IsRequired();

        builder.OwnsOne(x => x.Valor, v =>
        {
            v.Property(d => d.Quantia)
                .HasColumnName("valor")
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            v.Ignore(d => d.Moeda);
        });

        builder.Property(x => x.Descricao).HasColumnName("descricao").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Data).HasColumnName("data").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().IsRequired();
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        builder.Property(x => x.CategoriaId).HasColumnName("categoria_id");
        builder.Property(x => x.DataFutura).HasColumnName("data_futura").IsRequired();
        builder.Property(x => x.MotivoCancelamento).HasColumnName("motivo_cancelamento");
        builder.Property(x => x.CanceladoEm).HasColumnName("cancelado_em");
        builder.Property(x => x.CanceladoPor).HasColumnName("cancelado_por");
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key");
        builder.Property(x => x.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em").IsRequired();

        builder.HasIndex(x => x.Data).HasDatabaseName("idx_lancamentos_data");
        builder.HasIndex(x => new { x.UsuarioId, x.Data }).HasDatabaseName("idx_lancamentos_usuario_data");
        builder.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName("idx_lancamentos_idempotency");

        builder.Ignore(x => x.DomainEvents);
    }
}

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("outbox_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.Status, x.CriadoEm }).HasDatabaseName("idx_outbox_status_criado");
    }
}
