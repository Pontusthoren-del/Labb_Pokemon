namespace Labb__Pokémon.Models
{
    public class PokemonViewModel
    {
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
        public double Height { get; set; }
        public double Weight { get; set; }
        public List<string> Types { get; set; }
        public Dictionary<string, int> Stats { get; set; }
    }

    public class PokemonListItem
    {
        public string Name { get; set; } = "";
    }
}
