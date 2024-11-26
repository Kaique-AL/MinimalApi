using MinimalApi.Dominio.Entidades;
using MinimalApi.DTOs;

namespace MinimalApi.Dominio.Interfaces;


public interface IVeiculosServicos
{
    List<Veiculo> Todos(int? pagina = 1, string? nome = null, string? marca = null);
    Veiculo? BuscaPorId(int id);

    void Incluir(Veiculo veiculo);
    void Atualizar(Veiculo veiculo);
    void Apagar(Veiculo veiculo);
    List<Veiculo> Todos(int paginaAtual, int tamanhoPagina);
    Task<object> IncluirAsync(Veiculo veiculo);
}