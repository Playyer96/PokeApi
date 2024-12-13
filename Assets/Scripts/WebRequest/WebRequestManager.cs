using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PokeApi.WebRequest
{
    public class WebRequestManager
    {
        private readonly IWebRequestHandler _webRequestHandler;

        public WebRequestManager(bool useUnityWebRequestHandler = true)
        {
            _webRequestHandler = useUnityWebRequestHandler ? new UnityWebRequestHandler() : new HttpClientHandler();
        }

        public void AddGlobalHeader(string key, string value)
        {
            switch (_webRequestHandler)
            {
                case HttpClientHandler httpClientHandler:
                    httpClientHandler.AddDefaultRequestHeaders(key, value);
                    break;
                case UnityWebRequestHandler unityWebRequestHandler:
                    unityWebRequestHandler.AddDefaultRequestHeaders(key, value);
                    break;
            }
        }

        public UniTask GetAsync<T>(string url) => _webRequestHandler.GetAsync<T>(url).AsUniTask();

        public UniTask PostAsync<TRequest, TResponse>(string url, TRequest request) =>
            _webRequestHandler.PostAsync<TRequest, TResponse>(url, request).AsUniTask();

        public UniTask DeleteAsync<T>(string url) => _webRequestHandler.DeleteAsync<T>(url).AsUniTask();
        public UniTask FetchTextureAsync(string url) => _webRequestHandler.FetchTextureAsync(url).AsUniTask();
    }
}