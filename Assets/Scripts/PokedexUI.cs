using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using System.Net.Http;
using System.Threading;
using PokeApi.WebRequest.JsonData;
using PokeApi.WebRequest.Requests;

namespace DefaultNamespace
{
    public class PokedexUI : MonoBehaviour
    {
        [SerializeField] private UIDocument pokedexUIDocument;
        [SerializeField] private VisualTreeAsset pokemonUITemplate;

        private PokemonService pokemonService;
        private VisualElement pokemonsHolder;
        private static readonly HttpClient httpClient = new HttpClient();

        private void Start()
        {
            pokemonService = new PokemonService();

            // Get the PokemonsHolder VisualElement from the main Pokédex UI
            pokemonsHolder = pokedexUIDocument.rootVisualElement.Q<VisualElement>("PokemonsHolder");

            if (pokemonsHolder == null)
            {
                Debug.LogError("PokemonsHolder not found in Pokedex UI Document!");
                return;
            }

            // Load Pokémon data
            _ = LoadPokedexAsync();
        }

        private async Task LoadPokedexAsync()
        {
            Debug.Log("Starting to load Pokémon...");
            List<Pokemon> pokedex = await pokemonService.FetchPokemonListAsync();

            if (pokedex == null || pokedex.Count == 0)
            {
                Debug.LogError("No Pokémon data received!");
                return;
            }

            Debug.Log($"Fetched {pokedex.Count} Pokémon.");

            foreach (var pokemon in pokedex)
            {
                try
                {
                    Debug.Log($"Processing {pokemon.name}...");
                    await AddPokemonToUIAsync(pokemon);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error adding {pokemon.name} to UI: {e.Message}");
                }
            }
        }

        private async Task AddPokemonToUIAsync(Pokemon pokemon)
        {
            Debug.Log($"Creating UI for {pokemon.name}");

            VisualElement pokemonUI = pokemonUITemplate?.CloneTree();
            if (pokemonUI == null)
            {
                Debug.LogError("Failed to clone Pokémon UI template!");
                return;
            }

            Label pokemonNameLabel = pokemonUI.Q<Label>("PokemonName");
            if (pokemonNameLabel == null)
            {
                Debug.LogError("PokemonName label not found in Pokémon UI template!");
                return;
            }

            pokemonNameLabel.text = $"{pokemon.id} - {pokemon.name}";

            // Fetch the image and set it to the Image element asynchronously
            VisualElement pokemonImageElement = pokemonUI.Q<VisualElement>("Image"); // Adjust the name if needed

            if (pokemonImageElement != null)
            {
                string imageUrl = $"https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/{pokemon.id}.png";

                Texture2D texture = await FetchTextureAsync(imageUrl);

                if (texture != null)
                {
                    // Set the texture as a style background and dispose of the texture to avoid memory issues
                    pokemonImageElement.style.backgroundImage = new StyleBackground(texture);
                    Debug.Log($"Image set for {pokemon.name}");

                    // Dispose of texture if no longer needed elsewhere
                    texture.name = $"PokemonTexture_{pokemon.id}"; // Optional: for debugging purposes
                }
                else
                {
                    Debug.LogError($"Failed to load image for {pokemon.name} from {imageUrl}");
                }
            }
            else
            {
                Debug.LogError("PokemonImage element not found in Pokémon UI template!");
            }

            if (pokemonsHolder != null)
            {
                Debug.Log($"Adding {pokemon.name} to PokemonsHolder.");
                pokemonsHolder.Add(pokemonUI);
            }
            else
            {
                Debug.LogError("PokemonsHolder is null! Cannot add Pokémon UI.");
            }
        }

        private async Task<Texture2D> FetchTextureAsync(string url)
        {
            try
            {
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10))) // Set a timeout for the request
                {
                    byte[] imageBytes = await httpClient.GetByteArrayAsync(url).WithCancellation(cts.Token);
                    if (imageBytes != null && imageBytes.Length > 0)
                    {
                        Texture2D texture = new Texture2D(2, 2);
                        if (texture.LoadImage(imageBytes))
                        {
                            return texture;
                        }
                        else
                        {
                            Debug.LogError("Failed to load texture from byte array.");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogError($"Timeout reached while fetching image from {url}.");
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"Failed to fetch image from URL {url}: {e.Message}");
            }

            return null;
        }
    }

    public static class HttpClientExtensions
    {
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<T>();
            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                if (task == await Task.WhenAny(task, tcs.Task))
                {
                    return await task; // This will throw if the task was cancelled
                }
                else
                {
                    throw new OperationCanceledException();
                }
            }
        }
    }
}