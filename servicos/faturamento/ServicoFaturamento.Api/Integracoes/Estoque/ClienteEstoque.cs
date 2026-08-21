using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace ServicoFaturamento.Api.Integracoes.Estoque;

public sealed record ItemBaixaEstoque(
    int ProdutoId,
    int Quantidade);

public enum TipoResultadoBaixaEstoqueRemoto
{
    Sucesso,
    ProdutoNaoEncontrado,
    SaldoInsuficiente
}

public sealed record ResultadoBaixaEstoqueRemoto(
    TipoResultadoBaixaEstoqueRemoto Tipo,
    string? Detalhe = null);

public sealed class ClienteEstoque(HttpClient httpClient)
{
    public async Task<ResultadoBaixaEstoqueRemoto> BaixarAsync(
        long notaFiscalNumero,
        IReadOnlyCollection<ItemBaixaEstoque> itens,
        CancellationToken cancellationToken)
    {
        var requisicao = new BaixarEstoqueRequisicao(
            notaFiscalNumero,
            itens);

        using var resposta = await httpClient.PostAsJsonAsync(
            "api/estoque/baixas",
            requisicao,
            cancellationToken);

        if (resposta.StatusCode == HttpStatusCode.NoContent)
        {
            return new ResultadoBaixaEstoqueRemoto(
                TipoResultadoBaixaEstoqueRemoto.Sucesso);
        }

        if (resposta.StatusCode == HttpStatusCode.NotFound)
        {
            var problema = await resposta.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    cancellationToken: cancellationToken);

            return new ResultadoBaixaEstoqueRemoto(
                TipoResultadoBaixaEstoqueRemoto.ProdutoNaoEncontrado,
                problema?.Detail);
        }

        if (resposta.StatusCode == HttpStatusCode.Conflict)
        {
            var problema = await resposta.Content
                .ReadFromJsonAsync<ProblemDetails>(
                    cancellationToken: cancellationToken);

            return new ResultadoBaixaEstoqueRemoto(
                TipoResultadoBaixaEstoqueRemoto.SaldoInsuficiente,
                problema?.Detail);
        }

        throw new HttpRequestException(
            $"O Serviço de Estoque respondeu com HTTP {(int)resposta.StatusCode}.",
            null,
            resposta.StatusCode);
    }

    private sealed record BaixarEstoqueRequisicao(
        long NotaFiscalNumero,
        IReadOnlyCollection<ItemBaixaEstoque> Itens);
}
