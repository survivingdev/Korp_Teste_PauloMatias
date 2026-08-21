using Microsoft.EntityFrameworkCore;
using ServicoEstoque.Api.Dominio;

namespace ServicoEstoque.Api.Dados;

public sealed class EstoqueDbContext(DbContextOptions<EstoqueDbContext> options)
    : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var produto = modelBuilder.Entity<Produto>();

        produto.ToTable(
            "produtos",
            tabela => tabela.HasCheckConstraint(
                "ck_produtos_saldo_nao_negativo",
                "\"saldo\" >= 0"));

        produto.HasKey(p => p.Id);

        produto.Property(p => p.Id)
            .HasColumnName("id");

        produto.Property(p => p.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(50)
            .IsRequired();

        produto.HasIndex(p => p.Codigo)
            .IsUnique()
            .HasDatabaseName("ux_produtos_codigo");

        produto.Property(p => p.Descricao)
            .HasColumnName("descricao")
            .HasMaxLength(200)
            .IsRequired();

        produto.Property(p => p.Saldo)
            .HasColumnName("saldo")
            .IsRequired();
    }
}
