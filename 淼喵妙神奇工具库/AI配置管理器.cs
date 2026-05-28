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
using System.Text.RegularExpressions;
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

        public static async Task<string> 调用AI(AIConfigData config, List<AIChatMessage> 消息历史, string 自定义规则)
        {
            if (config == null) return null;

            if (config.提供者类型 == "Ollama本地")
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
                    Stream = false,
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
                    ["max_tokens"] = config.最大输出Token > 0 ? config.最大输出Token : 8192
                };
                if (config.温度.HasValue)
                    请求体["temperature"] = config.温度.Value;

                var jsonContent = new StringContent(JsonSerializer.Serialize(请求体), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(apiUrl, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseBody);
                    return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
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

            if (config.提供者类型 == "Ollama本地")
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
                    Stream = false,
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

            var requestBody = new { model, messages };
            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(apiUrl, jsonContent).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(responseBody);
                return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            }

            string 截断错误 = responseBody.Length > 500 ? responseBody.Substring(0, 500) : responseBody;
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

            if (config.提供者类型 == "Ollama本地")
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
                var 提示词 = 构建提示词(消息历史, 自定义规则) + 工具描述 + 分类规则提示词;
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
                    当前计划树 = 提取计划;

                var 评审配置 = 获取评审AI配置();
                if (当前计划树 != null && 评审配置 != null && !string.IsNullOrEmpty(评审配置.远程API地址))
                {
                    var 工具ID列表 = 工具列表?.Select(t => t.工具ID).ToList() ?? new List<string>();
                    var (_, 建议) = await AI使用经验管理器.评审计划(当前计划树, 工具ID列表).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(建议))
                    {
                        消息历史.Add(new AIChatMessage { 角色 = "系统", 内容 = $"[计划评审建议]\n{建议}\n\n请参考以上建议调整你的执行计划。" });
                        var 评审重试历史 = new List<AIChatMessage>(消息历史);
                        try
                        {
                            string 后续回复 = await 调用本地AI流式带工具(config, 评审重试历史, 自定义规则, 工具列表, 回调, 工具回调, 思考回调, 进度回调, cancellationToken, 当前计划树);
                            if (!string.IsNullOrEmpty(后续回复))
                                完整回复.Append("\n\n").Append(后续回复);
                        }
                        catch { }
                        return 完整回复.Length > 0 ? 完整回复.ToString() : "计划已评审，请重新提交调整后的计划。";
                    }
                }

                var 树修改列表 = new List<计划节点>();
                var 对话Id上下文 = 当前对话Id上下文?.Value;
                var 工具结果 = 工具列表 != null && 工具列表.Count > 0
                    ? MCP工具管理器.解析ToolCall响应(回复文本, 工具列表, async msg => { if (进度回调 != null) await 进度回调(msg); }, 对话Id上下文, 0, 当前计划树, 树修改列表)
                    : new List<string>();

                if (工具列表 != null && 工具列表.Count > 0 && 工具结果.Count > 0 && 当前计划树 == null && AI配置管理器.获取启用增量记录())
                {
                    if (工具回调 != null)
                        await 工具回调("⚠️ 未检测到计划。请使用 [计划]...[/计划] 格式提交执行计划后再开始工具调用。");
                    return 完整回复.Length > 0 ? 完整回复.ToString() : "未检测到计划。请使用 [计划]...[/计划] 格式提交执行计划后再开始工具调用。";
                }

                if (当前计划树 != null && 树修改列表.Count > 0)
                {
                    foreach (var 修改 in 树修改列表)
                        当前计划树.子步骤.Add(修改);
                }

                if (工具结果.Count == 0)
                    return 完整回复.Length > 0 ? 完整回复.ToString() : null;

                foreach (var result in 工具结果)
                {
                    string 工具消息 = $"🔧 {result}";
                    if (工具回调 != null)
                        await 工具回调(工具消息);
                    else
                        await 回调($"\n\n[工具执行结果] {result}\n\n");
                }

                var 新历史 = new List<AIChatMessage>(消息历史)
                {
                    new AIChatMessage { 角色 = "AI", 内容 = 回复文本 },
                    new AIChatMessage { 角色 = "系统", 内容 = "[工具执行结果]\n" + string.Join("\n", 工具结果) + "\n\n请基于以上工具执行结果继续回答用户的问题。", 是否私有 = true }
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
                int 插入位置 = string.IsNullOrEmpty(自定义规则) ? 2 : 3;
                messages.Insert(插入位置, new { role = "system", content = 分类规则提示词 });
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
                        当前计划树 = 提取计划;

                    var 评审配置2 = 获取评审AI配置();
                    if (当前计划树 != null && round == 1 && 评审配置2 != null && !string.IsNullOrEmpty(评审配置2.远程API地址))
                    {
                        var 工具ID列表 = 工具列表?.Select(t => t.工具ID).ToList() ?? new List<string>();
                        var (_, 建议) = await AI使用经验管理器.评审计划(当前计划树, 工具ID列表).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(建议))
                        {
                            messages.Add(new { role = "system", content = $"[计划评审建议]\n{建议}\n\n请参考以上建议调整你的执行计划。" });
                            continue;
                        }
                    }

                    if (当前计划树 == null && AI配置管理器.获取启用增量记录())
                    {
                        if (工具回调 != null)
                            await 工具回调("⚠️ 未检测到计划。请使用 [计划]...[/计划] 格式提交执行计划后再开始工具调用。");
                        return 累计回复.Length > 0 ? 累计回复.ToString() : "未检测到计划。请使用 [计划]...[/计划] 格式提交执行计划后再开始工具调用。";
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
                        if (AI配置管理器.获取启用增量记录() && !工具在计划内(tname, 当前计划树))
                        {
                            string 错误信息 = $"工具 {tname} 不在当前执行计划中，请按计划顺序执行工具。";
                            messages.Add(new Dictionary<string, object>
                            {
                                ["role"] = "tool",
                                ["tool_call_id"] = tid,
                                ["content"] = 错误信息
                            });
                            if (工具回调 != null)
                                await 工具回调(错误信息);
                            continue;
                        }
                        Dictionary<string, object> 工具参数 = null;
                        try
                        {
                            string argsStr = args.ToString();
                            if (!string.IsNullOrEmpty(argsStr))
                                工具参数 = JsonSerializer.Deserialize<Dictionary<string, object>>(argsStr);
                        }
                        catch { }
                        string toolResult = MCP工具管理器.执行工具(tname, 工具列表, 工具参数, async msg => { if (进度回调 != null) await 进度回调(msg); }, 对话Id上下文, round, 当前计划树, 树修改列表);

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

        private static List<object> 构建消息列表(List<AIChatMessage> 消息历史, string 自定义规则)
        {
            var messages = new List<object>();

            var 经验系统消息 = 构建经验系统消息从历史(消息历史);
            if (!string.IsNullOrEmpty(经验系统消息))
            {
                messages.Add(new { role = "system", content = 经验系统消息 });
            }

            if (AI配置管理器.获取启用增量记录())
            {
                messages.Add(new { role = "system", content = "【执行计划提交要求】\n请在首次调用工具前，用以下格式提交你的执行计划：\n[计划]\n需求=你要完成的目标\n步骤=步骤描述|工具ID\n步骤=复杂步骤|*|子步骤1|工具ID|子步骤2|工具ID\n[/计划]\n- 需求= 后写本次要完成的目标描述\n- 步骤=描述|工具ID 表示一个叶子步骤，直接调用对应工具\n- 步骤=描述|*|子步骤1|工具ID|... 表示需分解的内部步骤，*后跟子步骤与工具的配对\n- 计划只需在首次工具调用前提交一次，后续工具调用按计划顺序执行即可" });
            }

            if (!string.IsNullOrEmpty(自定义规则))
            {
                messages.Add(new { role = "system", content = 自定义规则 });
            }

            messages.Add(new { role = "system", content = "【系统通知解读指南】\n当脚本执行输出中包含以下标记时，请按对应方式处理：\n- [通知] — 脚本正常运行中产生的普通信息，简要告知用户即可\n- [提示] — 脚本执行过程中的瞬时提示，可选告知用户\n- [错误-需关注] — 脚本遇到了错误，请务必向用户明确指出错误内容，提醒检查脚本配置或运行环境\n- [警告-请注意] — 脚本有潜在问题或需要注意的情况，请提醒用户留意\n- [交互-确认] — 脚本在执行中需要确认，已自动选择默认值继续。如结果显示不理想，可询问用户是否手动调整\n- [交互-输入] — 脚本在执行中需要输入，已自动使用默认值。如结果不符合预期，可询问用户是否手动指定\n- [交互-选择] — 脚本在执行中面临选项，已自动选择第一个选项。如结果不理想，可询问用户是否手动选择\n- [图片组] — 脚本产生了图片结果，但当前AI无法查看完整图片内容，只能看到尺寸摘要。请告知用户有图片生成\n- [屏幕截图分析] — 系统通过多模态视觉AI对当前屏幕进行了分析，以下是分析结果。请结合此信息理解当前屏幕状态" });
            messages.Add(new { role = "system", content = "【通用MCP工具使用指南】\n除了脚本工具外，系统还提供了以下通用内置工具，可直接调用：\n- ask_vision_ai — 截取当前屏幕并调用视觉AI进行分析，将视觉AI的回复直接返回。你可以传入任意自定义分析Prompt，灵活控制视觉AI的分析内容。当你需要查看用户屏幕上有什么（窗口、按钮、文本内容等）时主动调用此工具。参数：提示词(必填,传给视觉AI的分析提示词,例如'屏幕上有哪些按钮?''登录按钮在什么位置?返回JSON坐标'等)\n- modify_script_remark — 修改当前MCP工具组中某个脚本的备注内容。仅能修改已加载为MCP工具的脚本，不能修改未加载的脚本。当发现脚本备注缺失、描述不准确或无法区分时，在征得用户同意后使用。参数：脚本名或路径、新备注\n- write_log — 将内容写入应用logs目录下的日志文件。适合记录调试信息、脚本运行状态或中间结果。参数：日志名、内容\n- read_log — 读取应用logs目录下的日志文件。可配合write_log使用，写入后读取确认。超过8000字符的内容会自动截断，此时应提醒用户直接查看文件。参数：日志名\n- search_scripts — 在所有已保存的脚本中按关键词搜索，返回匹配脚本的名称、分类和备注摘要。可指定搜索范围（'名称备注'或'完整内容'）和最大结果数。当不知道具体脚本名、想找参考实现时使用。参数：关键词(必填)、搜索范围(可选,默认'名称备注')、最大结果数(可选,默认5)\n- expand_script — 展开查看一个已保存脚本的完整内部结构——显示脚本头部信息（进程名、窗口标题、备注）以及每个节点的类型名和所有字段值。图片/二进制数据会自动省略。用于学习参考已有脚本的实现。参数：脚本标识(必填,脚本文件名不含扩展名)\n- web_search — 通过 Google 搜索引擎搜索互联网上的实时信息。当需要获取最新资讯、事实核查、或用户明确要求搜索时使用。需先在全局设置中配置 Google API Key 和 CX。参数：查询关键词、最大结果数\n\n### search_scripts — 搜索脚本\n在所有已保存脚本中按关键词搜索（不限当前对话加载的工具范围），返回匹配列表。\n- 参数：关键词 (string, 必填)、搜索范围 (string, 可选, '名称备注'默认/'完整内容')、最大结果数 (int, 可选, 默认5)\n- 名称备注模式：只搜索脚本文件名和备注文字，速度快\n- 完整内容模式：额外读取每个脚本文件全文搜索，能找到引用了特定节点的脚本（如搜'单击按键'会返回所有包含此节点的脚本）\n- 结果超过限制时会提示剩余条数，可缩小搜索范围\n- 典型用法：用户说「有没有签到类的脚本可以参考」→ search_scripts('签到')；想知道哪些脚本用了某个节点 → search_scripts('点击图片', '完整内容')\n\n### expand_script — 展开查看脚本\n展开查看一个已保存脚本的内部节点结构（不限当前对话加载的工具范围）。\n- 参数：脚本标识 (string, 必填, 脚本文件名不含.script扩展名)\n- 返回：脚本名、目标进程、窗口标题、备注 → 每个节点的类型名和所有字段值\n- 图片/二进制数据会自动省略，替换为「[图片数据已省略]」或「[数据过长已省略]」\n- 支持精确匹配和模糊匹配（名称Contains），模糊匹配时会提示\n- 典型用法：search_scripts找到参考脚本后 → expand_script('脚本名') 查看节点结构 → 用系统工具模仿创建\n\n### ask_vision_ai - 询问视觉AI\n截取当前屏幕并调用视觉AI（多模态模型，如GPT-4o、Qwen-VL、llava等）进行分析，将视觉AI的回复直接返回。参数：提示词(string, 必填, 自定义分析Prompt)。你可以自由构造分析提示词来实现各种视觉分析需求，如描述屏幕、定位元素、判断状态等。需要先在全局设置中配置视觉AI。\n\n### wait_until_time - 等待时间\n设置一个绝对时间点进行等待，不阻塞对话。时间到达后系统会自动通知你继续执行任务。\n- 参数：绝对时间 (string, 格式 yyyy-MM-dd HH:mm:ss)\n- 调用后立即返回确认消息，你可告知用户等待已设置\n- 注意：每对话同时只能有一个活跃等待，设置新等待会覆盖旧等待\n- 注意：用户在等待期间发送新消息会自动取消等待\n\n### wait_for_event - 等待事件\n监听指定名称的触发任务（TriggerTask），事件触发后系统自动通知你继续。\n- 参数：事件名称 (string, 对应触发任务的名称)、超时分钟 (int, 可选, 默认30)\n- 事件名称需与系统中已配置的触发任务名称完全匹配\n- 调用后立即返回确认消息，等待不阻塞对话\n- 注意：每对话同时只能有一个活跃等待（时间或事件），用户发送新消息会取消等待\n\n### web_search - 联网搜索\n通过 Google 搜索引擎搜索互联网上的实时信息。当需要获取最新资讯、事实核查、或用户明确要求搜索时使用。\n- 参数：查询关键词 (string, 必填)、最大结果数 (int, 可选, 默认5, 范围1-10)\n- 搜索使用 Google 搜索引擎，需要先在全局设置中配置 API 密钥和搜索引擎 ID (CX)\n- 结果包含网页标题、摘要和链接，质量高于免费搜索接口\n- 注意：如搜索无结果建议尝试其他关键词；如提示未配置请联系用户设置\n\n### find_window - 查找窗口\n根据窗口标题获取窗口的屏幕矩形坐标。\n- 参数：窗口标题 (string, 必填, 部分匹配，如'记事本'、'Chrome')\n- 返回JSON：{\"found\": true, \"title\": \"完整标题\", \"rect\": {\"x\":..., \"y\":..., \"width\":..., \"height\":...}} 或 {\"found\": false}\n- 基于Windows API，无需视觉AI\n- 典型用法：find_window('记事本') → 获取窗口位置和大小 → 计算操作区域\n\n### wait_for_ai_reply - 等待AI回复\n等待另一个对话中的AI完成输出，获取最后一条消息内容后继续执行。\n- 参数：对话名 (string, 必填, 目标对话名称)、超时分钟 (int, 可选, 默认30)\n- 仅当目标对话AI自然完成输出时触发，用户手动停止不会触发\n- 调用后立即返回确认消息，等待不阻塞对话\n- 注意：每对话同时只能有一个活跃等待，用户发送新消息会取消等待\n- 典型用法：对话A生成脚本 → 对话B: wait_for_ai_reply('对话A') → 获得脚本内容后继续处理\n\n## 脚本学习与模仿工作流\n当用户要求参考已有脚本创建类似脚本时，建议按以下流程：\n1. search_scripts(关键词) — 先搜索是否有相关的参考脚本\n2. expand_script(脚本名) — 展开查看参考脚本的完整节点结构\n3. 理解参考脚本中每个节点的类型、参数和跳转逻辑\n4. 使用系统工具（create_working_script → add_node → save_script）模仿创建\n\n使用建议：ask_vision_ai适合需要查看屏幕的场景，write_log和read_log可配合使用完成日志记录与审查；modify_script_remark应在用户明确同意后才执行；search_scripts和expand_script是脚本学习的重要入口\n\n### 工具执行进度提示\n当 AI 调用脚本工具时，系统会实时显示每个节点的执行进度（格式：\"{节点名称}已执行/已等待{X.X}s  {索引}/{总数}\"）。\n如需隐藏进度（适用于长脚本或不需要用户感知的静默执行），可在调用工具时添加参数 \"__隐藏进度\": true。\n- find_window — 根据窗口标题（部分匹配）查找可见窗口，返回窗口的屏幕矩形坐标和大小。参数：窗口标题(必填,部分匹配即可)\n- wait_for_ai_reply — 等待另一个对话中的AI完成输出，返回最后一条消息内容。用于对话间协同，例如等另一个对话处理完后再继续。参数：对话名(必填,目标对话名称), 超时分钟(可选,默认30)" });

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

            var 经验系统消息 = 构建经验系统消息从历史(消息历史);
            if (!string.IsNullOrEmpty(经验系统消息))
            {
                sb.AppendLine(经验系统消息);
                sb.AppendLine();
            }

            if (AI配置管理器.获取启用增量记录())
            {
                sb.AppendLine("【执行计划提交要求】");
                sb.AppendLine("请在首次调用工具前，用以下格式提交你的执行计划：");
                sb.AppendLine("[计划]");
                sb.AppendLine("需求=你要完成的目标");
                sb.AppendLine("步骤=步骤描述|工具ID");
                sb.AppendLine("步骤=复杂步骤|*|子步骤1|工具ID|子步骤2|工具ID");
                sb.AppendLine("[/计划]");
                sb.AppendLine("- 需求= 后写本次要完成的目标描述");
                sb.AppendLine("- 步骤=描述|工具ID 表示一个叶子步骤，直接调用对应工具");
                sb.AppendLine("- 步骤=描述|*|子步骤1|工具ID|... 表示需分解的内部步骤，*后跟子步骤与工具的配对");
                sb.AppendLine("- 计划只需在首次工具调用前提交一次，后续工具调用按计划顺序执行即可");
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(自定义规则))
            {
                sb.AppendLine("你必须遵守以下规则：");
                sb.AppendLine(自定义规则);
                sb.AppendLine();
            }

            sb.AppendLine("【系统通知解读指南】");
            sb.AppendLine("当脚本执行输出中包含以下标记时，请按对应方式处理：");
            sb.AppendLine("- [通知] — 脚本正常运行中产生的普通信息，简要告知用户即可");
            sb.AppendLine("- [提示] — 脚本执行过程中的瞬时提示，可选告知用户");
            sb.AppendLine("- [错误-需关注] — 脚本遇到了错误，请务必向用户明确指出错误内容，提醒检查脚本配置或运行环境");
            sb.AppendLine("- [警告-请注意] — 脚本有潜在问题或需要注意的情况，请提醒用户留意");
            sb.AppendLine("- [交互-确认] — 脚本在执行中需要确认，已自动选择默认值继续。如结果显示不理想，可询问用户是否手动调整");
            sb.AppendLine("- [交互-输入] — 脚本在执行中需要输入，已自动使用默认值。如结果不符合预期，可询问用户是否手动指定");
            sb.AppendLine("- [交互-选择] — 脚本在执行中面临选项，已自动选择第一个选项。如结果不理想，可询问用户是否手动选择");
            sb.AppendLine("- [图片组] — 脚本产生了图片结果，但当前AI无法查看完整图片内容，只能看到尺寸摘要。请告知用户有图片生成");
            sb.AppendLine("- [屏幕截图分析] — 系统通过多模态视觉AI对当前屏幕进行了分析，以下是分析结果。请结合此信息理解当前屏幕状态");
            sb.AppendLine();
            sb.AppendLine("【通用MCP工具使用指南】");
            sb.AppendLine("除了脚本工具外，系统还提供了以下通用内置工具，可直接调用：");
            sb.AppendLine("- ask_vision_ai — 截取当前屏幕并调用视觉AI进行分析，将视觉AI的回复直接返回。你可以传入任意自定义分析Prompt，灵活控制视觉AI的分析内容。当你需要查看用户屏幕上有什么（窗口、按钮、文本内容等）时主动调用此工具。参数：提示词(必填,传给视觉AI的分析提示词,例如'屏幕上有哪些按钮?''登录按钮在什么位置?返回JSON坐标'等)");
            sb.AppendLine("- modify_script_remark — 修改当前MCP工具组中某个脚本的备注内容。仅能修改已加载为MCP工具的脚本，不能修改未加载的脚本。当发现脚本备注缺失、描述不准确或无法区分时，在征得用户同意后使用。参数：脚本名或路径、新备注");
            sb.AppendLine("- write_log — 将内容写入应用logs目录下的日志文件。适合记录调试信息、脚本运行状态或中间结果。参数：日志名、内容");
            sb.AppendLine("- read_log — 读取应用logs目录下的日志文件。可配合write_log使用，写入后读取确认。超过8000字符的内容会自动截断，此时应提醒用户直接查看文件。参数：日志名");
            sb.AppendLine("- search_scripts — 在所有已保存的脚本中按关键词搜索（不限当前对话加载的工具范围），返回匹配脚本的名称、分类和备注摘要。参数：关键词(必填)、搜索范围(可选,'名称备注'默认/'完整内容')、最大结果数(可选,默认5)");
            sb.AppendLine("- expand_script — 展开查看一个已保存脚本的内部节点结构（不限当前对话加载的工具范围），显示头部信息和每个节点的类型名与字段值。图片数据自动省略。参数：脚本标识(必填,脚本文件名不含扩展名)");
            sb.AppendLine("- web_search — 通过 Google 搜索引擎搜索互联网上的实时信息。需要先在全局设置中配置 Google API Key 和 CX。参数：查询关键词、最大结果数");
            sb.AppendLine("- list_node_types — 列出所有可用的节点类型及其关键字段，用于 build_script 时选择正确的节点类型");
            sb.AppendLine("- get_node_creation_rules — 获取节点创建的规则表，包括鼠标操作优先级、等待策略、循环构建方式等");
            sb.AppendLine("- create_working_script — 初始化一个工作脚本，设置进程名和窗口标题。参数：进程名(必填), 窗口标题(必填)");
            sb.AppendLine("- add_node — 向工作脚本追加一个节点。参数：节点类型(必填), 节点名字(可选), 节点备注(可选), 成功后等待(可选), 失败后等待(可选), 及其他节点特有字段");
            sb.AppendLine("- remove_node — 从工作脚本中移除指定索引的节点，自动修正其他节点的跳转引用。参数：索引(必填)");
            sb.AppendLine("- modify_node — 修改工作脚本中指定节点的字段值。参数：索引(必填), 参数字典(必填,JSON对象)");
            sb.AppendLine("- execute_node — 单独执行工作脚本中指定索引的节点，用于测试。参数：索引(必填)");
            sb.AppendLine("- save_script — 将当前工作脚本序列化保存为.script文件。参数：脚本名(必填), 备注(可选), 分类名(可选,默认'未分类')");
            sb.AppendLine("- list_all_scripts — 列出系统中所有已保存的脚本文件及其分类信息");
            sb.AppendLine("- classify_script — 将指定脚本移动到目标分类。参数：脚本名或路径(必填), 目标分类(必填)");
            sb.AppendLine("- edit_category_rule — 编辑指定分类的AI规则。参数：分类名(必填), AI规则(必填)");
            sb.AppendLine("- create_category — 创建新的脚本分类。参数：分类名(必填), 父分类(可选)");
            sb.AppendLine();
            sb.AppendLine("### search_scripts - 搜索脚本");
            sb.AppendLine("在所有已保存脚本中按关键词搜索。参数：关键词(必填)、搜索范围(可选,'名称备注'/'完整内容')、最大结果数(可选,默认5)。完整内容模式会读取脚本文件全文搜索，能找到引用了特定节点的脚本。");
            sb.AppendLine();
            sb.AppendLine("### expand_script - 展开查看脚本");
            sb.AppendLine("展开查看脚本的完整节点结构。参数：脚本标识(必填,脚本文件名不含扩展名)。返回头部信息+每个节点的类型名和字段值，图片/二进制数据自动省略。支持精确和模糊匹配。");
            sb.AppendLine();
            sb.AppendLine("## 脚本学习与模仿工作流");
            sb.AppendLine("当需要参考已有脚本创建类似脚本时：");
            sb.AppendLine("1. search_scripts(关键词) → 找参考脚本");
            sb.AppendLine("2. expand_script(脚本名) → 查看节点结构");
            sb.AppendLine("3. get_node_creation_rules → 了解节点创建规则");
            sb.AppendLine("4. create_working_script → add_node → save_script 逐步构建");
            sb.AppendLine();
            sb.AppendLine("### ask_vision_ai - 询问视觉AI");
            sb.AppendLine("截取当前屏幕并调用视觉AI（多模态模型，如GPT-4o、Qwen-VL、llava等）进行分析，将视觉AI的回复直接返回。参数：提示词(string, 必填, 自定义分析Prompt)。你可以自由构造分析提示词来实现各种视觉分析需求，如描述屏幕、定位元素、判断状态等。需要先在全局设置中配置视觉AI。");
            sb.AppendLine();
            sb.AppendLine("### wait_until_time - 等待时间");
            sb.AppendLine("设置一个绝对时间点进行等待，不阻塞对话。时间到达后系统会自动通知你继续执行任务。");
            sb.AppendLine("- 参数：绝对时间 (string, 格式 yyyy-MM-dd HH:mm:ss)");
            sb.AppendLine("- 调用后立即返回确认消息，你可告知用户等待已设置");
            sb.AppendLine("- 注意：每对话同时只能有一个活跃等待，设置新等待会覆盖旧等待");
            sb.AppendLine("- 注意：用户在等待期间发送新消息会自动取消等待");
            sb.AppendLine();
            sb.AppendLine("### wait_for_event - 等待事件");
            sb.AppendLine("监听指定名称的触发任务（TriggerTask），事件触发后系统自动通知你继续。");
            sb.AppendLine("- 参数：事件名称 (string, 对应触发任务的名称)、超时分钟 (int, 可选, 默认30)");
            sb.AppendLine("- 事件名称需与系统中已配置的触发任务名称完全匹配");
            sb.AppendLine("- 调用后立即返回确认消息，等待不阻塞对话");
            sb.AppendLine("- 注意：每对话同时只能有一个活跃等待（时间或事件），用户发送新消息会取消等待");
            sb.AppendLine();
            sb.AppendLine("### web_search - 联网搜索");
            sb.AppendLine("通过 Google 搜索引擎搜索互联网上的实时信息。当需要获取最新资讯、事实核查、或用户明确要求搜索时使用。");
            sb.AppendLine("- 参数：查询关键词 (string, 必填)、最大结果数 (int, 可选, 默认5, 范围1-10)");
            sb.AppendLine("- 搜索使用 Google 搜索引擎，需要先在全局设置中配置 API 密钥和搜索引擎 ID (CX)");
            sb.AppendLine("- 结果包含网页标题、摘要和链接，质量高于免费搜索接口");
            sb.AppendLine("- 注意：如搜索无结果建议尝试其他关键词；如提示未配置请联系用户设置");
            sb.AppendLine();
            sb.AppendLine("### list_node_types - 列出节点类型");
            sb.AppendLine("列出所有可用的节点类型及其关键字段，按分类组织。用于了解可用节点和 add_node 时选择正确类型。");
            sb.AppendLine();
            sb.AppendLine("### get_node_creation_rules - 节点创建规则");
            sb.AppendLine("获取构建脚本的规则表：鼠标操作优先级、等待策略、循环构建方式等。在构建脚本前必调用。");
            sb.AppendLine();
            sb.AppendLine("### create_working_script - 创建工作脚本");
            sb.AppendLine("初始化一个内存中的工作脚本。参数：进程名(string, 必填), 窗口标题(string, 必填)。创建后即可用 add_node 添加节点。");
            sb.AppendLine();
            sb.AppendLine("### add_node - 添加节点");
            sb.AppendLine("向工作脚本追加一个节点。参数：节点类型(string, 必填, 如'点击图片'、'输入文本'等), 节点名字(string, 可选), 节点备注(string, 可选), 成功后等待(int, 可选, 默认500ms), 失败后等待(int, 可选, 默认与成功后相同), 其他节点特有字段按需传入。必须先调用 list_node_types 了解可用类型。");
            sb.AppendLine();
            sb.AppendLine("### remove_node - 移除节点");
            sb.AppendLine("从工作脚本中移除指定索引的节点，自动修正其他节点的跳转引用。参数：索引(int, 必填, 从0开始)。");
            sb.AppendLine();
            sb.AppendLine("### modify_node - 修改节点");
            sb.AppendLine("修改工作脚本中指定节点的字段值。参数：索引(int, 必填), 参数字典(object, 必填, JSON对象, 键为字段名值为新值)。可修改基类字段：节点名字、节点备注、成功后等待、失败后等待。");
            sb.AppendLine();
            sb.AppendLine("### execute_node - 执行节点");
            sb.AppendLine("单独执行工作脚本中指定索引的节点，用于测试节点是否正确。参数：索引(int, 必填)。返回执行结果（成功/失败）。仅在必要时测试关键节点，避免误操作。");
            sb.AppendLine();
            sb.AppendLine("### save_script - 保存脚本");
            sb.AppendLine("将当前工作脚本序列化保存为.script文件并注册到分类中。参数：脚本名(string, 必填), 备注(string, 可选), 分类名(string, 可选, 默认'未分类')。");
            sb.AppendLine();
            sb.AppendLine("### list_all_scripts - 列出所有脚本");
            sb.AppendLine("列出系统中所有已保存的脚本文件及其分类信息。在开始构建前调用，检查是否已有匹配的脚本。");
            sb.AppendLine();
            sb.AppendLine("### classify_script - 分类脚本");
            sb.AppendLine("将指定脚本移动到目标分类。参数：脚本名或路径(string, 必填), 目标分类(string, 必填)。");
            sb.AppendLine();
            sb.AppendLine("### edit_category_rule - 编辑分类规则");
            sb.AppendLine("编辑指定分类的AI规则。参数：分类名(string, 必填), AI规则(string, 必填, 新规则内容)。");
            sb.AppendLine();
            sb.AppendLine("### create_category - 创建分类");
            sb.AppendLine("创建新的脚本分类。参数：分类名(string, 必填), 父分类(string, 可选, 默认创建在根目录)。");
            sb.AppendLine();
            sb.AppendLine("### find_window - 查找窗口");
            sb.AppendLine("根据窗口标题获取窗口的屏幕矩形坐标。参数：窗口标题(string, 必填, 部分匹配)。基于Windows API，返回JSON含found/title/rect。");
            sb.AppendLine();
            sb.AppendLine("### wait_for_ai_reply - 等待AI回复");
            sb.AppendLine("等待另一个对话中的AI完成输出并返回最后一条消息内容。参数：对话名(string, 必填), 超时分钟(int, 可选, 默认30)。仅自然完成触发，用户手动停止不触发。");
            sb.AppendLine();
            sb.AppendLine("## 工作流程");
            sb.AppendLine("1. 调用 list_all_scripts 检查是否已有匹配脚本。若无，继续以下步骤");
            sb.AppendLine("2. 调用 get_node_creation_rules 了解节点创建规则（鼠标优先级、等待策略等）");
            sb.AppendLine("3. 调用 create_working_script 初始化工作脚本");
            sb.AppendLine("4. 逐节点调用 add_node 构建脚本（设名字、备注、等待时间）");
            sb.AppendLine("5. 调用 execute_node 测试关键节点（打开应用、关键点击等）");
            sb.AppendLine("6. 调用 modify_node 修复问题（调整等待时间、改名字等）");
            sb.AppendLine("7. 调用 save_script 保存 → classify_script 分类 → 报告用户");
            sb.AppendLine();
            sb.AppendLine("## 循环检测");
            sb.AppendLine("审查节点列表时：");
            sb.AppendLine("- 如果操作模式重复（如 签到→确认→返回→签到），识别为循环");
            sb.AppendLine("- 将循环末节点设为成功后跳转到循环首节点");
            sb.AppendLine("- 确保失败后跳转指向停止任务或下一段落");
            sb.AppendLine("- 在节点备注标注「循环开始」「循环结束」");
            sb.AppendLine();
            sb.AppendLine("## 加权质量评估（save_script 前必须自查）");
            sb.AppendLine("| 维度 | 权重 | 检查项 |");
            sb.AppendLine("|------|------|--------|");
            sb.AppendLine("| 正确性 | 最高 | 流程能否完整执行？每个循环是否在正确页面状态完成？ |");
            sb.AppendLine("| 稳定性 | 较高 | 等待时间是否合理？关键步骤是否有失败跳转路径？ |");
            sb.AppendLine("| 可读性 | 中等 | 节点名字是否清晰有意义？ |");
            sb.AppendLine("| 效率 | 中等 | 等待时间是否足够短？有无冗余操作？ |");
            sb.AppendLine("| 名字明确度 | 中等 | 节点名字是否准确反映动作（动宾结构）？ |");
            sb.AppendLine("| 备注明确度 | 最低 | 节点备注是否补充了名字未涵盖的上下文？ |");
            sb.AppendLine("发现问题先 modify_node 修复，再 save_script。");
            sb.AppendLine();
            sb.AppendLine("## 命名要求");
            sb.AppendLine("- 节点名字：使用动宾结构，准确反映动作（如「点击签到按钮」「输入用户名」）");
            sb.AppendLine("- 节点备注：补充上下文信息（如「此按钮可能在登录后弹窗中出现」）");
            sb.AppendLine("- 不同节点名字应明显区分");
            sb.AppendLine();
            sb.AppendLine("## 安全约束");
            sb.AppendLine("- 禁止高危操作（删除系统文件、修改注册表、格式化磁盘等）");
            sb.AppendLine("- 如用户需求涉及高危操作，应拒绝并说明原因");
            sb.AppendLine("- execute_node 仅在必要时测试，避免误操作");
            sb.AppendLine();
            sb.AppendLine("使用建议：write_log和read_log可配合使用完成日志记录与审查；modify_script_remark应在用户明确同意后才执行");
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine("### 工具执行进度提示");
            sb.AppendLine("当 AI 调用脚本工具时，系统会实时显示每个节点的执行进度（格式：\"{节点名称}已执行/已等待{X.X}s  {索引}/{总数}\"）。");
            sb.AppendLine("如需隐藏进度（适用于长脚本或不需要用户感知的静默执行），可在调用工具时添加参数 \"__隐藏进度\": true。");

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
            if (config.提供者类型 == "Ollama本地")
            {
                return !string.IsNullOrEmpty(config.Ollama地址);
            }
            else
            {
                return !string.IsNullOrEmpty(config.远程API地址) && !string.IsNullOrEmpty(config.加密API密钥);
            }
        }

        public static AIConfigData 获取评审AI配置()
        {
            var cfg = 根据Id获取配置(_数据.评审AI配置Id);
            return cfg ?? 获取全局配置();
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
                    if (_数据.评审AI配置Id == id)
                        _数据.评审AI配置Id = 第一Id;
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

        private static string 构建经验系统消息从历史(List<AIChatMessage> 消息历史)
        {
            var 工具ID列表 = 提取工具ID从消息历史(消息历史);
            return AI使用经验管理器.构建经验系统消息(工具ID列表);
        }

        private static List<string> 提取工具ID从消息历史(List<AIChatMessage> 消息历史)
        {
            var 工具ID集 = new HashSet<string>();
            foreach (var msg in 消息历史)
            {
                if (msg.角色 == "AI" && !string.IsNullOrEmpty(msg.内容))
                {
                    var 匹配 = Regex.Matches(msg.内容, @"<tool_call>([^<]+)</tool_call>");
                    foreach (Match m in 匹配)
                        if (m.Groups.Count > 1)
                            工具ID集.Add(m.Groups[1].Value.Trim());
                }
            }
            return 工具ID集.ToList();
        }

        private static bool 工具在计划内(string 工具ID, 计划节点 计划)
        {
            if (计划 == null) return true;
            return 查找计划叶子工具(计划).Contains(工具ID);
        }

        private static HashSet<string> 查找计划叶子工具(计划节点 计划)
        {
            var 结果 = new HashSet<string>();
            if (计划 == null) return 结果;
            if (计划.子步骤.Count == 0)
            {
                if (!string.IsNullOrEmpty(计划.工具ID))
                    结果.Add(计划.工具ID);
                return 结果;
            }
            foreach (var 子 in 计划.子步骤)
            {
                foreach (var id in 查找计划叶子工具(子))
                    结果.Add(id);
            }
            return 结果;
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
