using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace PokeApi.WebRequest
{
    public interface IWebRequestHandler
    {
        public void AddDefaultRequestHeaders(string key, string value);
        Task<T> GetAsync<T>(string url);
        Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest payload);
        Task<T> DeleteAsync<T>(string url);
        Task<Texture2D> FetchTextureAsync(string url);
    }
}