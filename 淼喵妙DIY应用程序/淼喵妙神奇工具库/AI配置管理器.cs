using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OllamaSharp;
using OllamaSharp.Models;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库
{
    public static class AI配置管理器
    {
        private static readonly object _锁 = new object();
        private static AIPersistenceData _数据 { get; set; }
        private static string _当前对话Id;
        public static AsyncLocal<string> 当前对话Id上下文 = new AsyncLocal<string>();
        public static string 默认用户名 = "默认用户";
        public static volatile bool 是否训练中 = false;

        private static string 数据目录 => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "淼喵妙脚本DIY");
        private static string 数据文件路径 => Path.Combine(数据目录, "ai_config.json");

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null
        };

        static AI配置管理器()
        {
            加载数据();
        }

        private static void 加载数据()
        {
            try
            {
                if (File.Exists(数据文件路径))
                {
                    string json = File.ReadAllText(数据文件路径);
                    _数据 = JsonSerializer.Deserialize<AIPersistenceData>(json, _jsonOptions) ?? new AIPersistenceData();
                }
                else
                {
                    _数据 = new AIPersistenceData();
                }
            }
            catch
            {
                _数据 = new AIPersistenceData();
            }

            if (_数据.对话列表.Count > 0)
            {
                _当前对话Id = _数据.对话列表[0].Id;
            }

            执行数据迁移();
        }

        private static void 执行数据迁移()
        {
            if (_数据 == null) return;
            bool 需要保存 = false;

            if (_数据.AI配置列表 == null)
                _数据.AI配置列表 = new List<AINamedConfig>();

            if (_数据.AI配置列表.Count == 0 && _数据.全局AI配置 != null && 配置是否有效(_数据.全局AI配置))
            {
                _数据.AI配置列表.Add(new AINamedConfig
                {
                    名称 = "默认配置",
                    配置 = _数据.全局AI配置
                });
                _数据.全局AI配置 = new AIConfigData();
                需要保存 = true;
            }

            if (_数据.AI配置列表.Count == 0)
            {
                _数据.AI配置列表.Add(new AINamedConfig
                {
                    名称 = "默认配置",
                    配置 = new AIConfigData()
                });
                需要保存 = true;
            }

            if (需要保存)
                保存数据();
        }

        public static void 保存数据()
        {
            lock (_锁)
            {
                try
                {
                    if (!Directory.Exists(数据目录))
                        Directory.CreateDirectory(数据目录);

                    string json = JsonSerializer.Serialize(_数据, _jsonOptions);
                    File.WriteAllText(数据文件路径, json);
                }
                catch { }
            }
        }

        public static AIConfigData 获取全局配置()
        {
            var 列表 = _数据.AI配置列表;
            if (列表 != null && 列表.Count > 0)
                return 列表[0].配置 ?? new AIConfigData();
            return _数据.全局AI配置 ?? new AIConfigData();
        }

        public static void 更新全局配置(AIConfigData config)
        {
            lock (_锁)
            {
                var 列表 = _数据.AI配置列表;
                if (列表 == null || 列表.Count == 0)
                {
                    列表 = new List<AINamedConfig> { new AINamedConfig { 名称 = "默认配置", 配置 = config } };
                    _数据.AI配置列表 = 列表;
                }
                else
                {
                    列表[0].配置 = config;
                }
            }
            保存数据();
        }

        public static string 获取全局自定义规则()
        {
            return _数据.全局自定义规则 ?? "始终用中文回复。如任务需要多个步骤，请先简要列出计划，再逐一调用工具执行。";
        }

        public static void 更新全局自定义规则(string 规则)
        {
            lock (_锁)
            {
                _数据.全局自定义规则 = 规则;
            }
            保存数据();
        }

        public static List<AIQuickCommand> 获取全局快捷指令()
        {
            return _数据.全局快捷指令列表 ?? new List<AIQuickCommand>();
        }

        public static void 更新全局快捷指令(List<AIQuickCommand> 指令列表)
        {
            lock (_锁)
            {
                _数据.全局快捷指令列表 = 指令列表 ?? new List<AIQuickCommand>();
            }
            保存数据();
        }

        public static AIConfigData 获取视觉AI配置()
        {
            var cfg = 根据Id获取配置(_数据.视觉AI配置Id);
            return cfg ?? 获取全局配置();
        }

        public static void 更新视觉AI配置Id(string 视觉Id)
        {
            lock (_锁)
            {
                _数据.视觉AI配置Id = 视觉Id;
            }
            保存数据();
        }

        public static void 更新视觉AI配置(AIConfigData config)
        {
            lock (_锁)
            {
                var 列表 = _数据.AI配置列表;
                if (列表 == null || 列表.Count == 0)
                {
                    列表 = new List<AINamedConfig> { new AINamedConfig { 名称 = "视觉配置", 配置 = config } };
                    _数据.AI配置列表 = 列表;
                    _数据.视觉AI配置Id = 列表[0].Id;
                }
                else if (!string.IsNullOrEmpty(_数据.视觉AI配置Id))
                {
                    var item = 列表.FirstOrDefault(i => i.Id == _数据.视觉AI配置Id);
                    if (item != null)
                        item.配置 = config;
                    else
                        列表[0].配置 = config;
                }
                else
                {
                    列表[0].配置 = config;
                }
            }
            保存数据();
        }

        public static bool 获取启用自主学习()
        {
            return _数据.启用自主学习;
        }

        public static void 设置启用自主学习(bool 值)
        {
            lock (_锁)
            {
                _数据.启用自主学习 = 值;
            }
            保存数据();
        }

        public static AIConfigData 获取经验AI配置()
        {
            var cfg = 根据Id获取配置(_数据.经验AI配置Id);
            if (cfg == null) return null;
            var result = new AIConfigData
            {
                Ollama地址 = cfg.Ollama地址,
                Ollama模型 = cfg.Ollama模型
            };
            if (string.IsNullOrEmpty(result.Ollama模型)) return null;
            return result;
        }

        public static void 更新经验AI配置(string 配置Id)
        {
            _数据.经验AI配置Id = 配置Id;
        }

        public static bool 获取启用增量记录()
        {
            return _数据.启用增量记录;
        }

        public static void 设置启用增量记录(bool 值)
        {
            lock (_锁)
            {
                _数据.启用增量记录 = 值;
            }
            保存数据();
        }

        public static bool 获取过滤私有消息()
        {
            return _数据.过滤私有消息;
        }

        public static void 设置过滤私有消息(bool 值)
        {
            lock (_锁)
            {
                _数据.过滤私有消息 = 值;
            }
            保存数据();
        }

        public static List<AIConversation> 获取所有对话()
        {
            return _数据.对话列表;
        }

        public static AIConversation 按名称查找对话(string 名称)
        {
            if (string.IsNullOrEmpty(名称)) return null;
            return _数据.对话列表?.FirstOrDefault(d => d.名称 == 名称);
        }

        public static AIConversation 获取当前对话()
        {
            return _数据.对话列表.FirstOrDefault(d => d.Id == _当前对话Id);
        }

        public static string 获取当前对话Id() => _当前对话Id;

        public static void 设置当前对话(string conversationId)
        {
            if (_数据.对话列表.Any(d => d.Id == conversationId))
            {
                _当前对话Id = conversationId;
            }
        }

        public static AIConversation 创建新对话(string 名称 = "新对话")
        {
            lock (_锁)
            {
                var 对话 = new AIConversation
                {
                    名称 = 名称,
                    文本AI配置Id = _数据.上次文本配置Id,
                    多模态AI配置Id = _数据.上次多模态配置Id
                };
                _数据.对话列表.Add(对话);
                _当前对话Id = 对话.Id;
                保存数据();
                return 对话;
            }
        }

        public static void 删除对话(string conversationId)
        {
            lock (_锁)
            {
                var 对话 = _数据.对话列表.FirstOrDefault(d => d.Id == conversationId);
                if (对话 == null) return;

                _数据.对话列表.Remove(对话);
                if (_当前对话Id == conversationId)
                {
                    _当前对话Id = _数据.对话列表.Count > 0 ? _数据.对话列表[0].Id : null;
                }
                保存数据();
            }
        }

        public static void 更新对话(AIConversation 对话)
        {
            lock (_锁)
            {
                var 现有 = _数据.对话列表.FirstOrDefault(d => d.Id == 对话.Id);
                if (现有 != null)
                {
                    int index = _数据.对话列表.IndexOf(现有);
                    _数据.对话列表[index] = 对话;
                }
            }
            保存数据();
        }

        public static void 追加消息(string conversationId, AIChatMessage message)
        {
            lock (_锁)
            {
                var 对话 = _数据.对话列表.FirstOrDefault(d => d.Id == conversationId);
                对话?.消息列表.Add(message);
            }
            保存数据();
        }

        public static string 加密密钥(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }
            catch
            {
                return "";
            }
        }

        public static string 解密密钥(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return "";
            try
            {
                byte[] data = Convert.FromBase64String(encryptedText);
                byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch
            {
                return "";
            }
        }

        private static string 解析提供者(AIConfigData config)
        {
            if (string.IsNullOrEmpty(config.提供者类型))
            {
                if (!string.IsNullOrEmpty(config.Ollama模型))
                    return "Ollama本地";
                if (!string.IsNullOrEmpty(config.远程API地址) && !string.IsNullOrEmpty(解密密钥(config.加密API密钥)))
                    return "OpenAI 兼容 API";
                return "";
            }
            return config.提供者类型;
        }

        public static async Task<string> 调用AI(AIConfigData config, List<AIChatMessage> 消息历史, string 自定义规则)
        {
            if (config == null) return null;

            if (解析提供者(config) == "Ollama本地")
            {
                return await 调用本地AI(config, 消息历史, 自定义规则);
            }
            else
            {
                return await 调用远程AI(config, 消息历史, 自定义规则);
            }
        }

        private static async Task<string> 调用本地AI(AIConfigData config, List<AIChatMessage> 消息历史, string 自定义规则)
        {
            try
            {
                string host = string.IsNullOrEmpty(config.Ollama地址) ? "http://localhost:11434" : config.Ollama地址;
                string model = string.IsNullOrEmpty(config.Ollama模型) ? "qwen2:0.5b" : config.Ollama模型;

                var 提示词 = 构建提示词(消息历史, 自定义规则);
                var client = new OllamaApiClient(new Uri(host));

                var request = new OllamaSharp.Models.GenerateRequest
                {
                    Model = model,
                    Prompt = 提示词,
                    Stream = true,
                    Options = config.温度.HasValue ? new RequestOptions { Temperature = config.温度.Value } : null
                };

                var sb = new StringBuilder();
                await foreach (var chunk in client.GenerateAsync(request).ConfigureAwait(false))
                {
                    if (chunk == null) continue;
                    if (!string.IsNullOrEmpty(chunk.Response))
                        sb.Append(chunk.Response);
                }

                return sb.Length > 0 ? sb.ToString() : null;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<string> 调用远程AI(AIConfigData config, List<AIChatMessage> 消息历史, string 自定义规则)
        {
            try
            {
                string apiUrl = string.IsNullOrEmpty(config.远程API地址) ? null : config.远程API地址;
                string apiKey = 解密密钥(config.加密API密钥);
                string model = string.IsNullOrEmpty(config.远程模型) ? "gpt-4o-mini" : config.远程模型;

                if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey))
                    return null;

                var messages = new List<object>();

                if (!string.IsNullOrEmpty(自定义规则))
                {
                    messages.Add(new { role = "system", content = 自定义规则 });
                }

                foreach (var msg in 消息历史)
                {
                    string role = msg.角色 switch
                    {
                        "AI" => "assistant",
                        "系统" => "system",
                        _ => "user"
                    };
                    messages.Add(new { role, content = msg.内容 });
                }

                if (messages.Count == 0) return null;

                var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var 请求体 = new Dictionary<string, object>
                {
                    ["model"] = model,
                    ["messages"] = messages,
                    ["max_tokens"] = config.最大输出Token > 0 ? config.最大输出Token : 8192,
                    ["stream"] = true
                };
                if (config.温度.HasValue)
                    请求体["temperature"] = config.温度.Value;

                var jsonContent = new StringContent(JsonSerializer.Serialize(请求体), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(apiUrl, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    var reader = new StreamReader(stream);
                    var sb = new StringBuilder();
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync();
                        if (string.IsNullOrEmpty(line)) continue;
                        if (!line.StartsWith("data: ")) continue;
                        string data = line.Substring(6);
                        if (data == "[DONE]") break;
                        try
                        {
                            using var doc = JsonDocument.Parse(data);
                            var delta = doc.RootElement.GetProperty("choices")[0].GetProperty("delta");
                            if (delta.TryGetProperty("content", out var ct) && ct.GetString() != null)
                                sb.Append(ct.GetString());
                        }
                        catch { }
                    }
                    return sb.Length > 0 ? sb.ToString() : null;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AI非流式] HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
                }
            }
            catch { }
            return null;
        }

        public static string 捕获屏幕截图()
        {
            try
            {
                int width = (int)System.Windows.SystemParameters.PrimaryScreenWidth;
                int height = (int)System.Windows.SystemParameters.PrimaryScreenHeight;
                using var bmp = new Bitmap(width, height);
                using var g = Graphics.FromImage(bmp);
                g.CopyFromScreen(0, 0, 0, 0, new Size(width, height));
                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Png);
                return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            }
            catch
            {
                return null;
            }
        }

        public static async Task<string> 调用AI分析截图(AIConfigData config, string base64Image, string 提示词)
        {
            if (config == null) return null;

            if (解析提供者(config) == "Ollama本地")
                return await 调用本地AI分析截图(config, base64Image, 提示词);
            else
                return await 调用远程AI分析截图(config, base64Image, 提示词);
        }

        private static async Task<string> 调用本地AI分析截图(AIConfigData config, string base64Image, string 提示词)
        {
            try
            {
                string host = string.IsNullOrEmpty(config.Ollama地址) ? "http://localhost:11434" : config.Ollama地址;
                string model = string.IsNullOrEmpty(config.Ollama模型) ? "minicpm-v:latest" : config.Ollama模型;

                string rawBase64 = base64Image;
                if (base64Image.StartsWith("data:image/png;base64,"))
                    rawBase64 = base64Image.Substring("data:image/png;base64,".Length);
                else if (base64Image.StartsWith("data:image/jpeg;base64,"))
                    rawBase64 = base64Image.Substring("data:image/jpeg;base64,".Length);

                var client = new OllamaApiClient(new Uri(host));
                var request = new OllamaSharp.Models.GenerateRequest
                {
                    Model = model,
                    Prompt = 提示词,
                    Stream = true,
                    Images = new string[] { rawBase64 },
                    Options = config.温度.HasValue ? new RequestOptions { Temperature = config.温度.Value } : null
                };

                var sb = new StringBuilder();
                await foreach (var chunk in client.GenerateAsync(request).ConfigureAwait(false))
                {
                    if (chunk == null) continue;
                    if (!string.IsNullOrEmpty(chunk.Response))
                        sb.Append(chunk.Response);
                }

                return sb.Length > 0 ? sb.ToString() : null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[本地截图分析] 异常: {ex.Message}");
                return null;
            }
        }

        private static async Task<string> 调用远程AI分析截图(AIConfigData config, string base64Image, string 提示词)
        {
            string apiUrl = config.远程API地址;
            string apiKey = 解密密钥(config.加密API密钥);
            string model = string.IsNullOrEmpty(config.远程模型) ? "qwen-vl-max" : config.远程模型;
            if (string.IsNullOrEmpty(apiUrl) || string.IsNullOrEmpty(apiKey)) return null;

            if (apiUrl.Contains("dashscope.aliyuncs.com"))
                return await 调用DashScope多模态截图(apiUrl, apiKey, model, base64Image, 提示词);

            if (!apiUrl.EndsWith("/chat/completions"))
            {
                if (apiUrl.EndsWith("/v1") || apiUrl.EndsWith("/compatible-mode/v1"))
                    apiUrl += "/chat/completions";
            }

            var messages = new List<object>
            {
                new Dictionary<string, object>
                {
                    ["role"] = "user",
                    ["content"] = new List<object>
                    {
                        new { type = "text", text = 提示词 },
                        new { type = "image_url", image_url = new { url = base64Image } }
                    }
                }
            };

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new Dictionary<string, object>
            {
                ["model"] = model,
                ["messages"] = messages,
                ["stream"] = true
            };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(apiUrl, jsonContent).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var reader = new StreamReader(stream);
                var sb = new StringBuilder();
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (string.IsNullOrEmpty(line)) continue;
                    if (!line.StartsWith("data: ")) continue;
                    string data = line.Substring(6);
                    if (data == "[DONE]") break;
                    try
                    {
                        using var doc = JsonDocument.Parse(data);
                        var delta = doc.RootElement.GetProperty("choices")[0].GetProperty("delta");
                        if (delta.TryGetProperty("content", out var ct) && ct.GetString() != null)
                            sb.Append(ct.GetString());
                    }
                    catch { }
                }
                return sb.Length > 0 ? sb.ToString() : null;
            }

            var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            string 截断错误 = errorBody.Length > 500 ? errorBody.Substring(0, 500) : errorBody;
            throw new InvalidOperationException($"视觉AI API 返回 HTTP {(int)response.StatusCode}: {截断错误}");
        }

        private static async Task<string> 调用DashScope多模态截图(string apiUrl, string apiKey, string model, string base64Image, string 提示词)
        {
            string apiBase = apiUrl.TrimEnd('/');
            if (apiBase.EndsWith("/compatible-mode/v1"))
                apiBase = apiBase.Replace("/compatible-mode/v1", "");
            if (!apiBase.EndsWith("/api/v1"))
                apiBase = apiBase + "/api/v1";
            string 多模态Url = apiBase + "/services/aigc/multimodal-generation/generation";

            var requestBody = new Dictionary<string, object>
            {
                ["model"] = model,
                ["input"] = new Dictionary<string, object>
                {
                    ["messages"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["role"] = "user",
                            ["content"] = new List<object>
                            {
                                new Dictionary<string, string> { ["text"] = 提示词 },
                                new Dictionary<string, string> { ["image"] = base64Image }
                            }
                        }
                    }
                }
            };

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(多模态Url, jsonContent).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                var content = doc.RootElement.GetProperty("output").GetProperty("choices")[0]
                    .GetProperty("message").GetProperty("content");
                if (content.ValueKind == JsonValueKind.Array && content.GetArrayLength() > 0)
                {
                    foreach (var item in content.EnumerateArray())
                    {
                        if (item.TryGetProperty("text", out var text))
                            return text.GetString();
                    }
                }
                return content.GetString();
            }

            string 截断错误 = responseBody.Length > 500 ? responseBody.Substring(0, 500) : responseBody;
            throw new InvalidOperationException($"DashScope 多模态 API 返回 HTTP {(int)response.StatusCode}: {截断错误}");
        }

        public static async Task<string> 调用AI流式(AIConfigData config, List<AIChatMessage> 消息历史, string 自定义规则, Func<string, Task> 回调, Func<string, Task> 思考回调 = null, CancellationToken cancellationToken = default)
        {
            return await 调用AI流式带工具(config, 消息历史, 自定义规则, null, 回调, null, 思考回调, null, cancellationToken);
        }

        public static async Task<string> 调用AI流式带工具(AIConfigData config, List<AIChatMessage> 消息历史, string 自定义规则, List<MCPToolDefinition> 工具列表, Func<string, Task> 回调, Func<string, Task> 工具回调 = null, Func<string, Task> 思考回调 = null, Func<string, Task> 进度回调 = null, CancellationToken cancellationToken = default, 计划节点 当前计划树 = null)
        {
            if (config == null) return null;

            if (!向量记忆管理器.是否可用)
            {
                var 记忆任务 = AI使用经验管理器.初始化向量记忆();
                await Task.WhenAny(记忆任务, Task.Delay(30000)).ConfigureAwait(false);
            }

            if (是否训练中)
            {
                回调?.Invoke("模型训练中，请稍候再试");
                return "模型训练中，请稍候再试";
            }

            if (解析提供者(config) == "Ollama本地")
            {
                return await 调用本地AI流式带工具(config, 消息历史, 自定义规则, 工具列表, 回调, 工具回调, 思考回调, 进度回调, cancellationToken, 当前计划树);
            }
            else
            {
                return await 调用远程AI流式带工具(config, 消息历史, 自定义规则, 工具列表, 回调, 工具回调, 思考回调, 进度回调, cancellationToken, 当前计划树);
            }
        }

        private static async Task<string> 调用本地AI流式带工具(AIConfigData config, List<AIChatMessage> 消息历史, string 自定义规则, List<MCPToolDefinition> 工具列表, Func<string, Task> 回调, Func<string, Task> 工具回调, Func<string, Task> 思考回调 = null, Func<string, Task> 进度回调 = null, CancellationToken cancellationToken = default, 计划节点 当前计划树 = null)
        {
            var 完整回复 = new StringBuilder();
            try
            {
                string host = string.IsNullOrEmpty(config.Ollama地址) ? "http://localhost:11434" : config.Ollama地址;
                string model = string.IsNullOrEmpty(config.Ollama模型) ? "qwen2:0.5b" : config.Ollama模型;

                var 工具描述 = 工具列表 != null ? MCP工具管理器.构建Ollama工具描述(工具列表) : "";
                var 分类规则提示词 = 工具列表 != null ? 收集分类规则提示词(工具列表) : "";
                var 记忆上下文 = await 构建记忆上下文(消息历史, config, 当前对话Id上下文?.Value).ConfigureAwait(false);
                var 提示词 = 构建提示词(消息历史, 自定义规则) + 记忆上下文 + 工具描述 + 分类规则提示词;
                var client = new OllamaApiClient(new Uri(host));

                var request = new OllamaSharp.Models.GenerateRequest
                {
                    Model = model,
                    Prompt = 提示词,
                    Stream = true,
                    Options = config.温度.HasValue ? new RequestOptions { Temperature = config.温度.Value } : null
                };

                var 思考解析器 = new 流式思考解析器(思考回调);
                await foreach (var chunk in client.GenerateAsync(request).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (chunk == null) continue;
                    if (!string.IsNullOrEmpty(chunk.Response))
                    {
                        完整回复.Append(chunk.Response);
                        if (思考回调 != null)
                            await 思考解析器.处理增量(chunk.Response, 回调);
                        else
                            await 回调(chunk.Response);
                    }
                }

                if (思考回调 != null)
                    await 思考解析器.完成(回调);

                string 回复文本 = 完整回复.ToString();
                if (string.IsNullOrEmpty(回复文本)) return null;

                var 提取计划 = AI使用经验管理器.从回复中提取计划(回复文本);
                if (提取计划 != null)
                {
                    当前计划树 = 提取计划;
                    _ = AI使用经验管理器.存储计划到向量库(提取计划, config);
                }

                var 树修改列表 = new List<计划节点>();
                var 对话Id上下文 = 当前对话Id上下文?.Value;
                var 工具结果 = 工具列表 != null && 工具列表.Count > 0
                    ? MCP工具管理器.解析ToolCall响应(回复文本, 工具列表, async msg => { if (进度回调 != null) await 进度回调(msg); }, 对话Id上下文, 0, 当前计划树, 树修改列表)
                    : new List<工具执行结果>();

                if (当前计划树 != null && 树修改列表.Count > 0)
                {
                    foreach (var 修改 in 树修改列表)
                        当前计划树.子步骤.Add(修改);
                }

                if (工具结果.Count == 0)
                    return 完整回复.Length > 0 ? 完整回复.ToString() : null;

                foreach (var item in 工具结果)
                {
                    var 记录ForEval = new 工具调用记录
                    {
                        工具ID = item.工具ID,
                        调用时间 = DateTime.Now,
                        输入参数 = new Dictionary<string, object>(),
                        是否成功 = !item.结果.Contains("执行失败"),
                        输出摘要 = item.结果,
                        对话ID = 对话Id上下文 ?? "",
                        对话轮次 = 0,
                        调用时计划树 = 当前计划树
                    };
                    Task.Run(async () =>
                    {
                        try
                        {
                            var 经验配置 = 获取经验AI配置();
                            if (经验配置 != null && 获取启用自主学习())
                            {
                                var (quality, match, reason, tags) = await AI使用经验管理器.评估工具调用(经验配置, 记录ForEval).ConfigureAwait(false);
                                AI使用经验管理器.更新印象(记录ForEval.工具ID, match, reason, 记录ForEval);
                                if (quality == "high")
                                {
                                    var 印象 = AI使用经验管理器.获取工具印象(记录ForEval.工具ID);
                                    int 印象版本 = 印象.修正历史.Count;
                                    AI使用经验管理器.追加训练样本(记录ForEval, quality, match, 印象.当前印象, 印象版本);
                                }
                            }
                            await AI使用经验管理器.存储工具经验到向量库(记录ForEval, config).ConfigureAwait(false);
                        }
                        catch { }
                    });

                    string 工具消息 = $"🔧 {item.结果}";
                    if (工具回调 != null)
                        await 工具回调(工具消息);
                    else
                        await 回调($"\n\n[工具执行结果] {item.结果}\n\n");
                }

                var 新历史 = new List<AIChatMessage>(消息历史)
                {
                    new AIChatMessage { 角色 = "AI", 内容 = 回复文本 },
                    new AIChatMessage { 角色 = "系统", 内容 = "[工具执行结果]\n" + string.Join("\n", 工具结果.Select(r => r.结果)) + "\n\n请基于以上工具执行结果继续回答用户的问题。", 是否私有 = true }
                };

                try
                {
                    string 后续回复 = await 调用本地AI流式带工具(config, 新历史, 自定义规则, 工具列表, 回调, 工具回调, 思考回调, 进度回调, cancellationToken, 当前计划树);
                    if (!string.IsNullOrEmpty(后续回复))
                        完整回复.Append("\n\n").Append(后续回复);
                }
                catch (Exception ex)
                {
                    if (工具回调 != null)
                        await 工具回调($"⚠️ 工具后续调用失败: {ex.Message}");
                }

                return 完整回复.Length > 0 ? 完整回复.ToString() : "工具已执行，但后续 AI 回复失败。";
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return 完整回复.Length > 0 ? 完整回复.ToString() : null;
            }
        }
        private static async Task<string> 调用远程AI流式带工具(AIConfigData config, List<AIChatMessage> 消息历史, string 自定义规则, List<MCPToolDefinition> 工具列表, Func<string, Task> 回调, Func<string, Task> 工具回调, Func<string, Task> 思考回调 = null, Func<string, Task> 进度回调 = null, CancellationToken cancellationToken = default, 计划节点 当前计划树 = null)
        {
            const int MaxToolCallRounds = 5;
            int round = 0;

            var messages = 构建消息列表(消息历史, 自定义规则);
            var 分类规则提示词 = 工具列表 != null ? 收集分类规则提示词(工具列表) : "";
            if (!string.IsNullOrEmpty(分类规则提示词))
            {
                messages.Insert(1, new { role = "system", content = 分类规则提示词 });
            }
            var 记忆上下文 = await 构建记忆上下文(消息历史, config, 当前对话Id上下文?.Value).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(记忆上下文))
            {
                messages.Insert(1, new { role = "system", content = 记忆上下文 });
            }
            var apiKey = 解密密钥(config.加密API密钥);
            string model = string.IsNullOrEmpty(config.远程模型) ? "gpt-4o-mini" : config.远程模型;
            if (string.IsNullOrEmpty(config.远程API地址) || string.IsNullOrEmpty(apiKey)) return null;

            var 累计回复 = new StringBuilder();
            bool 已执行工具 = false;

            while (round < MaxToolCallRounds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                round++;
                var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var 请求体 = new Dictionary<string, object>
                {
                    ["model"] = model,
                    ["messages"] = messages,
                    ["max_tokens"] = config.最大输出Token > 0 ? config.最大输出Token : 8192,
                    ["stream"] = true
                };
                if (工具列表 != null && 工具列表.Count > 0)
                    请求体["tools"] = MCP工具管理器.构建工具定义列表(工具列表);
                if (config.温度.HasValue)
                    请求体["temperature"] = config.温度.Value;

                var jsonContent = new StringContent(JsonSerializer.Serialize(请求体), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(config.远程API地址, jsonContent).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    var 错误内容 = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    int statusCode = (int)response.StatusCode;
                    string 错误描述 = statusCode switch
                    {
                        401 => "API Key 无效或未配置",
                        402 => "账户余额不足",
                        429 => "请求频率过高，请稍后重试",
                        400 or 422 => "请求参数错误",
                        _ => $"HTTP {statusCode}"
                    };
                    System.Diagnostics.Debug.WriteLine($"[AI流式] {错误描述}: {错误内容}");
                    通知工具.错误弹窗($"AI 调用失败: {错误描述}");
                    return 累计回复.Length > 0 ? 累计回复.ToString() : null;
                }

                var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var reader = new StreamReader(stream);
                var 本轮回复 = new StringBuilder();
                var 本轮思考 = new StringBuilder();
                var toolCalls = new Dictionary<int, (string id, string name, StringBuilder args)>();
                string finishReason = null;

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (!line.StartsWith("data: ")) continue;

                    string data = line.Substring(6);
                    if (data == "[DONE]") break;

                    try
                    {
                        using var doc = JsonDocument.Parse(data);
                        var choices = doc.RootElement.GetProperty("choices");
                        if (choices.GetArrayLength() > 0)
                        {
                            var choice = choices[0];
                            if (choice.TryGetProperty("finish_reason", out var fr) && fr.GetString() != null)
                            {
                                finishReason = fr.GetString();
                            }

                            var delta = choice.GetProperty("delta");
                            if (delta.TryGetProperty("reasoning_content", out var rc) && rc.GetString() != null)
                            {
                                string 思考文本 = rc.GetString();
                                本轮思考.Append(思考文本);
                                if (思考回调 != null)
                                    await 思考回调(思考文本);
                            }

                            if (delta.TryGetProperty("content", out var content) && content.GetString() != null)
                            {
                                string text = content.GetString();
                                本轮回复.Append(text);
                                累计回复.Append(text);
                                await 回调(text);
                            }

                            if (delta.TryGetProperty("tool_calls", out var tcArray))
                            {
                                foreach (var tc in tcArray.EnumerateArray())
                                {
                                    int idx = tc.GetProperty("index").GetInt32();
                                    if (!toolCalls.ContainsKey(idx))
                                    {
                                        string tid = tc.TryGetProperty("id", out var idEl) ? idEl.GetString() : "";
                                        string tname = tc.GetProperty("function").GetProperty("name").GetString();
                                        toolCalls[idx] = (tid, tname, new StringBuilder());
                                    }

                                    var func = tc.GetProperty("function");
                                    if (func.TryGetProperty("arguments", out var argsEl) && argsEl.GetString() != null)
                                    {
                                        toolCalls[idx].args.Append(argsEl.GetString());
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                if (toolCalls.Count > 0)
                {
                    var 提取计划 = AI使用经验管理器.从回复中提取计划(累计回复.ToString());
                    if (提取计划 != null)
                    {
                        当前计划树 = 提取计划;
                        _ = AI使用经验管理器.存储计划到向量库(提取计划, config);
                    }

                    System.Diagnostics.Debug.WriteLine($"[MSG] finishReason={finishReason}, 执行{工具列表.Count}工具, 列表: {string.Join(",", 工具列表.Select(t => t.工具ID))}");
                    已执行工具 = true;
                    var tcMessages = new List<object>();

                    var 助手消息 = new Dictionary<string, object>
                    {
                        ["role"] = "assistant",
                        ["tool_calls"] = tcMessages
                    };
                    if (本轮思考.Length > 0)
                        助手消息["reasoning_content"] = 本轮思考.ToString();
                    if (本轮回复.Length > 0)
                        助手消息["content"] = 本轮回复.ToString();
                    messages.Add(助手消息);

                    var 树修改列表 = new List<计划节点>();
                    var 对话Id上下文 = 当前对话Id上下文?.Value;
                    foreach (var kv in toolCalls.OrderBy(k => k.Key))
                    {
                        var (tid, tname, args) = kv.Value;
                        Dictionary<string, object> 工具参数 = null;
                        try
                        {
                            string argsStr = args.ToString();
                            if (!string.IsNullOrEmpty(argsStr))
                                工具参数 = JsonSerializer.Deserialize<Dictionary<string, object>>(argsStr);
                        }
                        catch { }
                        string toolResult = MCP工具管理器.执行工具(tname, 工具列表, 工具参数, async msg => { if (进度回调 != null) await 进度回调(msg); }, 对话Id上下文, round, 当前计划树, 树修改列表);

                        var 记录ForEval = new 工具调用记录
                        {
                            工具ID = tname,
                            调用时间 = DateTime.Now,
                            输入参数 = 工具参数 ?? new Dictionary<string, object>(),
                            是否成功 = !toolResult.Contains("执行失败"),
                            输出摘要 = toolResult,
                            对话ID = 对话Id上下文 ?? "",
                            对话轮次 = round,
                            调用时计划树 = 当前计划树
                        };
                        Task.Run(async () =>
                        {
                            try
                            {
                                var 经验配置 = 获取经验AI配置();
                                if (经验配置 != null && 获取启用自主学习())
                                {
                                    var (quality, match, reason, tags) = await AI使用经验管理器.评估工具调用(经验配置, 记录ForEval).ConfigureAwait(false);
                                    AI使用经验管理器.更新印象(记录ForEval.工具ID, match, reason, 记录ForEval);
                                    if (quality == "high")
                                    {
                                        var 印象 = AI使用经验管理器.获取工具印象(记录ForEval.工具ID);
                                        int 印象版本 = 印象.修正历史.Count;
                                        AI使用经验管理器.追加训练样本(记录ForEval, quality, match, 印象.当前印象, 印象版本);
                                    }
                                }
                                await AI使用经验管理器.存储工具经验到向量库(记录ForEval, config).ConfigureAwait(false);
                            }
                            catch { }
                        });

                        tcMessages.Add(new Dictionary<string, object>
                        {
                            ["id"] = tid,
                            ["type"] = "function",
                            ["function"] = new Dictionary<string, object>
                            {
                                ["name"] = tname,
                                ["arguments"] = args.ToString()
                            }
                        });

                        messages.Add(new Dictionary<string, object>
                        {
                            ["role"] = "tool",
                            ["tool_call_id"] = tid,
                            ["content"] = toolResult
                        });

                        if (工具回调 != null)
                            await 工具回调(toolResult);
                    }

                    if (树修改列表.Count > 0)
                    {
                        foreach (var 修改 in 树修改列表)
                            当前计划树.子步骤.Add(修改);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[MSG] 退出: finishReason={finishReason}, toolCalls.Count={toolCalls.Count}, 累计回复长度={累计回复.Length}");
                    return 累计回复.Length > 0 ? 累计回复.ToString() : null;
                }
            }

            return 累计回复.Length > 0 ? 累计回复.ToString() : "已达到最大工具调用轮次。";
        }

        private static string 收集分类规则提示词(List<MCPToolDefinition> 工具列表)
        {
            if (工具列表 == null || 工具列表.Count == 0) return "";

            var 分类节点表 = new Dictionary<string, 分类规则节点>();
            foreach (var tool in 工具列表)
            {
                if (分类节点表.ContainsKey(tool.分类路径)) continue;
                分类节点表[tool.分类路径] = new 分类规则节点
                {
                    分类路径 = tool.分类路径,
                    分类名 = tool.分类路径.Contains('/') ? tool.分类路径.Split('/').Last() : tool.分类路径,
                    分类规则 = tool.分类AI规则 ?? "",
                    祖先规则列表 = tool.祖先AI规则列表 ?? new List<string>(),
                    祖先路径列表 = tool.祖先分类路径列表 ?? new List<string>()
                };
            }

            var 根节点列表 = new List<分类规则节点>();
            foreach (var node in 分类节点表.Values)
            {
                string 父路径 = node.分类路径.Contains('/')
                    ? node.分类路径.Substring(0, node.分类路径.LastIndexOf('/'))
                    : null;
                if (父路径 != null && 分类节点表.TryGetValue(父路径, out var 父节点))
                    父节点.子节点.Add(node);
                else
                    根节点列表.Add(node);
            }

            if (根节点列表.Count == 0) return "";

            var sb = new StringBuilder();
            sb.AppendLine("【工具分类层级与使用规则】");
            sb.AppendLine("每个分类代表一组功能相关的工具，子分类是对父分类功能的进一步细化与特化。");
            sb.AppendLine("本次对话已启用的分类及其规则如下：");
            sb.AppendLine();

            foreach (var 根 in 根节点列表)
                渲染分类树(根, sb, 0, new HashSet<string>());

            return sb.ToString().TrimEnd();
        }

        private class 分类规则节点
        {
            public string 分类路径 = "";
            public string 分类名 = "";
            public string 分类规则 = "";
            public List<string> 祖先规则列表 = new List<string>();
            public List<string> 祖先路径列表 = new List<string>();
            public List<分类规则节点> 子节点 = new List<分类规则节点>();
        }

        private static void 渲染分类树(分类规则节点 node, StringBuilder sb, int 深度, HashSet<string> 祖先已输出规则)
        {
            string 缩进 = new string(' ', 深度 * 2);
            bool 有规则 = !string.IsNullOrEmpty(node.分类规则);
            bool 有继承规则 = node.祖先规则列表.Count > 0;

            sb.Append(缩进);
            sb.Append("▪ ");
            sb.Append(node.分类名);

            if (有规则)
            {
                sb.Append(" — 规则：");
                sb.Append(node.分类规则);
                祖先已输出规则.Add(node.分类规则);
            }

            sb.AppendLine();

            if (有继承规则)
            {
                for (int i = 0; i < node.祖先规则列表.Count; i++)
                {
                    string 祖先规则 = node.祖先规则列表[i];
                    string 祖先名 = i < node.祖先路径列表.Count
                        ? (node.祖先路径列表[i].Contains('/') ? node.祖先路径列表[i].Split('/').Last() : node.祖先路径列表[i])
                        : "父级";
                    if (祖先已输出规则.Contains(祖先规则)) continue;
                    sb.AppendLine($"{缩进}  ↳ 继承自「{祖先名}」：{祖先规则}");
                }
            }

            foreach (var 子 in node.子节点)
                渲染分类树(子, sb, 深度 + 1, new HashSet<string>(祖先已输出规则));
        }

        private static async Task<string> 构建记忆上下文(List<AIChatMessage> 消息历史, AIConfigData config, string 对话Id = null)
        {
            try
            {
                if (!向量记忆管理器.是否可用) return "";
                var 用户消息 = 消息历史.Where(m => m.角色 == "用户").Select(m => m.内容).ToList();
                if (用户消息.Count == 0) return "";
                var 查询 = 用户消息.Last();
                if (string.IsNullOrEmpty(查询) || 查询.Length > 500) return "";
                return await AI使用经验管理器.检索相关经验(查询, config, 对话Id).ConfigureAwait(false);
            }
            catch { return ""; }
        }

        private static List<object> 构建消息列表(List<AIChatMessage> 消息历史, string 自定义规则)
        {
            var messages = new List<object>();

            messages.Add(new { role = "system", content = "【系统通知解读】脚本输出标记: [通知]普通信息→简要告知; [提示]瞬时提示→可选告知; [错误-需关注]→明确指出错误; [警告-请注意]→提醒用户; [交互-确认]已自动选择默认值; [交互-输入]已自动使用默认值; [交互-选择]已自动选第一项; [图片组]有图片生成但AI无法查看; [屏幕截图分析]以下是视觉AI分析结果" });

            if (AI配置管理器.获取启用增量记录())
            {
                messages.Add(new { role = "system", content = "【计划要求】首次工具调用前提交计划——格式: [计划]\n需求=目标描述\n步骤=描述|工具ID(叶子步骤)\n步骤=描述|*|子步骤1|工具ID|子步骤2|工具ID(分解步骤)\n[/计划]" });
            }

            if (!string.IsNullOrEmpty(自定义规则))
            {
                messages.Add(new { role = "system", content = 自定义规则 });
            }

            foreach (var msg in 消息历史)
            {
                string role = msg.角色 switch
                {
                    "AI" => "assistant",
                    "系统" => "system",
                    _ => "user"
                };
                string 内容 = msg.内容;
                if (role == "user")
                    内容 = $"{msg.时间戳:yyyy/M/d HH:mm:ss} {默认用户名}: {内容}";

                if (role == "assistant" && !string.IsNullOrEmpty(msg.思考内容))
                {
                    messages.Add(new Dictionary<string, object>
                    {
                        ["role"] = role,
                        ["content"] = 内容,
                        ["reasoning_content"] = msg.思考内容
                    });
                }
                else if (role == "user" && msg.图片列表 != null && msg.图片列表.Count > 0)
                {
                    var contentParts = new List<object> { new { type = "text", text = 内容 } };
                    foreach (var img in msg.图片列表)
                    {
                        if (!string.IsNullOrEmpty(img))
                            contentParts.Add(new { type = "image_url", image_url = new { url = img } });
                    }
                    messages.Add(new { role, content = (object)contentParts });
                }
                else
                {
                    messages.Add(new { role, content = 内容 });
                }
            }

            return messages;
        }

        private static string 构建提示词(List<AIChatMessage> 消息历史, string 自定义规则)
        {
            var sb = new StringBuilder();

            sb.AppendLine("【系统通知解读】脚本输出标记: [通知]→普通信息; [提示]→可选告知; [错误-需关注]→明确指出; [警告-请注意]→提醒用户; [交互-确认/输入/选择]→已自动选择默认值; [图片组]→有图片生成; [屏幕截图分析]→以下是视觉AI分析结果");
            sb.AppendLine();

            if (AI配置管理器.获取启用增量记录())
            {
                sb.AppendLine("【计划要求】首次工具调用前提交计划——格式: [计划]\n需求=目标描述\n步骤=描述|工具ID(叶子步骤)\n步骤=描述|*|子步骤1|工具ID|子步骤2|工具ID(分解步骤)\n[/计划]");
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(自定义规则))
            {
                sb.AppendLine("你必须遵守以下规则：");
                sb.AppendLine(自定义规则);
                sb.AppendLine();
            }

            sb.AppendLine("【通用MCP工具指南】工具说明见上方工具列表。关键工作流: list_all_scripts检查已有脚本→get_node_creation_rules了解规则→create_working_script初始化→逐节点add_node构建→execute_node测试→modify_node修复→save_script保存。循环通过成功后跳转实现,备注标注循环开始/结束。保存前自查: 正确性>稳定性>可读性>效率>名字明确度>备注明确度。命名用动宾结构(如'点击签到按钮')。禁止高危操作。执行进度可控,添加__隐藏进度参数可静默执行。");
            sb.AppendLine();

            foreach (var msg in 消息历史)
            {
                switch (msg.角色)
                {
                    case "用户":
                        sb.AppendLine($"{msg.时间戳:yyyy/M/d HH:mm:ss} {默认用户名}: {msg.内容}");
                        break;
                    case "AI":
                        sb.AppendLine($"助手: {msg.内容}");
                        break;
                    case "系统":
                        sb.AppendLine(msg.内容);
                        break;
                }
            }

            return sb.ToString();
        }

        public static void 执行脚本工具(string 脚本路径)
        {
            try
            {
                if (File.Exists(脚本路径))
                {
                    string 内容 = File.ReadAllText(脚本路径);
                    var 脚本 = new 键鼠库.动作.自动任务脚本(IntPtr.Zero, 内容);
                    脚本.执行();
                }
            }
            catch { }
        }

        public static bool 配置是否有效(AIConfigData config)
        {
            if (config == null) return false;
            if (解析提供者(config) == "Ollama本地")
            {
                return !string.IsNullOrEmpty(config.Ollama地址);
            }
            else
            {
                return !string.IsNullOrEmpty(config.远程API地址) && !string.IsNullOrEmpty(config.加密API密钥);
            }
        }

        public static List<AINamedConfig> 获取配置列表()
        {
            return _数据.AI配置列表 ?? new List<AINamedConfig>();
        }

        public static void 添加配置(AINamedConfig config)
        {
            lock (_锁)
            {
                if (_数据.AI配置列表 == null)
                    _数据.AI配置列表 = new List<AINamedConfig>();
                _数据.AI配置列表.Add(config);
            }
            保存数据();
        }

        public static void 删除配置(string id)
        {
            lock (_锁)
            {
                if (_数据.AI配置列表 == null) return;
                var item = _数据.AI配置列表.FirstOrDefault(c => c.Id == id);
                if (item == null) return;
                _数据.AI配置列表.Remove(item);

                if (_数据.AI配置列表.Count > 0)
                {
                    var 第一Id = _数据.AI配置列表[0].Id;

                    if (_数据.视觉AI配置Id == id)
                        _数据.视觉AI配置Id = 第一Id;
                    if (_数据.经验AI配置Id == id)
                        _数据.经验AI配置Id = 第一Id;
                    if (_数据.上次文本配置Id == id)
                        _数据.上次文本配置Id = 第一Id;
                    if (_数据.上次多模态配置Id == id)
                        _数据.上次多模态配置Id = 第一Id;

                    foreach (var 对话 in _数据.对话列表)
                    {
                        if (对话.文本AI配置Id == id)
                            对话.文本AI配置Id = 第一Id;
                        if (对话.多模态AI配置Id == id)
                            对话.多模态AI配置Id = 第一Id;
                    }
                }
            }
            保存数据();
        }

        public static void 更新配置(AINamedConfig config)
        {
            lock (_锁)
            {
                if (_数据.AI配置列表 == null) return;
                int index = _数据.AI配置列表.FindIndex(c => c.Id == config.Id);
                if (index >= 0)
                    _数据.AI配置列表[index] = config;
            }
            保存数据();
        }

        public static AIConfigData 根据Id获取配置(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var 列表 = _数据.AI配置列表;
            if (列表 == null || 列表.Count == 0) return null;
            var item = 列表.FirstOrDefault(c => c.Id == id);
            if (item != null)
                return item.配置 ?? new AIConfigData();
            return 列表[0].配置 ?? new AIConfigData();
        }

        public static void 更新上次使用的配置Id(string 文本Id, string 多模态Id)
        {
            lock (_锁)
            {
                if (!string.IsNullOrEmpty(文本Id))
                    _数据.上次文本配置Id = 文本Id;
                if (!string.IsNullOrEmpty(多模态Id))
                    _数据.上次多模态配置Id = 多模态Id;
            }
            保存数据();
        }

        public static string 获取上次文本配置Id()
        {
            return _数据.上次文本配置Id;
        }

        public static string 获取上次多模态配置Id()
        {
            return _数据.上次多模态配置Id;
        }

        private class 流式思考解析器
        {
            private enum 解析状态 { 正常, 思考中 }
            private 解析状态 _状态 = 解析状态.正常;
            private readonly StringBuilder _缓冲区 = new StringBuilder();
            private readonly Func<string, Task> _思考回调;

            public 流式思考解析器(Func<string, Task> 思考回调)
            {
                _思考回调 = 思考回调;
            }

            public async Task 处理增量(string 增量, Func<string, Task> 正常回调)
            {
                _缓冲区.Append(增量);
                var 文本 = _缓冲区.ToString();

                if (_状态 == 解析状态.正常)
                {
                    var 思考开始索引 = 查找思考开始标签(文本);
                    if (思考开始索引 >= 0)
                    {
                        string 正常部分 = 文本.Substring(0, 思考开始索引);
                        int 标签结束 = 文本.IndexOf('>', 思考开始索引) + 1;
                        if (标签结束 > 0)
                        {
                            if (!string.IsNullOrEmpty(正常部分))
                                await 正常回调(正常部分);
                            _状态 = 解析状态.思考中;
                            _缓冲区.Clear();
                            int 思考内容开始 = 标签结束;
                            if (思考内容开始 < 文本.Length)
                                _缓冲区.Append(文本.Substring(思考内容开始));
                        }
                    }
                    else if (文本.Length > 8)
                    {
                        int 安全长度 = 文本.Length - 5;
                        if (安全长度 > 0)
                        {
                            string 安全部分 = 文本.Substring(0, 安全长度);
                            await 正常回调(安全部分);
                            _缓冲区.Clear();
                            _缓冲区.Append(文本.Substring(安全长度));
                        }
                    }
                }
                else
                {
                    var 思考结束索引 = 查找思考结束标签(文本);
                    if (思考结束索引 >= 0)
                    {
                        string 思考部分 = 文本.Substring(0, 思考结束索引);
                        if (!string.IsNullOrEmpty(思考部分))
                            await _思考回调(思考部分);
                        _状态 = 解析状态.正常;
                        _缓冲区.Clear();
                        string 剩余 = 文本.Substring(思考结束索引 + 搜索结束标签(文本.Substring(思考结束索引)));
                        if (!string.IsNullOrEmpty(剩余))
                        {
                            _缓冲区.Append(剩余);
                            _状态 = 解析状态.正常;
                            await 处理增量("", 正常回调);
                        }
                    }
                    else if (文本.Length > 15)
                    {
                        int 安全长度 = 文本.Length - 8;
                        string 安全部分 = 文本.Substring(0, 安全长度);
                        await _思考回调(安全部分);
                        _缓冲区.Clear();
                        _缓冲区.Append(文本.Substring(安全长度));
                    }
                }
            }

            public async Task 完成(Func<string, Task> 正常回调)
            {
                var 文本 = _缓冲区.ToString();
                if (string.IsNullOrEmpty(文本)) return;

                if (_状态 == 解析状态.正常)
                {
                    await 正常回调(文本);
                }
                else
                {
                    var 结束索引 = 查找思考结束标签(文本);
                    if (结束索引 >= 0)
                    {
                        string 思考部分 = 文本.Substring(0, 结束索引);
                        if (!string.IsNullOrEmpty(思考部分))
                            await _思考回调(思考部分);
                        string 剩余 = 文本.Substring(结束索引 + 搜索结束标签(文本.Substring(结束索引)));
                        if (!string.IsNullOrEmpty(剩余))
                            await 正常回调(剩余);
                    }
                    else
                    {
                        await _思考回调(文本);
                    }
                }
            }

            private static int 查找思考开始标签(string 文本)
            {
                int i = 文本.IndexOf("<thinking>", StringComparison.OrdinalIgnoreCase);
                if (i >= 0) return i;
                i = 文本.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
                if (i >= 0) return i;
                i = 文本.IndexOf("<｜end▁of▁thinking｜>", StringComparison.OrdinalIgnoreCase);
                if (i >= 0) return i;
                return -1;
            }

            private static int 查找思考结束标签(string 文本)
            {
                int i = 文本.IndexOf("</thinking>", StringComparison.OrdinalIgnoreCase);
                if (i >= 0) return i;
                i = 文本.IndexOf("</think>", StringComparison.OrdinalIgnoreCase);
                if (i >= 0) return i;
                i = 文本.IndexOf("<｜end▁of▁thinking｜>", StringComparison.OrdinalIgnoreCase);
                if (i >= 0) return i;
                return -1;
            }

            private static int 搜索结束标签(string 文本)
            {
                int i = 文本.IndexOf("</thinking>", StringComparison.OrdinalIgnoreCase);
                if (i >= 0) return i + "</thinking>".Length;
                i = 文本.IndexOf("</think>", StringComparison.OrdinalIgnoreCase);
                if (i >= 0) return i + "</think>".Length;
                i = 文本.IndexOf(" response", StringComparison.OrdinalIgnoreCase);
                if (i >= 0) return i + "<｜end▁of▁thinking｜>".Length;
                return 0;
            }
        }
    }
}
