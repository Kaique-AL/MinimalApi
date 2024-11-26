using Microsoft.EntityFrameworkCore;
using MinimalApi.Dominio.Entidades;

namespace MinimalApi.Infraestrutura.Db;

public class DbContexto : DbContext
{
    private IConfiguration _configuration;

    public DbSet<Administrador> administradors {get; set;}
    public DbSet<Veiculo> Veiculos {get; set;}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Administrador>().HasData(
            new Administrador {
                Id = 1,
                Email = "administrador@teste.com",
                Senha = "123456",
                Perfil = "Adm"
            }
        );
    }

    public DbContexto(IConfiguration configuration, DbContextOptions options) : base(options)
    {
        _configuration = configuration ?? throw new ArgumentException(nameof(configuration));
    }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var typeDatabase = _configuration["TypeDatabase"];
        var connectionString = _configuration.GetConnectionString(typeDatabase);

        if(typeDatabase == "SqlServer")
        {
            optionsBuilder.UseSqlServer(connectionString);
        }

        else if(typeDatabase == "Postgresql")
        {
            optionsBuilder.UseNpgsql(connectionString);
        }

        else if(typeDatabase == "Mysql")
        {
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
    }
}
