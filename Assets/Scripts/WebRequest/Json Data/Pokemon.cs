using System;
using System.Collections.Generic;

namespace PokeApi.WebRequest.JsonData
{
    [Serializable]
    public class PokemonListResponse
    {
        public List<PokemonResult> results;
        public string next;
    }

    [Serializable]
    public class PokemonResult
    {
        public string name;
        public string url;
    }

    [Serializable]
    public class Pokemon
    {
        public int id;
        public string name;
        public int height;
        public int weight;
        public List<PokemonType> types;
        public byte[] imageData;
    }

    [Serializable]
    public class PokemonType
    {
        public TypeInfo type;
    }

    [Serializable]
    public class TypeInfo
    {
        public string name;
    }
}