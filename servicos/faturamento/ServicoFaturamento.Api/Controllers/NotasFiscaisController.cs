using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicoFaturamento.Api.Contratos;
using ServicoFaturamento.Api.Dados;
using ServicoFaturamento.Api.Dominio;
using ServicoFaturamento.Api.Integracoes.Estoque;

namespace ServicoFaturamento.Api.Controllers;

[ApiController]
[Route("api/notas-fiscais")]
public sealed class NotasFiscaisController(
    FaturamentoDbContext contexto,
    ClienteEstoque clienteEstoque,
    ILogger<NotasFiscaisController> logger) : ControllerBase
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

    [HttpPost("{numero:long}/processar")]
    [ProducesResponseType<NotaFiscalResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<NotaFiscalResposta>> Processar(
        long numero,
        CancellationToken cancellationToken)
    {
        var nota = await contexto.NotasFiscais
            .Include(n => n.Itens)
            .SingleOrDefaultAsync(
                n => n.Numero == numero,
                cancellationToken);

        if (nota is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Nota fiscal não encontrada.",
                Detail = $"A nota fiscal {numero} não foi encontrada."
            });
        }

        if (nota.Status != StatusNotaFiscal.Aberta)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Nota fiscal já processada.",
                Detail = $"A nota fiscal {numero} já está fechada."
            });
        }

        ResultadoBaixaEstoqueRemoto resultado;

        try
        {
            resultado = await clienteEstoque.BaixarAsync(
                nota.Numero,
                nota.Itens
                    .Select(item => new ItemBaixaEstoque(
                        item.ProdutoId,
                        item.Quantidade))
                    .ToArray(),
                cancellationToken);
        }
        catch (TaskCanceledException excecao)
            when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                excecao,
                "Timeout ao comunicar com o Serviço de Estoque para a nota fiscal {Numero}.",
                numero);

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Serviço de Estoque indisponível.",
                    Detail =
                        "Não foi possível processar a nota fiscal porque o Serviço de Estoque não respondeu a tempo. Tente novamente."
                });
        }
        catch (HttpRequestException excecao)
        {
            logger.LogWarning(
                excecao,
                "Falha ao comunicar com o Serviço de Estoque para a nota fiscal {Numero}.",
                numero);

            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Serviço de Estoque indisponível.",
                    Detail =
                        "Não foi possível processar a nota fiscal porque o Serviço de Estoque está indisponível. Tente novamente."
                });
        }

        if (resultado.Tipo == TipoResultadoBaixaEstoqueRemoto.ProdutoNaoEncontrado)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Produto da nota fiscal não encontrado.",
                Detail = resultado.Detalhe
            });
        }

        if (resultado.Tipo == TipoResultadoBaixaEstoqueRemoto.SaldoInsuficiente)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Saldo de estoque insuficiente.",
                Detail = resultado.Detalhe
            });
        }

        nota.Status = StatusNotaFiscal.Fechada;

        await contexto.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Nota fiscal {Numero} processada e fechada com sucesso.",
            numero);

        return Ok(MapearResposta(nota));
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
