using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OllamaSharp;
using OllamaSharp.Models;

namespace 淼喵妙神奇工具库
{
    public static class 嵌入服务
    {
        private static readonly object _锁 = new object();
        private static OllamaApiClient _缓存Client;
        private static Uri _缓存地址;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        private const int 最大批次大小 = 32;

        public static async Task<float[]> 生成嵌入(string 文本, AIConfigData config, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(文本))
                return Array.Empty<float>();
            var results = await 批量生成嵌入(new[] { 文本 }, config, cancellationToken).ConfigureAwait(false);
            return results.Count > 0 ? results[0] : Array.Empty<float>();
        }

        public static async Task<List<float[]>> 批量生成嵌入(IList<string> 文本列表, AIConfigData config, CancellationToken cancellationToken = default)
        {
            var result = new List<float[]>();
            if (文本列表 == null || 文本列表.Count == 0) return result;
            if (config == null || string.IsNullOrEmpty(config.Ollama地址))
                return result;

            var 模型名 = !string.IsNullOrEmpty(config.嵌入模型) ? config.嵌入模型 : "nomic-embed-text";

            try
            {
                var client = 获取Client(config.Ollama地址);
                for (int i = 0; i < 文本列表.Count; i += 最大批次大小)
                {
                    var batch = 文本列表.Skip(i).Take(最大批次大小).ToList();
                    var request = new EmbedRequest
                    {
                        Model = 模型名,
                        Input = batch,
                        Truncate = true
                    };
                    var response = await client.EmbedAsync(request, cancellationToken).ConfigureAwait(false);
                    if (response?.Embeddings != null)
                    {
                        foreach (var emb in response.Embeddings)
                            result.Add(emb.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"嵌入服务异常: {ex.Message}");
            }
            return result;
        }

        public static async Task<float[]> 生成嵌入远程(string 文本, AIConfigData config, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(文本) || config == null || string.IsNullOrEmpty(config.远程API地址))
                return Array.Empty<float>();

            try
            {
                var 模型名 = !string.IsNullOrEmpty(config.嵌入模型) ? config.嵌入模型 : "nomic-embed-text";
                var requestBody = new
                {
                    model = 模型名,
                    input = 文本
                };
                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, config.远程API地址.TrimEnd('/') + "/embeddings")
                {
                    Content = content
                };

                if (!string.IsNullOrEmpty(config.加密API密钥))
                {
                    request.Headers.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", 加密密钥(config.加密API密钥));
                }

                var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return Array.Empty<float>();

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var first = data[0];
                    if (first.TryGetProperty("embedding", out var embedding) && embedding.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var result = new float[embedding.GetArrayLength()];
                        int idx = 0;
                        foreach (var val in embedding.EnumerateArray())
                            result[idx++] = val.GetSingle();
                        return result;
                    }
                }
                return Array.Empty<float>();
            }
            catch
            {
                return Array.Empty<float>();
            }
        }

        private static OllamaApiClient 获取Client(string ollamaAddress)
        {
            lock (_锁)
            {
                var uri = new Uri(ollamaAddress);
                if (_缓存Client != null && _缓存地址 == uri)
                    return _缓存Client;
                _缓存地址 = uri;
                _缓存Client = new OllamaApiClient(_缓存地址);
                return _缓存Client;
            }
        }

        private static string 加密密钥(string encrypted)
        {
            try
            {
                return 淼喵妙神奇工具库.AI配置管理器.解密密钥(encrypted);
            }
            catch
            {
                return encrypted;
            }
        }
    }
}
