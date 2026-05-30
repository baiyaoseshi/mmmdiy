using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ChromaDB.Client;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库
{
    public class 向量记忆条目
    {
        public string Id { get; set; }
        public string 文档文本 { get; set; }
        public float 距离 { get; set; }
        public Dictionary<string, object> 元数据 { get; set; }
    }

    public static class 向量记忆管理器
    {
        private static readonly object _锁 = new object();
        private static ChromaClient _chromaClient;
        private static ChromaConfigurationOptions _chromaConfig;
        private static HttpClient _httpClient;
        private static bool _已初始化;
        private static string _chroma地址;
        private static bool _已通知连接成功;
        private static bool _已通知连接失败;
        private static int _连续失败计数;

        private const string 工具经验Collection = "tool_experiences";
        private const string 对话记忆Collection = "conversation_memory";
        private const string 计划模板Collection = "plan_templates";
        private const int 默认TopK = 5;

        private static Process _chroma进程;
        private static bool _正在启动;

        public static bool 是否可用 => _已初始化 && _chromaClient != null;

        public static string 当前连接状态
        {
            get
            {
                if (_已初始化) return "已连接";
                if (_正在启动) return "启动中...";
                if (_连续失败计数 > 0) return $"未连接 (失败{_连续失败计数}次)";
                return "未初始化";
            }
        }

        public static async Task<bool> 初始化(string chroma服务地址 = "http://localhost:8000")
        {
            lock (_锁)
            {
                if (_已初始化 && _chroma地址 == chroma服务地址)
                    return true;
            }

            _chroma地址 = chroma服务地址;

            if (await 尝试连接(chroma服务地址).ConfigureAwait(false))
                return true;

            if (!_已通知连接失败)
            {
                _已通知连接失败 = true;
                通知工具.吐司通知("🔧 ChromaDB 未运行，正在自动启动...");
            }

            await 启动ChromaDB服务().ConfigureAwait(false);

            for (int i = 0; i < 15; i++)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (await 尝试连接(chroma服务地址).ConfigureAwait(false))
                    return true;
            }

            lock (_锁) { _连续失败计数++; }
            _已通知连接成功 = false;
            通知工具.吐司通知($"⚠️ ChromaDB 启动失败 ({chroma服务地址})\n请手动: pip install chromadb && chroma run --port 8000");
            return false;
        }

        private static async Task<bool> 尝试连接(string 地址)
        {
            try
            {
                var config = new ChromaConfigurationOptions(uri: 地址.TrimEnd('/') + "/api/v1/");
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var chromaClient = new ChromaClient(config, client);
                await chromaClient.GetVersion().ConfigureAwait(false);

                _chromaConfig = new ChromaConfigurationOptions(uri: 地址.TrimEnd('/') + "/api/v1/");
                _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                _chromaClient = new ChromaClient(_chromaConfig, _httpClient);

                await _chromaClient.GetOrCreateCollection(工具经验Collection,
                    new Dictionary<string, object> { ["description"] = "工具调用经验向量存储" }).ConfigureAwait(false);
                await _chromaClient.GetOrCreateCollection(对话记忆Collection,
                    new Dictionary<string, object> { ["description"] = "对话摘要长期记忆" }).ConfigureAwait(false);
                await _chromaClient.GetOrCreateCollection(计划模板Collection,
                    new Dictionary<string, object> { ["description"] = "成功执行计划模板" }).ConfigureAwait(false);

                lock (_锁)
                {
                    _已初始化 = true;
                    _连续失败计数 = 0;
                    _正在启动 = false;
                }

                if (!_已通知连接成功)
                {
                    _已通知连接成功 = true;
                    _已通知连接失败 = false;
                    var 工具计数 = await 获取Collection计数(工具经验Collection).ConfigureAwait(false);
                    通知工具.吐司通知($"🧠 ChromaDB 长期记忆已连接 | 已有 {工具计数} 条工具经验");
                }

                return true;
            }
            catch
            {
                lock (_锁) { _已初始化 = false; }
                return false;
            }
        }

        private static async Task 启动ChromaDB服务()
        {
            _正在启动 = true;
            try
            {
                var python路径 = 查找Python路径();
                if (python路径 == null)
                {
                    通知工具.吐司通知("⚠️ 未找到 Python 环境，无法自动启动 ChromaDB");
                    return;
                }

                var 数据目录 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "淼喵妙脚本DIY", "chroma_data");
                Directory.CreateDirectory(数据目录);

                _chroma进程 = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = python路径,
                        Arguments = $"-m chromadb run --path \"{数据目录}\" --port 8000",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                _chroma进程.Start();
                await Task.Delay(500).ConfigureAwait(false);

                if (_chroma进程.HasExited)
                {
                    var error = await _chroma进程.StandardError.ReadToEndAsync().ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"ChromaDB 进程启动失败: {error}");
                    通知工具.吐司通知($"⚠️ ChromaDB 启动失败\n{error.Split('\n').FirstOrDefault() ?? ""}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动 ChromaDB 异常: {ex.Message}");
            }
        }

        private static string 查找Python路径()
        {
            var 候选路径列表 = new List<string>();

            var 应用目录 = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 5; i++)
            {
                var venvPath = Path.Combine(应用目录, ".venv", "Scripts", "python.exe");
                if (File.Exists(venvPath))
                {
                    候选路径列表.Add(venvPath);
                    break;
                }
                应用目录 = Directory.GetParent(应用目录)?.FullName;
                if (应用目录 == null) break;
            }

            if (候选路径列表.Count > 0) return 候选路径列表[0];

            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });
                if (process != null)
                {
                    process.WaitForExit(3000);
                    if (process.ExitCode == 0) return "python";
                }
            }
            catch { }

            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = "--version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });
                if (process != null)
                {
                    process.WaitForExit(3000);
                    if (process.ExitCode == 0) return "python3";
                }
            }
            catch { }

            return null;
        }

        public static async Task<bool> 添加工具经验(string 唯一Id, string 文档文本, float[] 向量,
            Dictionary<string, object> 元数据 = null)
        {
            if (!_已初始化) return false;
            try
            {
                var collection = await _chromaClient.GetOrCreateCollection(工具经验Collection).ConfigureAwait(false);
                var collectionClient = new ChromaCollectionClient(collection, _chromaConfig, _httpClient);
                await collectionClient.Add(
                    ids: new List<string> { 唯一Id },
                    embeddings: new List<ReadOnlyMemory<float>> { new ReadOnlyMemory<float>(向量) },
                    documents: new List<string> { 文档文本 },
                    metadatas: new List<Dictionary<string, object>> { 元数据 ?? new Dictionary<string, object>() }).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<List<向量记忆条目>> 搜索工具经验(float[] 查询向量, int topK = 5,
            Dictionary<string, object> 元数据过滤 = null)
        {
            return await 搜索Collection(工具经验Collection, 查询向量, topK, 元数据过滤).ConfigureAwait(false);
        }

        public static async Task<List<向量记忆条目>> 语义搜索工具经验(string 查询文本, AIConfigData config, int topK = 5)
        {
            var 向量 = await 嵌入服务.生成嵌入(查询文本, config).ConfigureAwait(false);
            if (向量.Length == 0) return new List<向量记忆条目>();
            return await 搜索工具经验(向量, topK).ConfigureAwait(false);
        }

        public static async Task<bool> 添加对话记忆(string 唯一Id, string 文档文本, float[] 向量,
            string 对话Id, Dictionary<string, object> 元数据 = null)
        {
            if (!_已初始化) return false;
            try
            {
                var meta = 元数据 ?? new Dictionary<string, object>();
                meta["对话Id"] = 对话Id;
                meta["时间戳"] = DateTime.Now.ToString("o");

                var collection = await _chromaClient.GetOrCreateCollection(对话记忆Collection).ConfigureAwait(false);
                var collectionClient = new ChromaCollectionClient(collection, _chromaConfig, _httpClient);
                await collectionClient.Add(
                    ids: new List<string> { 唯一Id },
                    embeddings: new List<ReadOnlyMemory<float>> { new ReadOnlyMemory<float>(向量) },
                    documents: new List<string> { 文档文本 },
                    metadatas: new List<Dictionary<string, object>> { meta }).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<List<向量记忆条目>> 搜索对话记忆(float[] 查询向量, string 对话Id = null, int topK = 5)
        {
            var filter = 对话Id != null ? new Dictionary<string, object> { ["对话Id"] = 对话Id } : null;
            return await 搜索Collection(对话记忆Collection, 查询向量, topK, filter).ConfigureAwait(false);
        }

        public static async Task<List<向量记忆条目>> 语义搜索对话记忆(string 查询文本, AIConfigData config,
            string 对话Id = null, int topK = 5)
        {
            var 向量 = await 嵌入服务.生成嵌入(查询文本, config).ConfigureAwait(false);
            if (向量.Length == 0) return new List<向量记忆条目>();
            return await 搜索对话记忆(向量, 对话Id, topK).ConfigureAwait(false);
        }

        public static async Task<bool> 添加计划模板(string 唯一Id, string 文档文本, float[] 向量,
            Dictionary<string, object> 元数据 = null)
        {
            if (!_已初始化) return false;
            try
            {
                var meta = 元数据 ?? new Dictionary<string, object>();
                meta["时间戳"] = DateTime.Now.ToString("o");

                var collection = await _chromaClient.GetOrCreateCollection(计划模板Collection).ConfigureAwait(false);
                var collectionClient = new ChromaCollectionClient(collection, _chromaConfig, _httpClient);
                await collectionClient.Add(
                    ids: new List<string> { 唯一Id },
                    embeddings: new List<ReadOnlyMemory<float>> { new ReadOnlyMemory<float>(向量) },
                    documents: new List<string> { 文档文本 },
                    metadatas: new List<Dictionary<string, object>> { meta }).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<List<向量记忆条目>> 搜索计划模板(float[] 查询向量, int topK = 5)
        {
            return await 搜索Collection(计划模板Collection, 查询向量, topK, null).ConfigureAwait(false);
        }

        public static async Task<List<向量记忆条目>> 语义搜索计划模板(string 查询文本, AIConfigData config, int topK = 5)
        {
            var 向量 = await 嵌入服务.生成嵌入(查询文本, config).ConfigureAwait(false);
            if (向量.Length == 0) return new List<向量记忆条目>();
            return await 搜索计划模板(向量, topK).ConfigureAwait(false);
        }

        public static async Task<bool> 删除条目(string collectionName, string 唯一Id)
        {
            if (!_已初始化) return false;
            try
            {
                var collection = await _chromaClient.GetOrCreateCollection(collectionName).ConfigureAwait(false);
                var collectionClient = new ChromaCollectionClient(collection, _chromaConfig, _httpClient);
                await collectionClient.Delete(ids: new List<string> { 唯一Id }).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<long> 获取Collection计数(string collectionName)
        {
            if (!_已初始化) return 0;
            try
            {
                var collection = await _chromaClient.GetOrCreateCollection(collectionName).ConfigureAwait(false);
                var collectionClient = new ChromaCollectionClient(collection, _chromaConfig, _httpClient);
                return await collectionClient.Count().ConfigureAwait(false);
            }
            catch
            {
                return 0;
            }
        }

        public static string 构建记忆上下文(List<向量记忆条目> 工具经验列表,
            List<向量记忆条目> 对话记忆列表 = null,
            List<向量记忆条目> 计划模板列表 = null)
        {
            if (工具经验列表 == null || 工具经验列表.Count == 0) return "";

            var sb = new StringBuilder();
            sb.AppendLine("【长期记忆 - 相关历史经验】");
            sb.AppendLine("以下是与你当前任务语义最相似的历史经验，可作为参考：");
            sb.AppendLine();

            if (工具经验列表.Count > 0)
            {
                sb.AppendLine("--- 相关工具调用经验 ---");
                foreach (var item in 工具经验列表.Take(默认TopK))
                {
                    sb.AppendLine($"• {item.文档文本} (相关度: {item.距离:F3})");
                    if (item.元数据?.TryGetValue("是否成功", out var success) == true)
                        sb.AppendLine($"  结果: {(success is bool b && b ? "✓成功" : "✗失败")}");
                    if (item.元数据?.TryGetValue("时间戳", out var ts) == true)
                        sb.AppendLine($"  时间: {ts}");
                }
                sb.AppendLine();
            }

            if (计划模板列表 != null && 计划模板列表.Count > 0)
            {
                sb.AppendLine("--- 相关成功计划 ---");
                foreach (var item in 计划模板列表.Take(3))
                {
                    sb.AppendLine($"• {item.文档文本}");
                }
                sb.AppendLine();
            }

            if (对话记忆列表 != null && 对话记忆列表.Count > 0)
            {
                sb.AppendLine("--- 相关对话记忆 ---");
                foreach (var item in 对话记忆列表.Take(3))
                {
                    sb.AppendLine($"• {item.文档文本}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("请参考以上历史经验，避免重复已知错误，优先采用已验证成功的方案。");
            return sb.ToString();
        }

        private static async Task<List<向量记忆条目>> 搜索Collection(string collectionName, float[] 查询向量,
            int topK, Dictionary<string, object> 元数据过滤)
        {
            if (!_已初始化 || 查询向量 == null || 查询向量.Length == 0)
                return new List<向量记忆条目>();

            try
            {
                var collection = await _chromaClient.GetOrCreateCollection(collectionName).ConfigureAwait(false);
                var collectionClient = new ChromaCollectionClient(collection, _chromaConfig, _httpClient);

                var results = await collectionClient.Query(
                    queryEmbeddings: new List<ReadOnlyMemory<float>> { new ReadOnlyMemory<float>(查询向量) },
                    nResults: topK,
                    include: ChromaQueryInclude.Documents | ChromaQueryInclude.Distances |
                             ChromaQueryInclude.Metadatas).ConfigureAwait(false);

                var 条目列表 = new List<向量记忆条目>();
                foreach (var batch in results)
                {
                    foreach (var entry in batch)
                    {
                        var 元数据 = entry.Metadata ?? new Dictionary<string, object>();
                        条目列表.Add(new 向量记忆条目
                        {
                            Id = entry.Id,
                            文档文本 = (entry.Document is string doc) ? doc : (entry.Document?.ToString() ?? ""),
                            距离 = (float)entry.Distance,
                            元数据 = 元数据
                        });
                    }
                }
                return 条目列表;
            }
            catch
            {
                return new List<向量记忆条目>();
            }
        }
    }
}
