using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using JetBrains.Annotations;
using PokeApi.WebRequest.JsonData;
using UnityEngine;

namespace PokeApi.WebRequest.Requests
{
    public class PokemonService
    {
        private readonly WebRequestManager _webRequestManager;
        private readonly string PokemonListUrl = $"{WebRequestConstants.BaseURL}/pokemon?limit=151&offset=0"; // Adjusted to limit to the first 151 Pokémon
        private readonly int MaxDegreeOfParallelism = 10; // Set the degree of parallelism as needed

        public PokemonService()
        {
            _webRequestManager = new WebRequestManager();
            _webRequestManager.AddGlobalHeader("Content-type", "application/json");
        }

        [CanBeNull]
        public async Task<List<Pokemon>> FetchPokemonListAsync()
        {
            Debug.Log("Fetching Pokémon list...");
            List<Pokemon> pokedex = new List<Pokemon>();
            List<Task<Pokemon>> fetchTasks = new List<Task<Pokemon>>();

            try
            {
                // Fetch the initial list of Pokémon names and URLs
                var response = await _webRequestManager.GetAsync<PokemonListResponse>(PokemonListUrl);
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
                using (SemaphoreSlim semaphore = new SemaphoreSlim(MaxDegreeOfParallelism))
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
    }
}