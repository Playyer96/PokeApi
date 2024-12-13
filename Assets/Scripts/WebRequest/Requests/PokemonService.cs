using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PokeApi.WebRequest.JsonData;
using UnityEngine;

namespace PokeApi.WebRequest.Requests
{
    public class PokemonService
    {
        private readonly IWebRequestHandler _webRequestHandler;
        private readonly string _pokemonListUrl = $"{WebRequestConstants.BaseURL}/pokemon?limit=151&offset=0";
        private readonly string _pokemonUrl = $"{WebRequestConstants.BaseURL}/pokemon/";

        private const int BatchSize = 50;

        public PokemonService(bool useUnityWebRequestHandler = true)
        {
            _webRequestHandler = useUnityWebRequestHandler ? new UnityWebRequestHandler() : new HttpClientHandler();
            _webRequestHandler.AddDefaultRequestHeaders("Content-type", "application/json");
        }

        public PokemonService(IWebRequestHandler webRequestHandler)
        {
            _webRequestHandler = webRequestHandler;
            _webRequestHandler.AddDefaultRequestHeaders("Content-type", "application/json");
        }

        public async UniTask<Pokemon> FetchPokemon(Pokemon pokemon)
        {
            // Fetch Pokémon data by ID
            var result = await _webRequestHandler.GetAsync<Pokemon>($"{_pokemonUrl}{pokemon.id}");
            if (result == null)
            {
                Debug.LogError($"Failed to fetch Pokémon with ID {pokemon.id}");
            }

            return result;
        }

        public async UniTask<List<Pokemon>> FetchPokemonListAsync()
        {
            Debug.Log("Fetching Pokémon list...");
            List<Pokemon> pokedex = new List<Pokemon>();
            List<UniTask<Pokemon>> fetchTasks = new List<UniTask<Pokemon>>();

            try
            {
                // Fetch the list of Pokémon
                var response = await _webRequestHandler.GetAsync<PokemonListResponse>(_pokemonListUrl);
                if (response == null || response.results == null)
                {
                    Debug.LogError("Failed to fetch Pokémon list or received invalid response.");
                    return null;
                }

                // Add tasks to fetch each Pokémon asynchronously
                foreach (var result in response.results)
                {
                    fetchTasks.Add(FetchPokemonByUrlAsync(result.url));
                }

                // Batch the requests to avoid overwhelming the server
                using (var semaphore = new SemaphoreSlim(BatchSize))
                {
                    List<UniTask<Pokemon>> currentBatch = new List<UniTask<Pokemon>>();

                    foreach (var task in fetchTasks)
                    {
                        await semaphore.WaitAsync();
                        currentBatch.Add(task.ContinueWith(pokemon =>
                        {
                            semaphore.Release();
                            return pokemon;
                        }));
                    }

                    // Wait for all requests to complete
                    var results = await UniTask.WhenAll(currentBatch);

                    // Filter out the valid Pokémon based on their IDs
                    foreach (var pokemon in results)
                    {
                        if (pokemon != null && pokemon.id >= 1 && pokemon.id <= 151)
                        {
                            pokedex.Add(pokemon);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fetching Pokémon list: {e.Message}");
            }

            Debug.Log($"Fetched {pokedex.Count} Pokémon.");
            return pokedex;
        }

        private async UniTask<Pokemon> FetchPokemonByUrlAsync(string url)
        {
            try
            {
                return await _webRequestHandler.GetAsync<Pokemon>(url);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fetching Pokémon data from URL {url}: {e.Message}");
                return null;
            }
        }

        public async UniTask<Texture2D> FetchTextureAsync(string url)
        {
            // Fetch texture asynchronously
            var texture = await _webRequestHandler.FetchTextureAsync(url);
            if (texture == null)
            {
                Debug.LogError($"Failed to fetch texture from URL {url}");
            }

            return texture;
        }
    }
}