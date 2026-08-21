using Microsoft.EntityFrameworkCore;
using ServicoEstoque.Api.Dados;

namespace ServicoEstoque.Api.Aplicacao;

public sealed class ServicoBaixaEstoque(
    EstoqueDbContext contexto)
{
    public async Task<ResultadoBaixaEstoque> BaixarAsync(
        IReadOnlyCollection<ItemBaixaEstoque> itens,
        CancellationToken cancellationToken)
    {
        var itensOrdenados = itens
            .OrderBy(item => item.ProdutoId)
            .ToArray();

        await using var transacao =
            await contexto.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var item in itensOrdenados)
            {
                var linhasAlteradas = await contexto.Produtos
                    .Where(produto =>
                        produto.Id == item.ProdutoId &&
                        produto.Saldo >= item.Quantidade)
                    .ExecuteUpdateAsync(
                        atualizacao => atualizacao.SetProperty(
                            produto => produto.Saldo,
                            produto => produto.Saldo - item.Quantidade),
                        cancellationToken);

                if (linhasAlteradas == 1)
                {
                    continue;
                }

                var saldoDisponivel = await contexto.Produtos
                    .AsNoTracking()
                    .Where(produto => produto.Id == item.ProdutoId)
                    .Select(produto => (int?)produto.Saldo)
                    .SingleOrDefaultAsync(cancellationToken);

                await transacao.RollbackAsync(cancellationToken);

                if (saldoDisponivel is null)
                {
                    return new ResultadoBaixaEstoque(
                        TipoResultadoBaixaEstoque.ProdutoNaoEncontrado,
                        item.ProdutoId);
                }

                return new ResultadoBaixaEstoque(
                    TipoResultadoBaixaEstoque.SaldoInsuficiente,
                    item.ProdutoId,
                    saldoDisponivel);
            }

            await transacao.CommitAsync(cancellationToken);

            return new ResultadoBaixaEstoque(
                TipoResultadoBaixaEstoque.Sucesso);
        }
        catch
        {
            await transacao.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
