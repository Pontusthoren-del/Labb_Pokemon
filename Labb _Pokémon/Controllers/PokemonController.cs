using Labb__Pokémon.Service;
using Microsoft.AspNetCore.Mvc;

namespace Labb__Pokémon.Controllers
{
    public class PokemonController : Controller
    {

        private readonly IPokemonService _pokemonService;

        public PokemonController(IPokemonService pokemonService)
        {
            _pokemonService = pokemonService;
        }
        public async Task<IActionResult> Index()
        {
            var pokemonList = await _pokemonService.GetPokemonList(20);
            return View(pokemonList);
        }

        public async Task<IActionResult> Search(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return View();
            }
            return RedirectToAction("Details", new { name = search });
        }

        public async Task<IActionResult> SearchByType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return RedirectToAction("Search");
            }
            var pokemonList = await _pokemonService.GetPokemonByType(type, 20);
            ViewBag.SearchedType = type;
            return View("TypeResults", pokemonList);
        }

        public async Task<IActionResult> Details(string name)
        {
            var pokemon = await _pokemonService.GetPokemon(name);

            if (pokemon == null)
            {
                ViewBag.ErrorMessage = $"Kunde inte hitta Pokémon '{name}'";
                return View("NotFound");
            }
            return View(pokemon);
        }
    }
}
