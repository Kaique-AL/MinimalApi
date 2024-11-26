using MinimalApi.Infraestrutura.Db;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Dominio.Interfaces;
using Microsoft.EntityFrameworkCore;
using MinimalApi.DTOs;

namespace MinimalApi.Dominio.Servicos
{
    public class AdministradorServicos : IAdministradorServicos
    {
        private readonly DbContexto _contexto;

        public AdministradorServicos(DbContexto contexto)
        {
            _contexto = contexto;
        }

        public Administrador Incluir(Administrador administrador)
        {
            _contexto.administradors.Add(administrador);
            _contexto.SaveChanges();

            return administrador;
        }

        public Administrador? Login(LoginDTO loginDTO)
        {
            return _contexto.administradors
                .FirstOrDefault(a => a.Email == loginDTO.Email && a.Senha == loginDTO.Senha);
        }

        public Administrador? BuscarPorId(int id)
        {
            return _contexto.administradors.Where(a => a.Id == id).FirstOrDefault();
        }

        public async Task RegistrarAsync(Administrador administrador)
        {
            await _contexto.administradors.AddAsync(administrador);
            await _contexto.SaveChangesAsync();
        }

        public List<Administrador> Todos(int? pagina)
        {
            var query = _contexto.administradors.AsQueryable();

            int itensPorPagina = 10;

            if(pagina != null)
                query = query.Skip(((int)pagina - 1) * itensPorPagina).Take(itensPorPagina);
        
            return query.ToList();
        }

    }
}
