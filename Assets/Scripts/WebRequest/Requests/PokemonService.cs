using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using PokeApi.WebRequest.JsonData;
using UnityEngine;

namespace PokeApi.WebRequest.Requests
{
    public class PokemonService
    {
        private readonly WebRequestManager _webRequestManager;
        private readonly string _pokemonListUrl = $"{WebRequestConstants.BaseURL}/pokemon?limit=151&offset=0";
        private readonly string _pokemonUrl = $"{WebRequestConstants.BaseURL}/pokemon/";

        private const int BatchSize = 50;

        public PokemonService()
        {
            _webRequestManager = new WebRequestManager();
            _webRequestManager.AddGlobalHeader("Content-type", "application/json");
        }

        public async Task<Pokemon> FetchPokemon(Pokemon pokemon)
        {
            return await _webRequestManager.GetAsync<Pokemon>($"{_pokemonUrl}{pokemon.id}");
        }

        public async Task<List<Pokemon>> FetchPokemonListAsync()
        {
            Debug.Log("Fetching Pokémon list...");
            List<Pokemon> pokedex = new List<Pokemon>();
            List<Task<Pokemon>> fetchTasks = new List<Task<Pokemon>>();

            try
            {
                // Fetch the initial list of Pokémon names and URLs
                var response = await _webRequestManager.GetAsync<PokemonListResponse>(_pokemonListUrl);
                if (response == null || response.results == null)
                {
                    Debug.LogError("Failed to fetch Pokémon list or received invalid response.");
                    return null;
                }

                // Create tasks to fetch the data for each Pokémon
                foreach (var result in response.results)
                {
                    fetchTasks.Add(FetchPokemonAsync(result.url));
                }

                // Process tasks in batches with a limited degree of parallelism
                using (SemaphoreSlim semaphore = new SemaphoreSlim(BatchSize))
                {
                    List<Task<Pokemon>> currentBatch = new List<Task<Pokemon>>();

                    foreach (var task in fetchTasks)
                    {
                        await semaphore.WaitAsync();
                        currentBatch.Add(task);

                        task.ContinueWith(t => semaphore.Release());
                    }

                    // Wait for all tasks in the batch to complete
                    var results = await Task.WhenAll(currentBatch);

                    // Filter out only the first 151 Pokémon (first generation)
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

        private async Task<Pokemon> FetchPokemonAsync(string url)
        {
            try
            {
                return await _webRequestManager.GetAsync<Pokemon>(url);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error fetching Pokémon data from URL {url}: {e.Message}");
                return null; // Return null or handle error appropriately
            }
        }

        public async Task<Texture2D> FetchTextureAsync(string url)
        {
            return await _webRequestManager.FetchTextureAsync(url);
        }
    }
}