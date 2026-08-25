using Labb__Pokémon.Models;

namespace Labb__Pokémon.Service
{
    public interface IPokemonService
    {
        Task<PokemonViewModel?> GetPokemon(string search);
        Task<List<PokemonViewModel>> GetPokemonList(int limit = 20);
        Task<List<PokemonViewModel>> GetPokemonByType(string type, int limit = 20);
    }
}
