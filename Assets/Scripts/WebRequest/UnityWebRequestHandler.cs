using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace PokeApi.WebRequest
{
    public class UnityWebRequestHandler : IWebRequestHandler
    {
        private readonly Dictionary<string, string> _globalHeaders = new Dictionary<string, string>();

        public void AddDefaultRequestHeaders(string key, string value)
        {
            if (!_globalHeaders.ContainsKey(key))
            {
                _globalHeaders[key] = value;
            }
        }

        public UnityWebRequest ApplyGlobalHeaders(UnityWebRequest unityWebRequest)
        {
            foreach (var hearder in _globalHeaders)
            {
                unityWebRequest.SetRequestHeader(hearder.Key, hearder.Value);
            }

            return unityWebRequest;
        }

        public async UniTask<T> GetAsync<T>(string url)
        {
            using UnityWebRequest request = UnityWebRequest.Get(url);

            ApplyGlobalHeaders(request);

            await request.SendWebRequest().ToUniTask();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception(request.error);
            }

            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }

        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest payload)
        {
            string json = JsonUtility.ToJson(payload);
            byte[] data = Encoding.UTF8.GetBytes(json);

            using UnityWebRequest request = UnityWebRequest.PostWwwForm(url, "application/json");

            ApplyGlobalHeaders(request);
            request.uploadHandler = new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest().ToUniTask();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception(request.error);
            }

            return JsonUtility.FromJson<TResponse>(request.downloadHandler.text);
        }

        public async UniTask<T> DeleteAsync<T>(string url)
        {
            using UnityWebRequest request = UnityWebRequest.Delete(url);
            ApplyGlobalHeaders(request);

            await request.SendWebRequest().ToUniTask();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new Exception(request.error);
            }

            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }

        public async UniTask<Texture2D> FetchTextureAsync(string url)
        {
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            await request.SendWebRequest().ToUniTask();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to fetch texture: {request.error}");
                return null;
            }

            return DownloadHandlerTexture.GetContent(request);
        }
    }
}