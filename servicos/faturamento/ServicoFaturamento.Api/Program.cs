using Microsoft.EntityFrameworkCore;
using ServicoFaturamento.Api.Dados;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var stringConexao = builder.Configuration.GetConnectionString("Faturamento")
    ?? throw new InvalidOperationException(
        "A string de conexão 'Faturamento' não foi configurada.");

builder.Services.AddDbContext<FaturamentoDbContext>(opcoes =>
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
