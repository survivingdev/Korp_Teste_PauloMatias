using Microsoft.EntityFrameworkCore;
using ServicoEstoque.Api.Dados;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var stringConexao = builder.Configuration.GetConnectionString("Estoque")
    ?? throw new InvalidOperationException(
        "A string de conexão 'Estoque' não foi configurada.");

builder.Services.AddDbContext<EstoqueDbContext>(opcoes =>
    opcoes.UseNpgsql(stringConexao));

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
