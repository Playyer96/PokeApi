using System.Net;
using System.Threading.Tasks;

namespace PokeApi.WebRequest
{
    public interface IWebRequestHandler
    {
        Task<T> GetAsync<T>(string url);
        Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest payload);
        Task<T> DeleteAsync<T>(string url);
        public void AddDefaultRequestHeaders(string key, string value);
    }
}