using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicoFaturamento.Api.Contratos;
using ServicoFaturamento.Api.Dados;
using ServicoFaturamento.Api.Dominio;

namespace ServicoFaturamento.Api.Controllers;

[ApiController]
[Route("api/notas-fiscais")]
public sealed class NotasFiscaisController(
    FaturamentoDbContext contexto) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<NotaFiscalResposta>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<NotaFiscalResposta>>> Listar(
        CancellationToken cancellationToken)
    {
        var notas = await contexto.NotasFiscais
            .AsNoTracking()
            .Include(n => n.Itens)
            .OrderBy(n => n.Numero)
            .ToListAsync(cancellationToken);

        return Ok(notas.Select(MapearResposta).ToList());
    }

    [HttpGet("{numero:long}")]
    [ProducesResponseType<NotaFiscalResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotaFiscalResposta>> ObterPorNumero(
        long numero,
        CancellationToken cancellationToken)
    {
        var nota = await contexto.NotasFiscais
            .AsNoTracking()
            .Include(n => n.Itens)
            .SingleOrDefaultAsync(
                n => n.Numero == numero,
                cancellationToken);

        if (nota is null)
        {
            return NotFound();
        }

        return Ok(MapearResposta(nota));
    }

    [HttpPost]
    [ProducesResponseType<NotaFiscalResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NotaFiscalResposta>> Criar(
        CriarNotaFiscalRequisicao requisicao,
        CancellationToken cancellationToken)
    {
        if (requisicao.Itens.Count == 0)
        {
            ModelState.AddModelError(
                nameof(requisicao.Itens),
                "A nota fiscal deve possuir pelo menos um item.");
        }

        var produtosDuplicados = requisicao.Itens
            .GroupBy(i => i.ProdutoId)
            .Where(grupo => grupo.Count() > 1)
            .Select(grupo => grupo.Key)
            .ToArray();

        if (produtosDuplicados.Length > 0)
        {
            ModelState.AddModelError(
                nameof(requisicao.Itens),
                "Um mesmo produto não pode aparecer mais de uma vez na nota fiscal.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var nota = new NotaFiscal
        {
            Status = StatusNotaFiscal.Aberta,
            CriadaEmUtc = DateTime.UtcNow,
            Itens = requisicao.Itens
                .Select(item => new ItemNotaFiscal
                {
                    ProdutoId = item.ProdutoId,
                    Quantidade = item.Quantidade
                })
                .ToList()
        };

        contexto.NotasFiscais.Add(nota);

        await contexto.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(ObterPorNumero),
            new { numero = nota.Numero },
            MapearResposta(nota));
    }

    private static NotaFiscalResposta MapearResposta(NotaFiscal nota)
    {
        return new NotaFiscalResposta(
            nota.Numero,
            nota.Status.ToString(),
            nota.CriadaEmUtc,
            nota.Itens
                .OrderBy(i => i.Id)
                .Select(i => new ItemNotaFiscalResposta(
                    i.ProdutoId,
                    i.Quantidade))
                .ToList());
    }
}
