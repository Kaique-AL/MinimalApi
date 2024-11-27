using MinimalApi.Infraestrutura.Db;
using MinimalApi.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MinimalApi.Dominio.Interfaces;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Servicos;
using MinimalApi.Dominio.ModelViews;
using MinimalApi.Dominio.Enuns;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;

#region builder

var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();

builder.Services.AddAuthentication(option => {
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option => {
        option.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
    };

});



builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServicos, AdministradorServicos>();
builder.Services.AddScoped<IVeiculosServicos, VeiculoServicos>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => 
{
    
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",  
        Type = SecuritySchemeType.Http,  
        Scheme = "Bearer",  
        BearerFormat = "JWT",  
        In = ParameterLocation.Header,
        Description = "Insira o token JWT desta forma: aqui"
    });

    
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"  
                }
            },
            new string[] {}  
        }
    });
});


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
app.MapGet("/", () => Results.Json(new { Message = "Bem-vindo à API!" })).AllowAnonymous().WithTags("Home");
#endregion

#region Administradors

string GerarTokenJwt(Administrador administrador)
{
    if (string.IsNullOrEmpty(key))
    {
        return string.Empty;
    }

    var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

    var claims = new List<Claim>
    {
        new Claim("Email", administrador.Email),
        new Claim("Perfil", administrador.Perfil),
        new Claim(ClaimTypes.Role, administrador.Email)
    };

    var token = new JwtSecurityToken(
        claims: claims,
        expires: DateTime.Now.AddDays(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}


app.MapPost("/administradors/login", ([FromBody] LoginDTO loginDTO, IAdministradorServicos administradorServicos) =>
{
    if (string.IsNullOrEmpty(loginDTO.Email) || string.IsNullOrEmpty(loginDTO.Senha))
    {
        return Results.BadRequest("Email ou senha não podem ser vazios.");
    }

    var administrador = administradorServicos.Login(loginDTO);
    if (administrador != null)
    {
        string token = GerarTokenJwt(administrador);
        return Results.Ok(new AdministradorLogado
        {
            Email = administrador.Email,
            Perfil = administrador.Perfil,
            Token = token
        });
    }
    else
    {
        return Results.NotFound(new { message = "O usuário não foi encontrado." });
    }
}).AllowAnonymous().WithTags("Administradores");

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
})
.RequireAuthorization()
.WithTags("Administradores");

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
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Administradores");


app.MapGet("/administradors/{id}", ([FromRoute] int id, IAdministradorServicos administradorServicos) =>
{
    var administrador = administradorServicos.BuscarPorId(id);
    if (administrador == null) return Results.NotFound(); 
    return Results.Ok(new AdministradorModelView{
        Id = administrador.Id,
        Email = administrador.Email,
        Perfil = administrador.Perfil
    }); 
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Administradores");



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
}).RequireAuthorization().WithTags("Veiculos");


app.MapGet("/veiculos", ([FromQuery] int? pagina, IVeiculosServicos veiculosServicos) =>
{
    var veiculos = veiculosServicos.Todos(pagina);
    return Results.Ok(veiculos); 
}).RequireAuthorization().WithTags("Veiculos");


app.MapGet("/veiculos/{id}", ([FromRoute] int id, IVeiculosServicos veiculosServicos) =>
{
    var veiculo = veiculosServicos.BuscaPorId(id);
    if (veiculo == null) return Results.NotFound(); 
    return Results.Ok(veiculo); 
}).RequireAuthorization().WithTags("Veiculos");


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
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Veiculos");


app.MapDelete("/veiculos/{id}", ([FromRoute] int id, IVeiculosServicos veiculosServicos) =>
{
    var veiculo = veiculosServicos.BuscaPorId(id);
    if (veiculo == null) return Results.NotFound(); 

    veiculosServicos.Apagar(veiculo); 

    return Results.NoContent(); 
})
.RequireAuthorization()
.RequireAuthorization(new AuthorizeAttribute{Roles = "Adm"})
.WithTags("Veiculos");

#endregion

#region Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();
#endregion
