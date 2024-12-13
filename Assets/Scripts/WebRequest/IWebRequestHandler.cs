using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PokeApi.WebRequest
{
    public interface IWebRequestHandler
    {
        public void AddDefaultRequestHeaders(string key, string value);
        UniTask<T> GetAsync<T>(string url);
        UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest payload);
        UniTask<T> DeleteAsync<T>(string url);
        UniTask<Texture2D> FetchTextureAsync(string url);
    }
}