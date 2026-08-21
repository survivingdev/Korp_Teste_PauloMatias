using Microsoft.EntityFrameworkCore;
using ServicoEstoque.Api.Aplicacao;
using ServicoEstoque.Api.Dados;

var builder = WebApplication.CreateBuilder(args);

const string politicaCorsInterface = "InterfaceAngular";

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(opcoes =>
{
    opcoes.AddPolicy(
        politicaCorsInterface,
        politica =>
        {
            politica
                .WithOrigins(
                    "http://localhost:4200",
                    "http://127.0.0.1:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var stringConexao = builder.Configuration.GetConnectionString("Estoque")
    ?? throw new InvalidOperationException(
        "A string de conexão 'Estoque' não foi configurada.");

builder.Services.AddDbContext<EstoqueDbContext>(opcoes =>
    opcoes.UseNpgsql(stringConexao));

builder.Services.AddScoped<ServicoBaixaEstoque>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(politicaCorsInterface);

app.UseAuthorization();

app.MapControllers();

app.Run();
