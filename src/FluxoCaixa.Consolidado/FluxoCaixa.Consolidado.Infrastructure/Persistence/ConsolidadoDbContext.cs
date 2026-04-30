using FluxoCaixa.Consolidado.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FluxoCaixa.Consolidado.Infrastructure.Persistence;

public class ConsolidadoDbContext(DbContextOptions<ConsolidadoDbContext> options) : DbContext(options)
{
    public DbSet<ConsolidadoDiario> ConsolidadosDiarios => Set<ConsolidadoDiario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ConsolidadoDiarioConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}

public class ConsolidadoDiarioConfiguration : IEntityTypeConfiguration<ConsolidadoDiario>
{
    public void Configure(EntityTypeBuilder<ConsolidadoDiario> builder)
    {
        builder.ToTable("consolidado_diario");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Data).HasColumnName("data").IsRequired();
        builder.Property(x => x.TotalCreditos).HasColumnName("total_creditos").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalDebitos).HasColumnName("total_debitos").HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.SaldoAcumulado).HasColumnName("saldo_acumulado").HasColumnType("decimal(18,2)").IsRequired();
        builder.Ignore(x => x.SaldoFinal);
        builder.Property(x => x.QtdLancamentos).HasColumnName("qtd_lancamentos").IsRequired();
        builder.Property(x => x.QtdCreditos).HasColumnName("qtd_creditos").IsRequired();
        builder.Property(x => x.QtdDebitos).HasColumnName("qtd_debitos").IsRequired();
        builder.Property(x => x.UltimaAtualizacao).HasColumnName("ultima_atualizacao").IsRequired();
        builder.Property(x => x.Versao).HasColumnName("versao").IsConcurrencyToken().IsRequired();
        builder.Property(x => x.CriadoEm).HasColumnName("criado_em").IsRequired();
        builder.Property(x => x.AtualizadoEm).HasColumnName("atualizado_em").IsRequired();

        builder.HasIndex(x => x.Data).IsUnique().HasDatabaseName("idx_consolidado_data");

        builder.Ignore(x => x.DomainEvents);
    }
}
