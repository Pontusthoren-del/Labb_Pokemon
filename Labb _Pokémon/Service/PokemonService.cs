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

        // Gemensam hjälpmetod: gör ett GET-anrop och hanterar fel enhetligt.
        // Returnerar json-strängen vid success, null vid 404,
        // men kastar vidare (throw) om API:t är nere/timeout.
        private async Task<string?> GetJsonAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // finns inte - normalt scenario
            }
            catch (HttpRequestException)
            {
                throw; // API:t nere - skickas vidare till controllern
            }
            catch (TaskCanceledException)
            {
                throw; // timeout - skickas vidare till controllern
            }
        }

        // hämtar EN pokemon på namn, används både av sök och detaljsidan
        public async Task<PokemonViewModel?> GetPokemon(string search)
        {
            var url = $"pokemon/{search.ToLower()}";

            var json = await GetJsonAsync(url);
            if (json == null)
            {
                return null;
            }

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
                .ToDictionary(s => s.Stat!.Name!, s => s.BaseStat) ?? new Dictionary<string, int>(),
                EvolutionSteps = await GetEvolutionChain(data.Name!)

            };
        }

        // hämtar en lista av slumpade pokemons, används på "Pokémon List"-sidan
        public async Task<List<PokemonViewModel>> GetPokemonList(int limit = 20)
        {
            var result = new List<PokemonViewModel>();
            var random = new Random();
            var offset = random.Next(0, 1000); // slumpar var i listan vi börjar

            var url = $"pokemon?limit={limit}&offset={offset}";
            var json = await GetJsonAsync(url);
            if (json == null)
            {
                return result;
            }

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

        // hämtar alla pokemons av en viss typ, t.ex. "fire" eller "water"
        public async Task<List<PokemonViewModel>> GetPokemonByType(string type)
        {
            var result = new List<PokemonViewModel>();

            var types = type.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLower())
                .ToList();

            if (types.Count == 0)
            {
                return result;
            }

            // Hämta namn-listan för varje typ
            var nameListsPerType = new List<HashSet<string>>();

            foreach (var t in types)
            {
                var url = $"type/{t}";
                var json = await GetJsonAsync(url);
                if (json == null)
                {
                    // om en av typerna inte finns/misslyckas, kan vi inte matcha - returnera tomt
                    return result;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var typeData = JsonSerializer.Deserialize<PokemonTypeApiResponse>(json, options);

                var names = typeData?.Pokemon?
                    .Select(p => p.Pokemon?.Name)
                    .Where(n => n != null)
                    .Select(n => n!)
                    .ToHashSet() ?? new HashSet<string>();

                nameListsPerType.Add(names);
            }

            // Snittet: bara namn som finns i ALLA listorna
            var commonNames = nameListsPerType
                .Aggregate((a, b) => a.Intersect(b).ToHashSet());

            foreach (var name in commonNames)
            {
                var details = await GetPokemon(name);
                if (details != null)
                {
                    result.Add(details);
                }
            }

            return result;
        }

        // söker på delsträng i namnet (t.ex. "pika" hittar "pikachu").
        // Hämtar hela namn-listan från PokeAPI, filtrerar lokalt med
        // Contains, och hämtar sen full data bara för träffarna
        public async Task<List<PokemonViewModel>> SearchPokemonByName(string query)
        {
            var result = new List<PokemonViewModel>();

            if (string.IsNullOrWhiteSpace(query))
            {
                return result;
            }

            var url = "pokemon?limit=2000";
            var json = await GetJsonAsync(url);
            if (json == null)
            {
                return result;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var listData = JsonSerializer.Deserialize<PokemonListApiResponse>(json, options);

            if (listData?.Results == null)
            {
                return result;
            }

            // filtrerar lokalt eftersom PokeAPI inte har ett sök-endpoint
            var matches = listData.Results
                .Where(p => p.Name != null && p.Name.Contains(query.ToLower()))
                .Take(20) // begränsar antal fulla uppslag så sidan inte blir seg
                .ToList();

            foreach (var match in matches)
            {
                var details = await GetPokemon(match.Name!);
                if (details != null)
                {
                    result.Add(details);
                }
            }
            return result;
        }
        public async Task<List<EvolutionStepViewModel>> GetEvolutionChain(string pokemonName)
        {
            var names = new List<string>();

            var speciesJson = await GetJsonAsync($"pokemon-species/{pokemonName.ToLower()}");
            if (speciesJson == null)
            {
                return new List<EvolutionStepViewModel>();
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var speciesData = JsonSerializer.Deserialize<PokemonSpeciesApiResponse>(speciesJson, options);

            var chainUrl = speciesData?.EvolutionChain?.Url;
            if (chainUrl == null)
            {
                return new List<EvolutionStepViewModel>();
            }

            var relativeUrl = chainUrl.Replace(_httpClient.BaseAddress!.ToString(), "");
            var chainJson = await GetJsonAsync(relativeUrl);
            if (chainJson == null)
            {
                return new List<EvolutionStepViewModel>();
            }

            var chainData = JsonSerializer.Deserialize<EvolutionChainApiResponse>(chainJson, options);
            CollectEvolutionNames(chainData?.Chain, names);

            // Hämta bild för varje namn i kedjan (lätt anrop, bara sprite behövs)
            var steps = new List<EvolutionStepViewModel>();
            foreach (var name in names)
            {
                var json = await GetJsonAsync($"pokemon/{name}");
                string? imageUrl = null;

                if (json != null)
                {
                    var data = JsonSerializer.Deserialize<PokemonApiResponse>(json, options);
                    imageUrl = data?.Sprites?.FrontDefault;
                }

                steps.Add(new EvolutionStepViewModel
                {
                    Name = name,
                    ImageUrl = imageUrl,
                    IsCurrent = name.Equals(pokemonName, StringComparison.OrdinalIgnoreCase)
                });
            }

            return steps;
        }

        // Går igenom evolutionsträdet och lägger till varje namn i listan.
        // Rekursiv eftersom en pokémon kan ha flera evolutioner (grenar)
        private void CollectEvolutionNames(EvolutionNodeDto? node, List<string> names)
        {
            if (node?.Species?.Name == null)
            {
                return;
            }

            names.Add(node.Species.Name);

            if (node.EvolvesTo != null)
            {
                foreach (var next in node.EvolvesTo)
                {
                    CollectEvolutionNames(next, names);
                }
            }
        }
    }
}