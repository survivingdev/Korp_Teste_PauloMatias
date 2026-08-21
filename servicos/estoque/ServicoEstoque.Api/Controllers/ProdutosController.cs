using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ServicoEstoque.Api.Contratos;
using ServicoEstoque.Api.Dados;
using ServicoEstoque.Api.Dominio;

namespace ServicoEstoque.Api.Controllers;

[ApiController]
[Route("api/produtos")]
public sealed class ProdutosController(EstoqueDbContext contexto) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<ProdutoResposta>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ProdutoResposta>>> Listar(
        CancellationToken cancellationToken)
    {
        var produtos = await contexto.Produtos
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new ProdutoResposta(
                p.Id,
                p.Codigo,
                p.Descricao,
                p.Saldo))
            .ToListAsync(cancellationToken);

        return Ok(produtos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProdutoResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProdutoResposta>> ObterPorId(
        int id,
        CancellationToken cancellationToken)
    {
        var produto = await contexto.Produtos
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProdutoResposta(
                p.Id,
                p.Codigo,
                p.Descricao,
                p.Saldo))
            .SingleOrDefaultAsync(cancellationToken);

        if (produto is null)
        {
            return NotFound();
        }

        return Ok(produto);
    }

    [HttpPost]
    [ProducesResponseType<ProdutoResposta>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProdutoResposta>> Criar(
        CriarProdutoRequisicao requisicao,
        CancellationToken cancellationToken)
    {
        var codigo = requisicao.Codigo.Trim().ToUpperInvariant();
        var descricao = requisicao.Descricao.Trim();

        if (string.IsNullOrWhiteSpace(codigo))
        {
            ModelState.AddModelError(nameof(requisicao.Codigo), "O código é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(descricao))
        {
            ModelState.AddModelError(nameof(requisicao.Descricao), "A descrição é obrigatória.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var codigoJaExiste = await contexto.Produtos
            .AsNoTracking()
            .AnyAsync(p => p.Codigo == codigo, cancellationToken);

        if (codigoJaExiste)
        {
            return ConflitoCodigo(codigo);
        }

        var produto = new Produto
        {
            Codigo = codigo,
            Descricao = descricao,
            Saldo = requisicao.Saldo
        };

        contexto.Produtos.Add(produto);

        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao)
            when (excecao.InnerException is PostgresException postgres &&
                  postgres.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            return ConflitoCodigo(codigo);
        }

        var resposta = new ProdutoResposta(
            produto.Id,
            produto.Codigo,
            produto.Descricao,
            produto.Saldo);

        return CreatedAtAction(
            nameof(ObterPorId),
            new { id = produto.Id },
            resposta);
    }

    private ConflictObjectResult ConflitoCodigo(string codigo)
    {
        return Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Código de produto já cadastrado.",
            Detail = $"Já existe um produto cadastrado com o código '{codigo}'."
        });
    }
}
