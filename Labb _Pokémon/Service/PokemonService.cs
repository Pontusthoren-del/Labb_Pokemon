using Labb__Pokémon.Models;
using System.Text.Json;

namespace Labb__Pokémon.Service
{
    public class PokemonService : IPokemonService
    {
        // httpClient kommer in via DI, kopplad till BaseAddress i Program.cs
        private readonly HttpClient _httpClient;

        public PokemonService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // hämtar EN pokemon på namn, används både av sök och detaljsidan
        public async Task<PokemonViewModel?> GetPokemon(string search)
        {
            var url = $"pokemon/{search.ToLower()}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode(); // kastar fel om t.ex. 404

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // så name/Name matchar oavsett skiftläge
                };

                // packar upp JSON:en med hjälp av wrapper-klassen
                var data = JsonSerializer.Deserialize<PokemonApiResponse>(json, options);

                if (data == null)
                {
                    return null;
                }

                // här mappar jag om från wrapper-datan till min egen ViewModel
                // det är den här delen som gör att views inte behöver bry sig
                // om hur PokéAPI:s JSON faktiskt ser ut
                return new PokemonViewModel
                {
                    Name = data.Name,
                    ImageUrl = data.Sprites?.FrontDefault,
                    Height = data.Height / 10.0, // dm -> m
                    Weight = data.Weight / 10.0, // hg -> kg
                    Types = data.Types?
                    .Where(t => t.Type?.Name != null)
                    .Select(t => t.Type!.Name!)
                    .ToList() ?? new List<string>(),
                    Stats = data.Stats?
                    .Where(s => s.Stat?.Name != null)
                    .ToDictionary(s => s.Stat!.Name!, s => s.BaseStat) ?? new Dictionary<string, int>()
                };
            }
            catch
            {
                // täcker både "pokemon finns inte" (404) och "API:t är nere"
                return null;
            }
        }

        // hämtar en lista av slumpade pokemons, används på "Pokémon List"-sidan
        public async Task<List<PokemonViewModel>> GetPokemonList(int limit = 20)
        {
            var result = new List<PokemonViewModel>();
            var random = new Random();
            var offset = random.Next(0, 1000); // slumpar var i listan vi börjar

            try
            {
                var url = $"pokemon?limit={limit}&offset={offset}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                var listData = JsonSerializer.Deserialize<PokemonListApiResponse>(json, options);

                if (listData?.Results == null)
                {
                    return result;
                }

                // listan ger bara namn, så jag måste hämta varje pokemon
                // för sig för att få bilden också (GetPokemon gör hela mappningen)
                foreach (var entry in listData.Results)
                {
                    if (entry.Name == null) continue;

                    var details = await GetPokemon(entry.Name);
                    if (details != null)
                    {
                        result.Add(details);
                    }
                }
                return result;
            }
            catch
            {
                return result; // tom lista om nåt går fel
            }
        }

        // hämtar alla pokemons av en viss typ, t.ex. "fire" eller "water"
        public async Task<List<PokemonViewModel>> GetPokemonByType(string type, int limit = 20)
        {
            var result = new List<PokemonViewModel>();

            try
            {
                var url = $"type/{type.ToLower()}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                var typeData = JsonSerializer.Deserialize<PokemonTypeApiResponse>(json, options);

                if (typeData?.Pokemon == null)
                {
                    return result;
                }

                // en typ kan ha typ 100+ pokemons, så jag tar bara de första "limit" st
                // annars blir det väldigt många API-anrop och sidan blir seg
                foreach (var entry in typeData.Pokemon.Take(limit))
                {
                    var name = entry.Pokemon?.Name;
                    if (name == null) continue;

                    var details = await GetPokemon(name);
                    if (details != null)
                    {
                        result.Add(details);
                    }
                }
                return result;
            }
            catch
            {
                return result;
            }
        }
    }
}