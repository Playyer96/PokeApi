using System.Net.Http;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace PokeApi.WebRequest
{
    public class HttpClientHandler : IWebRequestHandler
    {
        private static readonly HttpClient Client = new HttpClient();

        public void AddDefaultRequestHeaders(string key, string value)
        {
            // Only add to DefaultRequestHeaders if key is valid (common headers)
            if (key.ToLower() == "user-agent" || key.ToLower() == "authorization") 
            {
                if (!Client.DefaultRequestHeaders.Contains(key))
                {
                    Client.DefaultRequestHeaders.Add(key, value);
                }
            }
            else
            {
                Debug.LogWarning($"Unsupported header: {key}. Only 'User-Agent' or 'Authorization' headers are allowed.");
            }
        }

        public async UniTask<T> GetAsync<T>(string url)
        {
            HttpResponseMessage response = await Client.GetAsync(url).AsUniTask();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error :{response.StatusCode}");
            }

            string json = await response.Content.ReadAsStringAsync().AsUniTask();
            return JsonConvert.DeserializeObject<T>(json);
        }

        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest payload)
        {
            string json = JsonConvert.SerializeObject(payload);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await Client.PostAsync(url, content).AsUniTask();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error :{response.StatusCode}");
            }

            string responseJson = await response.Content.ReadAsStringAsync().AsUniTask();
            return JsonConvert.DeserializeObject<TResponse>(responseJson);
        }

        public async UniTask<T> DeleteAsync<T>(string url)
        {
            HttpResponseMessage response = await Client.DeleteAsync(url).AsUniTask();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error :{response.StatusCode}");
            }

            string json = await response.Content.ReadAsStringAsync().AsUniTask();
            return JsonConvert.DeserializeObject<T>(json);
        }

        public async UniTask<Texture2D> FetchTextureAsync(string url)
        {
            try
            {
                byte[] imageBytes = await Client.GetByteArrayAsync(url);
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(imageBytes))
                    {
                        return texture;
                    }
                    else
                    {
                        Debug.LogError("Failed to load texture from byte array.");
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"Failed to fetch texture: {e.Message}");
            }

            return null;
        }
    }
}