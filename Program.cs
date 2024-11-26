using MinimalApi.Infraestrutura.Db;
using MinimalApi.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Servicos;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Enuns;

#region builder

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAdministradorServicos, AdministradorServicos>();
builder.Services.AddScoped<IVeiculosServicos, VeiculoServicos>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuração do DbContext com base no tipo de banco de dados (SQL Server, PostgreSQL ou MySQL)
var typeDatabase = builder.Configuration["TypeDatabase"];
var connectionString = builder.Configuration.GetConnectionString(typeDatabase);

switch (typeDatabase)
{
    case "SqlServer":
        builder.Services.AddDbContext<DbContexto>(options =>
            options.UseSqlServer(connectionString));
        break;

    case "Postgresql":
        builder.Services.AddDbContext<DbContexto>(options =>
            options.UseNpgsql(connectionString));
        break;

    case "Mysql":
        builder.Services.AddDbContext<DbContexto>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        break;

    default:
        throw new InvalidOperationException($"Banco de dados {typeDatabase} não é suportado.");
}

var app = builder.Build();

#endregion

#region Home
app.MapGet("/", () => Results.Json(new { Message = "Bem-vindo à API!" })).WithTags("Home");
#endregion

#region Administradors

app.MapPost("/administradors/login", ([FromBody] LoginDTO loginDTO, IAdministradorServicos administradorServicos) =>
{
    
    if (string.IsNullOrEmpty(loginDTO.Email) || string.IsNullOrEmpty(loginDTO.Senha))
    {
        return Results.BadRequest("Email ou senha não podem ser vazios.");
    }

    if (administradorServicos.Login(loginDTO) != null)
    {
        return Results.Ok("Login efetuado com sucesso");
    }
    else
    {
       
        return Results.NotFound(new { message = "O usuário não foi encontrado." });
    }
}).WithTags("Administradores");

app.MapPost("/administradors", async ([FromBody] AdministradorDTO administradorDTO, IAdministradorServicos administradorServicos) =>
{
    
    var validacao = new ErrosDeValidacao
    {
        Mensagens = new List<string>()
    };

    if (string.IsNullOrEmpty(administradorDTO.Email))
        validacao.Mensagens.Add("Email não pode ser vazio");
    if (string.IsNullOrEmpty(administradorDTO.Senha))
        validacao.Mensagens.Add("Senha não pode ser vazia");
    if (administradorDTO.Perfil == null)
        validacao.Mensagens.Add("Perfil não pode ser vazio");

    if (validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    var adm = new Administrador
    {
        Email = administradorDTO.Email,
        Senha = administradorDTO.Senha,
        Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Gerente.ToString()
    };

    await administradorServicos.RegistrarAsync(adm); 

    return Results.Ok(new AdministradorModelView{
        Id = adm.Id,
        Email = adm.Email,
        Perfil = adm.Perfil
    });
}).WithTags("Administradores");

app.MapGet("/administradors", ([FromQuery] int? pagina, IAdministradorServicos administradorServicos) =>
{
    var adms = new List<AdministradorModelView>();
    var administradores = administradorServicos.Todos(pagina);
    foreach(var adm in administradores)
    {
        adms.Add(new AdministradorModelView{
            Id = adm.Id,
            Email = adm.Email,
            Perfil = adm.Perfil
        });
    }
    return Results.Ok(adms); 
}).WithTags("Administradores");


app.MapGet("/administradors/{id}", ([FromRoute] int id, IAdministradorServicos administradorServicos) =>
{
    var administrador = administradorServicos.BuscarPorId(id);
    if (administrador == null) return Results.NotFound(); 
    return Results.Ok(new AdministradorModelView{
        Id = administrador.Id,
        Email = administrador.Email,
        Perfil = administrador.Perfil
    }); 
}).WithTags("Administradores");



#endregion

#region Veiculos

ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
{
    var validacao = new ErrosDeValidacao{
        Mensagens = new List<string>()
    };

    if (string.IsNullOrEmpty(veiculoDTO.Nome))
        validacao.Mensagens.Add("O nome não pode ser vazio");

    if (string.IsNullOrEmpty(veiculoDTO.Marca))
        validacao.Mensagens.Add("A marca não pode ser vazia");

    if (veiculoDTO.Ano < 1950)
        validacao.Mensagens.Add("Veículo muito antigo, aceito somente anos superiores a 1950");

    return validacao;
}


app.MapPost("/veiculos", ([FromBody] VeiculoDTO veiculoDTO, IVeiculosServicos veiculosServicos) =>
{
    var validacao = validaDTO(veiculoDTO); 
    if (validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao);

    var veiculo = new Veiculo
    {
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano
    };

    veiculosServicos.Incluir(veiculo); 

    return Results.Ok(veiculo); 
});


app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculosServicos veiculosServicos) =>
{
    var veiculos = veiculosServicos.Todos(pagina);
    return Results.Ok(veiculos); 
}).WithTags("Veiculos");


app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculosServicos veiculosServicos) =>
{
    var veiculo = veiculosServicos.BuscaPorId(id);
    if (veiculo == null) return Results.NotFound(); 
    return Results.Ok(veiculo); 
}).WithTags("Veiculos");


app.MapPut("/veiculos/{id}", ([FromRoute] int id, VeiculoDTO veiculoDTO, IVeiculosServicos veiculosServicos) =>
{
        
    var veiculo = veiculosServicos.BuscaPorId(id);
    if (veiculo == null) return Results.NotFound(); 

    var validacao = validaDTO(veiculoDTO); 
    if (validacao.Mensagens.Count > 0)
        return Results.BadRequest(validacao); 


    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;

    veiculosServicos.Atualizar(veiculo);

    return Results.Ok(veiculo); 
}).WithTags("Veiculos");


app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculosServicos veiculosServicos) =>
{
    var veiculo = veiculosServicos.BuscaPorId(id);
    if (veiculo == null) return Results.NotFound(); 

    veiculosServicos.Apagar(veiculo); 

    return Results.NoContent(); 
}).WithTags("Veiculos");

#endregion

#region Swagger
app.UseSwagger();
app.UseSwaggerUI();
#endregion

app.Run();

