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

        // Startsidan - visar en lista av slumpade pokémon
        public async Task<IActionResult> Index()
        {
            // NYTT: try/catch
            try
            {
                var pokemonList = await _pokemonService.GetPokemonList(20);
                return View(pokemonList);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return HandleServiceUnavailable(ex);
            }
        }

        // Söker på namn. Ett exakt namn skickar direkt till Details,
        // flera träffar (delsträng, t.ex. "pika") visar en resultatlista,
        // inga träffar visar NotFound
        public async Task<IActionResult> Search(string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return View();
            }

            try
            {
                var results = await _pokemonService.SearchPokemonByName(search);

                if (results.Count == 0)
                {
                    ViewBag.ErrorMessage = $"Kunde inte hitta någon Pokémon som matchar '{search}'";
                    return View("NotFound");
                }

                if (results.Count == 1)
                {
                    return RedirectToAction("Details", new { name = results[0].Name });
                }

                ViewBag.SearchedTerm = search;
                return View("SearchResults", results);
            }
            // ÄNDRAT: två catch-block ersatta med samma hjälpmetod som resten
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return HandleServiceUnavailable(ex);
            }
        }

        // Söker på en eller flera typer (kommaseparerat), visar alla
        // pokémon som matchar (AND-logik om flera typer angetts)
        public async Task<IActionResult> SearchByType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return RedirectToAction("Search");
            }

            // NYTT: try/catch
            try
            {
                var pokemonList = await _pokemonService.GetPokemonByType(type);
                ViewBag.SearchedType = type;
                return View("TypeResults", pokemonList);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return HandleServiceUnavailable(ex);
            }
        }

        // Visar detaljsidan för EN specifik pokémon (exakt namn/id)
        public async Task<IActionResult> Details(string name)
        {
            // NYTT: try/catch
            try
            {
                var pokemon = await _pokemonService.GetPokemon(name);

                if (pokemon == null)
                {
                    ViewBag.ErrorMessage = $"Kunde inte hitta Pokémon '{name}'";
                    return View("NotFound");
                }
                return View(pokemon);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                return HandleServiceUnavailable(ex);
            }
        }

        private IActionResult HandleServiceUnavailable(Exception ex)
        {
            // Kolla vilken typ av fel det är, och sätt olika meddelanden beroende på det
            if (ex is TaskCanceledException)
            {
                // Anropet till API:et tog för lång tid (timeout)
                ViewBag.ErrorMessage = "Anropet tog för lång tid, försök igen.";
            }
            else
            {
                // Annars antar vi att API:et är nere/onåbart (HttpRequestException)
                ViewBag.ErrorMessage = "Pokémon-API:et svarar inte just nu.";
            }

            // Visa samma vy i båda fallen, bara meddelandet skiljer sig
            return View("ServiceUnavailable");
        }
    }
}