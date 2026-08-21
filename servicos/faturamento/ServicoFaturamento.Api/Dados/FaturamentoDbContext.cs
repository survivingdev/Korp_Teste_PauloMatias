using Microsoft.EntityFrameworkCore;
using ServicoFaturamento.Api.Dominio;

namespace ServicoFaturamento.Api.Dados;

public sealed class FaturamentoDbContext(
    DbContextOptions<FaturamentoDbContext> options)
    : DbContext(options)
{
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();

    public DbSet<ItemNotaFiscal> ItensNotaFiscal => Set<ItemNotaFiscal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var nota = modelBuilder.Entity<NotaFiscal>();

        nota.ToTable(
            "notas_fiscais",
            tabela => tabela.HasCheckConstraint(
                "ck_notas_fiscais_status",
                "\"status\" IN (1, 2)"));

        nota.HasKey(n => n.Numero);

        nota.Property(n => n.Numero)
            .HasColumnName("numero")
            .ValueGeneratedOnAdd();

        nota.Property(n => n.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        nota.Property(n => n.CriadaEmUtc)
            .HasColumnName("criada_em_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        var item = modelBuilder.Entity<ItemNotaFiscal>();

        item.ToTable(
            "itens_nota_fiscal",
            tabela => tabela.HasCheckConstraint(
                "ck_itens_nota_fiscal_quantidade_positiva",
                "\"quantidade\" > 0"));

        item.HasKey(i => i.Id);

        item.Property(i => i.Id)
            .HasColumnName("id");

        item.Property(i => i.NotaFiscalNumero)
            .HasColumnName("nota_fiscal_numero");

        item.Property(i => i.ProdutoId)
            .HasColumnName("produto_id")
            .IsRequired();

        item.Property(i => i.Quantidade)
            .HasColumnName("quantidade")
            .IsRequired();

        item.HasIndex(i => new
            {
                i.NotaFiscalNumero,
                i.ProdutoId
            })
            .IsUnique()
            .HasDatabaseName("ux_itens_nota_fiscal_produto");

        item.HasOne(i => i.NotaFiscal)
            .WithMany(n => n.Itens)
            .HasForeignKey(i => i.NotaFiscalNumero)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
