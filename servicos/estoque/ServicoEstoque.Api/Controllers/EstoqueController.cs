using Microsoft.AspNetCore.Mvc;
using ServicoEstoque.Api.Aplicacao;
using ServicoEstoque.Api.Contratos;

namespace ServicoEstoque.Api.Controllers;

[ApiController]
[Route("api/estoque")]
public sealed class EstoqueController(
    ServicoBaixaEstoque servicoBaixaEstoque,
    ILogger<EstoqueController> logger) : ControllerBase
{
    [HttpPost("baixas")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Baixar(
        BaixarEstoqueRequisicao requisicao,
        CancellationToken cancellationToken)
    {
        var itens = requisicao.Itens ?? [];

        if (itens.Count == 0)
        {
            ModelState.AddModelError(
                nameof(requisicao.Itens),
                "A baixa deve possuir pelo menos um item.");
        }

        var produtosDuplicados = itens
            .GroupBy(item => item.ProdutoId)
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => grupo.Key)
            .ToArray();

        if (produtosDuplicados.Length > 0)
        {
            ModelState.AddModelError(
                nameof(requisicao.Itens),
                "Um mesmo produto não pode aparecer mais de uma vez na baixa.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        logger.LogInformation(
            "Processando baixa de estoque da nota fiscal {NotaFiscalNumero} com {QuantidadeItens} item(ns).",
            requisicao.NotaFiscalNumero,
            itens.Count);

        var resultado = await servicoBaixaEstoque.BaixarAsync(
            itens
                .Select(item => new ItemBaixaEstoque(
                    item.ProdutoId,
                    item.Quantidade))
                .ToArray(),
            cancellationToken);

        return resultado.Tipo switch
        {
            TipoResultadoBaixaEstoque.Sucesso =>
                NoContent(),

            TipoResultadoBaixaEstoque.ProdutoNaoEncontrado =>
                NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Produto não encontrado.",
                    Detail = $"O produto {resultado.ProdutoId} não foi encontrado."
                }),

            TipoResultadoBaixaEstoque.SaldoInsuficiente =>
                Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Saldo de estoque insuficiente.",
                    Detail =
                        $"O produto {resultado.ProdutoId} possui saldo " +
                        $"{resultado.SaldoDisponivel}, insuficiente para a baixa solicitada."
                }),

            _ => throw new InvalidOperationException(
                "Resultado de baixa de estoque desconhecido.")
        };
    }
}
