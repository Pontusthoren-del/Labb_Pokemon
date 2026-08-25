using System.Text.Json.Serialization;

namespace Labb__Pokémon.Models
{
    // Detta matchar hela JSON-svaret man får från /pokemon/{namn}
    internal class PokemonApiResponse
    {
        public string? Name { get; set; }
        public int Height { get; set; }  
        public int Weight { get; set; }   

        public SpritesDto? Sprites { get; set; }
        public List<TypeEntryDto>? Types { get; set; }
        public List<StatEntryDto>? Stats { get; set; }
    }

    // sprites är ett eget objekt i JSON:en, så det behöver en egen klass
    internal class SpritesDto
    {
        [JsonPropertyName("front_default")]  // JSON skriver det med understreck, C# vill ha PascalCase
        public string? FrontDefault { get; set; }
    }

    // varje post i types-listan ser ut såhär: { "type": { "name": "electric" } }
    internal class TypeEntryDto
    {
        public TypeDto? Type { get; set; }
    }

    // och type-objektet har bara ett namn, men det ligger ett steg längre in
    internal class TypeDto
    {
        public string? Name { get; set; }
    }

    // samma grej med stats, varje post har en siffra + ett namn på statsen
    internal class StatEntryDto
    {
        [JsonPropertyName("base_stat")]
        public int BaseStat { get; set; }
        public StatDto? Stat { get; set; }
    }

    internal class StatDto
    {
        public string? Name { get; set; }
    }

    //  De här är för list-endpointen (/pokemon?limit=...), helt annat svar
    internal class PokemonListApiResponse
    {
        public List<PokemonListEntryDto>? Results { get; set; }
    }

    // listan ger bara namn + url, ingen bild. Därför gör jag ett till anrop
    // per pokemon i GetPokemonList() för att faktiskt hämta bilden
    internal class PokemonListEntryDto
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
    }

    //  Och de här är för type-endpointen (/type/{namn}) 

    internal class PokemonTypeApiResponse
    {
        public List<PokemonTypeEntryDto>? Pokemon { get; set; }
    }

    // lite krångligt: det ligger ett objekt som heter "pokemon" inuti listan "pokemon"
    // men eftersom det bara har namn + url kan jag återanvända samma klass som ovan
    internal class PokemonTypeEntryDto
    {
        public PokemonListEntryDto? Pokemon { get; set; }
    }
}