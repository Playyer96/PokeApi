using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Cysharp.Threading.Tasks;
using PokeApi.WebRequest;
using PokeApi.WebRequest.JsonData;
using PokeApi.WebRequest.Requests;

namespace PokeApi
{
    public class PokedexUI : MonoBehaviour
    {
        [SerializeField] private UIDocument pokedexUIDocument;
        [SerializeField] private VisualTreeAsset pokemonUITemplate;
        [SerializeField] private VisualTreeAsset pokemonDetailUITemplate;

        private PokemonService _pokemonService;
        private VisualElement _pokemonsContainer;
        private VisualElement _pokemonDetailsContainer;
        private VisualElement _loadingVisualElement;

        private Label _loadingLabel;

        private bool _isFetchingPokemons;

        private void Awake()
        {
            _pokemonService = new PokemonService();

            _pokemonsContainer = pokedexUIDocument.rootVisualElement.Q<VisualElement>("PokemonsHolder");
            _pokemonDetailsContainer = pokedexUIDocument.rootVisualElement.Q<VisualElement>("PokemonDetailsContainer");
            _loadingVisualElement = pokedexUIDocument.rootVisualElement.Q<VisualElement>("LoadingVisualElement");
            _loadingLabel = _loadingVisualElement.Q<Label>("LoadingLabel");
        }

        private void Start()
        {
            // Load Pokémon data
            _ = LoadPokedexAsync();

            _loadingVisualElement.SetEnabled(true);
            _loadingVisualElement.visible = true;

            _pokemonDetailsContainer.RegisterCallback<PointerDownEvent>(evt =>
            {
                _pokemonDetailsContainer.Clear();
                _pokemonDetailsContainer.SetEnabled(false);
                _pokemonDetailsContainer.visible = false;
            });
        }

        private async UniTask LoadPokedexAsync()
        {
            Debug.Log("Starting to load Pokémon...");

            // Enable and show the loading label
            _loadingLabel.text = "Fetching Data ...";
            _loadingVisualElement.SetEnabled(true);
            _loadingVisualElement.visible = true;
            _isFetchingPokemons = true;

            // Start the animated loading label in a separate task
            var loadingAnimationTask = AnimateLoadingLabel();

            List<Pokemon> pokedex = await _pokemonService.FetchPokemonListAsync();

            if (pokedex == null || pokedex.Count == 0)
            {
                Debug.LogError("Failed to fetch Pokémon list or list is empty.");
                return;
            }

            const int batchSize = 30; // Number of Pokémon processed concurrently
            var orderedPokemons = new List<(int index, VisualElement uiElement)>();

            for (int i = 0; i < pokedex.Count; i += batchSize)
            {
                var batch = pokedex.Skip(i).Take(batchSize).ToList();

                // Fetch Pokémon UI elements concurrently and keep their index
                var tasks = batch
                    .Select((pokemon, index) => FetchPokemonUIAsync(pokemon, i + index))
                    .ToList();

                try
                {
                    var batchResults = await UniTask.WhenAll(tasks);
                    orderedPokemons.AddRange(batchResults);
                    Debug.Log(
                        $"Processed batch {i / batchSize + 1} of {Math.Ceiling((double)pokedex.Count / batchSize)}.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing batch {i / batchSize + 1}: {ex.Message}");
                }
            }

            // Add Pokémon to the UI in order
            orderedPokemons.Sort((a, b) => a.index.CompareTo(b.index));
            foreach (var (_, uiElement) in orderedPokemons)
            {
                _pokemonsContainer.Add(uiElement);
            }

            _isFetchingPokemons = false;

            // Stop the loading animation and hide the label
            await loadingAnimationTask;

            _loadingVisualElement.SetEnabled(false);
            _loadingVisualElement.visible = false;
            Debug.Log("Finished loading all Pokémon.");
        }

        private async UniTask AnimateLoadingLabel()
        {
            string baseText = "Fetching Data";
            int dotCount = 0;

            while (_isFetchingPokemons)
            {
                // Update the loading label with the current dot count
                _loadingLabel.text = $"{baseText}{new string('.', dotCount)}";
                dotCount = (dotCount + 1) % 4; // Cycle between 0, 1, 2, 3 dots

                try
                {
                    await UniTask.Delay(500);
                }
                catch (TaskCanceledException)
                {
                    // Task was cancelled, break the loop
                    break;
                }
            }
        }

        private async UniTask<(int index, VisualElement uiElement)> FetchPokemonUIAsync(Pokemon pokemon, int index)
        {
            try
            {
                VisualElement pokemonUI = pokemonUITemplate?.CloneTree();
                Label pokemonNameLabel = pokemonUI.Q<Label>("PokemonName");
                pokemonNameLabel.text = $"{pokemon.id} - {StringUtils.CapitalizeFirstLetter(pokemon.name)}";

                Button pokemonDetailButton = pokemonUI.Q<Button>("PokemonButton");
                VisualElement pokemonImageElement = pokemonUI.Q<VisualElement>("Image");

                string imageUrl = $"{WebRequestConstants.PokemonArtworkBaseUrl}{pokemon.name}{FileFormats.PNG}";
                Texture2D texture = await _pokemonService.FetchTextureAsync(imageUrl);

                pokemonDetailButton.clicked += () => OnClickPokemonDetailButton(pokemon, texture);

                if (pokemonImageElement != null)
                {
                    if (texture != null)
                    {
                        pokemonImageElement.style.backgroundImage = new StyleBackground(texture);
                        texture.name = $"PokemonTexture_{pokemon.id}";
                    }
                    else
                    {
                        Debug.LogError($"Failed to load image for {pokemon.name} from {imageUrl}");
                    }
                }

                return (index, pokemonUI);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error adding Pokémon {pokemon.name}: {ex.Message}");
                return (index, null);
            }
        }

        private void OnClickPokemonDetailButton(Pokemon pokemon, Texture2D texture)
        {
            Debug.Log($"Pokemon clicked: {pokemon.name}");
            _pokemonDetailsContainer.SetEnabled(true);
            _pokemonDetailsContainer.visible = true;

            VisualElement pokemonDetailUI = pokemonDetailUITemplate?.CloneTree();


            Label pokemonNameLabel = pokemonDetailUI.Q<Label>("PokemonName");
            Label pokemonHeightLabel = pokemonDetailUI.Q<Label>("HeightLabel");
            Label pokemonWeightLabel = pokemonDetailUI.Q<Label>("WeightLabel");
            Label pokemonTypesLabel = pokemonDetailUI.Q<Label>("TypesLabel");
            VisualElement pokemonImageElement = pokemonDetailUI.Q<VisualElement>("Image");

            pokemonNameLabel.text = $"ID: {pokemon.id} - {StringUtils.CapitalizeFirstLetter(pokemon.name)}";
            pokemonHeightLabel.text = $"Height: {pokemon.height} m";
            pokemonWeightLabel.text = $"Weight: {pokemon.weight} kg";
            pokemonTypesLabel.text =
                $"Types: {string.Join(", ", pokemon.types.Select(t => StringUtils.CapitalizeFirstLetter(t.type.name)))}";

            pokemonImageElement.style.backgroundImage = new StyleBackground(texture);

            _pokemonDetailsContainer.Add(pokemonDetailUI);
        }
    }
}