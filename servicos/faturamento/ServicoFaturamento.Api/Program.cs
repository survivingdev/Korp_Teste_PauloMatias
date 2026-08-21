using Microsoft.EntityFrameworkCore;
using ServicoFaturamento.Api.Dados;
using ServicoFaturamento.Api.Integracoes.Estoque;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var stringConexao = builder.Configuration.GetConnectionString("Faturamento")
    ?? throw new InvalidOperationException(
        "A string de conexão 'Faturamento' não foi configurada.");

builder.Services.AddDbContext<FaturamentoDbContext>(opcoes =>
    opcoes.UseNpgsql(stringConexao));

var urlBaseEstoque = builder.Configuration["Servicos:Estoque:UrlBase"]
    ?? throw new InvalidOperationException(
        "A URL do Serviço de Estoque não foi configurada.");

if (!Uri.TryCreate(
        urlBaseEstoque.TrimEnd('/') + "/",
        UriKind.Absolute,
        out var uriEstoque))
{
    throw new InvalidOperationException(
        "A URL configurada para o Serviço de Estoque é inválida.");
}

builder.Services.AddHttpClient<ClienteEstoque>(cliente =>
{
    cliente.BaseAddress = uriEstoque;
    cliente.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
