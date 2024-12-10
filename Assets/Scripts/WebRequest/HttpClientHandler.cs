using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PokeApi.WebRequest
{
    public class HttpClientHandler : IWebRequestHandler
    {
        private static readonly HttpClient client = new HttpClient();

        public void AddDefaultRequestHeaders(string key, string value)
        {
            if (!client.DefaultRequestHeaders.Contains(key))
            {
                client.DefaultRequestHeaders.Add(key, value);
            }
        }

        public async Task<T> GetAsync<T>(string url)
        {
            HttpResponseMessage response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error :{response.StatusCode}");
            }

            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest payload)
        {
            string json = JsonConvert.SerializeObject(payload);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error :{response.StatusCode}");
            }

            string responseJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResponse>(responseJson);
        }

        public async Task<T> DeleteAsync<T>(string url)
        {
            HttpResponseMessage response = await client.DeleteAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error :{response.StatusCode}");
            }

            string json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}