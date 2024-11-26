using MinimalApi.Dominio.Entidades;
using MinimalApi.DTOs;

namespace MinimalApi.Dominio.Interfaces
{
    public interface IAdministradorServicos
    {
        Administrador? Login(LoginDTO loginDTO);
        Administrador Incluir(Administrador administrador);
        Administrador BuscarPorId(int id);
        List<Administrador> Todos(int? pagina);
        Task RegistrarAsync(Administrador administrador);
    }
}
