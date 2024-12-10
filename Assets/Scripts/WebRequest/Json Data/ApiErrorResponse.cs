using System;

namespace PokeApi.WebRequest.JsonData
{
    [Serializable]
    public class ApiErrorResponse
    {
        public string error { get; set; }
    }
}