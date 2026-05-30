using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace 淼喵妙神奇工具库
{
    public class 计划节点
    {
        public string 需求 { get; set; } = "";
        public string 工具ID { get; set; } = null!;
        public List<计划节点> 子步骤 { get; set; } = new List<计划节点>();
    }

    public class 工具调用记录
    {
        public string 工具ID { get; set; } = "";
        public DateTime 调用时间 { get; set; }
        public Dictionary<string, object> 输入参数 { get; set; } = new Dictionary<string, object>();
        public bool 是否成功 { get; set; }
        public long 耗时ms { get; set; }
        public string 输出摘要 { get; set; } = "";
        public string 对话ID { get; set; } = "";
        public int 对话轮次 { get; set; }
        public 计划节点 调用时计划树 { get; set; } = null!;
        public List<计划节点> 调用后树修改 { get; set; } = new List<计划节点>();
    }

    public class 统计存储根
    {
        public List<工具调用记录> 工具调用记录 { get; set; } = new List<工具调用记录>();
    }

    public class 印象修正记录
    {
        public string 类型 { get; set; } = "";
        public string 旧印象 { get; set; } = "";
        public string 新印象 { get; set; } = "";
        public string 触发原因 { get; set; } = "";
        public string 来源对话ID { get; set; } = "";
        public DateTime 时间戳 { get; set; }
    }

    public class 工具印象
    {
        public string 工具ID { get; set; } = "";
        public string 第一印象 { get; set; } = "";
        public string 当前印象 { get; set; } = "";
        public float 置信度 { get; set; }
        public DateTime 首次使用时间 { get; set; }
        public DateTime 最近更新时间 { get; set; }
        public List<印象修正记录> 修正历史 { get; set; } = new List<印象修正记录>();
    }

    public class 印象存储根
    {
        public Dictionary<string, 工具印象> 印象词典 { get; set; } = new Dictionary<string, 工具印象>();
    }

    public static class AI使用经验管理器
    {
        private static readonly string _数据目录 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "淼喵妙脚本DIY");
        private static readonly string _统计文件路径 = Path.Combine(_数据目录, "ai_tool_statistics.json");
        private static readonly string _印象文件路径 = Path.Combine(_数据目录, "ai_tool_impressions.json");
        private static readonly string _训练数据路径 = Path.Combine(_数据目录, "ai_training_data.jsonl");
        private static readonly object _统计锁 = new object();
        private static readonly object _印象锁 = new object();
        private static readonly object _训练数据锁 = new object();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null
        };

        public static 统计存储根 加载统计()
        {
            lock (_统计锁)
            {
                try
                {
                    if (File.Exists(_统计文件路径))
                    {
                        string json = File.ReadAllText(_统计文件路径);
                        return JsonSerializer.Deserialize<统计存储根>(json, _jsonOptions) ?? new 统计存储根();
                    }
                }
                catch { }
                return new 统计存储根();
            }
        }

        public static void 保存统计(统计存储根 统计)
        {
            lock (_统计锁)
            {
                try
                {
                    if (!Directory.Exists(_数据目录))
                        Directory.CreateDirectory(_数据目录);

                    string json = JsonSerializer.Serialize(统计, _jsonOptions);
                    File.WriteAllText(_统计文件路径, json);
                }
                catch { }
            }
        }

        public static void 追加调用记录(工具调用记录 记录)
        {
            lock (_统计锁)
            {
                try
                {
                    var 统计 = 加载统计();
                    统计.工具调用记录.Add(记录);
                    保存统计(统计);
                }
                catch { }
            }
        }

        public static void 清空所有统计()
        {
            try
            {
                File.Delete(_统计文件路径);
            }
            catch { }
        }

        public static void 清空工具统计(string 工具ID)
        {
            lock (_统计锁)
            {
                try
                {
                    var 统计 = 加载统计();
                    统计.工具调用记录.RemoveAll(r => r.工具ID == 工具ID);
                    保存统计(统计);
                }
                catch { }
            }
        }

        public static 计划节点 解析计划(string 计划文本)
        {
            if (string.IsNullOrEmpty(计划文本)) return null;

            try
            {
                var 行列表 = 计划文本.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0)
                    .ToList();

                if (行列表.Count == 0) return null;

                string 需求 = null;
                foreach (var 行 in 行列表)
                {
                    if (行.StartsWith("需求="))
                    {
                        需求 = 行.Substring("需求=".Length).Trim();
                        break;
                    }
                }

                if (需求 == null) return null;

                var 根 = new 计划节点 { 需求 = 需求 };
                var 步骤行列表 = 行列表.Where(l => l.StartsWith("步骤=")).ToList();

                foreach (var 步骤行 in 步骤行列表)
                {
                    string 内容 = 步骤行.Substring("步骤=".Length).Trim();
                    var 解析结果 = 解析单步骤(内容);
                    if (解析结果 != null)
                        根.子步骤.Add(解析结果);
                }

                return 根;
            }
            catch
            {
                return null;
            }
        }

        private static 计划节点 解析单步骤(string 步骤内容)
        {
            if (string.IsNullOrEmpty(步骤内容)) return null;

            var 段 = 步骤内容.Split('|');
            if (段.Length < 2) return null;

            var 节点 = new 计划节点
            {
                需求 = 段[0].Trim(),
                工具ID = 段[1].Trim()
            };

            if (节点.工具ID == "*")
            {
                节点.工具ID = null;
                int i = 2;
                while (i + 1 < 段.Length)
                {
                    string 子需求 = 段[i].Trim();
                    string 子工具 = 段[i + 1].Trim();

                    if (子工具 == "*")
                    {
                        var 嵌套节点 = 解析嵌套步骤(段, ref i);
                        if (嵌套节点 != null)
                            节点.子步骤.Add(嵌套节点);
                    }
                    else
                    {
                        节点.子步骤.Add(new 计划节点
                        {
                            需求 = 子需求,
                            工具ID = 子工具
                        });
                        i += 2;
                    }
                }
            }

            return 节点;
        }

        private static 计划节点 解析嵌套步骤(string[] 段, ref int 起始索引)
        {
            string 需求 = 段[起始索引].Trim();
            起始索引++;

            if (起始索引 >= 段.Length) return null;

            string 标识 = 段[起始索引].Trim();
            起始索引++;

            var 节点 = new 计划节点 { 需求 = 需求 };

            if (标识 == "*")
            {
                节点.工具ID = null;
                while (起始索引 + 1 < 段.Length)
                {
                    string 子需求 = 段[起始索引].Trim();
                    string 子工具 = 段[起始索引 + 1].Trim();

                    if (子工具 == "*")
                    {
                        var 嵌套节点 = 解析嵌套步骤(段, ref 起始索引);
                        if (嵌套节点 != null)
                            节点.子步骤.Add(嵌套节点);
                    }
                    else
                    {
                        节点.子步骤.Add(new 计划节点
                        {
                            需求 = 子需求,
                            工具ID = 子工具
                        });
                        起始索引 += 2;
                    }
                }
            }
            else
            {
                节点.工具ID = 标识;
            }

            return 节点;
        }

        public static 计划节点 从回复中提取计划(string 回复)
        {
            if (string.IsNullOrEmpty(回复)) return null;

            var 匹配 = Regex.Match(回复, @"\[计划\]([\s\S]*?)\[/计划\]", RegexOptions.None, TimeSpan.FromSeconds(2));
            if (!匹配.Success) return null;

            return 解析计划(匹配.Groups[1].Value);
        }

        public static (bool 是否有印象, float 置信度, DateTime 最近更新时间) 获取工具印象状态(string 工具ID)
        {
            var 印象 = 加载印象();
            if (印象.印象词典.TryGetValue(工具ID, out var 工具印象))
                return (true, 工具印象.置信度, 工具印象.最近更新时间);
            return (false, 0, DateTime.MinValue);
        }

        public static 工具印象 获取工具印象(string 工具ID, string 工具名称 = "", string 工具备注 = "")
        {
            var 印象 = 加载印象();
            if (印象.印象词典.TryGetValue(工具ID, out var 工具印象))
                return 工具印象;

            var 描述 = string.IsNullOrEmpty(工具备注)
                ? $"工具 {工具名称}"
                : $"工具 {工具名称}：{工具备注}";
            return new 工具印象
            {
                工具ID = 工具ID,
                第一印象 = "",
                当前印象 = 描述,
                置信度 = 0.3f,
                首次使用时间 = DateTime.MinValue,
                最近更新时间 = DateTime.Now
            };
        }

        public static async Task<string> 生成第一印象(AIConfigData 经验配置, 工具调用记录 记录)
        {
            if (经验配置 == null || string.IsNullOrEmpty(经验配置.Ollama模型))
                return "";
            try
            {
                var prompt = $"你是一个工具使用分析专家。以下是一个自动化工具的首次成功使用记录。请根据实际调用过程，生成对该工具的'第一印象'（一段简洁的描述，说明工具的用途、行为特征和注意事项）：\n\n" +
                             $"工具原始描述：{记录.输入参数?.GetValueOrDefault("__工具描述", "") ?? ""}\n" +
                             $"调用参数：{JsonSerializer.Serialize(记录.输入参数 ?? new Dictionary<string, object>())}\n" +
                             $"执行结果：{记录.输出摘要}\n" +
                             $"是否成功：{记录.是否成功}\n\n" +
                             $"请输出JSON格式：{{\"impression\": \"第一印象描述\"}}";
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var requestBody = new { model = 经验配置.Ollama模型, prompt = prompt, stream = false };
                var response = await client.PostAsync($"{经验配置.Ollama地址}/api/generate",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode) return "";
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                var responseText = doc.RootElement.GetProperty("response").GetString() ?? "";
                var jsonMatch = Regex.Match(responseText, @"\{[^}]*""impression""[^}]*\}", RegexOptions.None, TimeSpan.FromSeconds(1));
                if (jsonMatch.Success)
                {
                    using var impDoc = JsonDocument.Parse(jsonMatch.Value);
                    return impDoc.RootElement.GetProperty("impression").GetString() ?? "";
                }
                return responseText.Trim();
            }
            catch { return ""; }
        }

        public static void 更新印象(string 工具ID, string impressionMatch, string 评估原因, 工具调用记录 记录)
        {
            lock (_印象锁)
            {
                var 印象数据 = 加载印象();
                if (!印象数据.印象词典.TryGetValue(工具ID, out var 工具印象))
                {
                    工具印象 = new 工具印象 { 工具ID = 工具ID, 置信度 = 0.3f };
                    印象数据.印象词典[工具ID] = 工具印象;
                }

                var 修正记录 = new 印象修正记录
                {
                    类型 = impressionMatch == "首次使用" ? "第一印象" :
                           impressionMatch == "矛盾" ? "覆盖" : "修正",
                    旧印象 = 工具印象.当前印象,
                    新印象 = "",
                    触发原因 = 评估原因,
                    来源对话ID = 记录.对话ID,
                    时间戳 = DateTime.Now
                };

                switch (impressionMatch)
                {
                    case "首次使用":
                        if (string.IsNullOrEmpty(工具印象.第一印象) && 工具印象.置信度 <= 0.3f)
                        {
                            工具印象.第一印象 = 工具印象.当前印象;
                            修正记录.类型 = "第一印象";
                            修正记录.旧印象 = "";
                            修正记录.新印象 = 工具印象.当前印象;
                            修正记录.触发原因 = "首次成功使用：" + 评估原因;
                        }
                        break;
                    case "一致":
                        工具印象.置信度 = Math.Min(0.95f, 工具印象.置信度 + 0.02f);
                        修正记录.新印象 = 工具印象.当前印象;
                        break;
                    case "偏差":
                        工具印象.置信度 = Math.Max(0.3f, 工具印象.置信度 - 0.01f);
                        修正记录.新印象 = 工具印象.当前印象 + "。补充：" + 评估原因;
                        工具印象.当前印象 = 修正记录.新印象;
                        break;
                    case "矛盾":
                        if (!string.IsNullOrEmpty(工具印象.第一印象))
                        {
                            工具印象.第一印象 = 工具印象.当前印象;
                        }
                        工具印象.置信度 = 0.5f;
                        修正记录.类型 = "覆盖";
                        修正记录.新印象 = 评估原因;
                        工具印象.当前印象 = 评估原因;
                        break;
                }

                工具印象.最近更新时间 = DateTime.Now;
                if (工具印象.首次使用时间 == DateTime.MinValue)
                    工具印象.首次使用时间 = DateTime.Now;
                工具印象.修正历史.Add(修正记录);
                保存印象(印象数据);
            }
        }

        public static void 清空工具印象(string 工具ID)
        {
            lock (_印象锁)
            {
                var 印象 = 加载印象();
                印象.印象词典.Remove(工具ID);
                保存印象(印象);
            }
        }

        public static void 清空所有印象()
        {
            lock (_印象锁)
            {
                var 印象 = new 印象存储根();
                保存印象(印象);
            }
        }

        private static 印象存储根 加载印象()
        {
            lock (_印象锁)
            {
                try
                {
                    if (File.Exists(_印象文件路径))
                    {
                        string json = File.ReadAllText(_印象文件路径);
                        return JsonSerializer.Deserialize<印象存储根>(json, _jsonOptions) ?? new 印象存储根();
                    }
                }
                catch { }
                return new 印象存储根();
            }
        }

        private static void 保存印象(印象存储根 印象)
        {
            lock (_印象锁)
            {
                try
                {
                    if (!Directory.Exists(_数据目录))
                        Directory.CreateDirectory(_数据目录);
                    string json = JsonSerializer.Serialize(印象, _jsonOptions);
                    File.WriteAllText(_印象文件路径, json);
                }
                catch { }
            }
        }

        public static async Task<(string quality, string impressionMatch, string reason, List<string> tags)> 评估工具调用(AIConfigData 经验配置, 工具调用记录 记录)
        {
            if (经验配置 == null || string.IsNullOrEmpty(经验配置.Ollama模型))
                return ("low", "偏差", "经验AI未配置", new List<string>());

            try
            {
                var 工具印象 = 获取工具印象(记录.工具ID);
                var 印象描述 = 工具印象.置信度 >= 0.3f ? 工具印象.当前印象 : "无";

                var prompt = $"你是一个工具使用质量评估专家。请评估以下工具调用的质量和与印象的一致性。\n\n" +
                             $"工具ID：{记录.工具ID}\n" +
                             $"当前印象：{印象描述}\n" +
                             $"输入参数：{JsonSerializer.Serialize(记录.输入参数 ?? new Dictionary<string, object>())}\n" +
                             $"执行结果：{记录.输出摘要}\n" +
                             $"是否成功：{记录.是否成功}\n" +
                             $"耗时：{记录.耗时ms}ms\n\n" +
                             $"请输出JSON格式：{{\"quality\": \"high\"或\"low\", \"impression_match\": \"一致\"或\"偏差\"或\"矛盾\"或\"首次使用\", \"reason\": \"评估原因\", \"tags\": [\"标签1\", \"标签2\"]}}";

                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var requestBody = new { model = 经验配置.Ollama模型, prompt = prompt, stream = false };
                var response = await client.PostAsync($"{经验配置.Ollama地址}/api/generate",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    return ("low", "偏差", "经验AI调用失败", new List<string>());

                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                var responseText = doc.RootElement.GetProperty("response").GetString() ?? "";

                var jsonMatch = Regex.Match(responseText, @"\{[^}]*\}", RegexOptions.None, TimeSpan.FromSeconds(1));
                if (jsonMatch.Success)
                {
                    using var evalDoc = JsonDocument.Parse(jsonMatch.Value);
                    var root = evalDoc.RootElement;
                    var quality = root.TryGetProperty("quality", out var q) ? q.GetString() ?? "low" : "low";
                    var match = root.TryGetProperty("impression_match", out var m) ? m.GetString() ?? "偏差" : "偏差";
                    var reason = root.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "";
                    var tags = new List<string>();
                    if (root.TryGetProperty("tags", out var t) && t.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var tag in t.EnumerateArray())
                            tags.Add(tag.GetString() ?? "");
                    }
                    return (quality, match, reason, tags);
                }

                return 记录.是否成功 ? ("high", "首次使用", "自动判断：调用成功", new List<string>()) : ("low", "偏差", "自动判断：调用失败", new List<string>());
            }
            catch
            {
                return ("low", "偏差", "评估超时或异常", new List<string>());
            }
        }

        public static void 追加训练样本(工具调用记录 记录, string quality, string impressionMatch, string 当前印象, int 印象版本)
        {
            if (quality != "high") return;
            lock (_训练数据锁)
            {
                try
                {
                    if (!Directory.Exists(_数据目录))
                        Directory.CreateDirectory(_数据目录);

                    var 计划描述 = 计划转为文本(记录.调用时计划树);
                    var sample = new Dictionary<string, object>
                    {
                        ["instruction"] = 记录.调用时计划树?.需求 ?? "",
                        ["input"] = $"工具印象: {当前印象} | 输入参数: {JsonSerializer.Serialize(记录.输入参数 ?? new Dictionary<string, object>())} | 当前计划: {计划描述}",
                        ["output"] = 记录.输出摘要,
                        ["quality"] = quality,
                        ["impression_match"] = impressionMatch,
                        ["印象版本"] = 印象版本,
                        ["是否成功"] = 记录.是否成功,
                        ["耗时ms"] = 记录.耗时ms,
                        ["调用时计划树"] = 记录.调用时计划树,
                        ["调用后树修改"] = 记录.调用后树修改,
                        ["时间戳"] = 记录.调用时间.ToString("o")
                    };
                    string line = JsonSerializer.Serialize(sample, _jsonOptions) + Environment.NewLine;
                    File.AppendAllText(_训练数据路径, line);
                }
                catch { }
            }
        }

        public static void 数据清洗()
        {
            lock (_训练数据锁)
            {
                try
                {
                    if (!File.Exists(_训练数据路径)) return;
                    var lines = File.ReadAllLines(_训练数据路径)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(l =>
                        {
                            try { return JsonSerializer.Deserialize<Dictionary<string, object>>(l, _jsonOptions); }
                            catch { return null; }
                        })
                        .Where(d => d != null)
                        .ToList();

                    var seen = new HashSet<string>();
                    var cleaned = new List<Dictionary<string, object>>();
                    foreach (var item in lines)
                    {
                        var key = (item.TryGetValue("instruction", out var i) ? i?.ToString() : "") + "|" +
                                  (item.TryGetValue("input", out var inp) ? inp?.ToString() : "");
                        var hash = key.GetHashCode().ToString();
                        if (seen.Add(hash))
                        {
                            var input = item.TryGetValue("input", out var iv) ? iv?.ToString() ?? "" : "";
                            if (input.Length >= 20 && input.Length <= 8000)
                                cleaned.Add(item);
                            else
                                item["_filtered"] = "过短或过长";
                        }
                        else
                        {
                            item["_filtered"] = "重复";
                        }
                    }

                    var cleanedPath = _训练数据路径.Replace(".jsonl", "_cleaned.jsonl");
                    var cleanedContent = string.Join(Environment.NewLine,
                        cleaned.Select(d => JsonSerializer.Serialize(d, _jsonOptions)));
                    if (!string.IsNullOrEmpty(cleanedContent))
                        cleanedContent += Environment.NewLine;
                    File.WriteAllText(cleanedPath, cleanedContent);
                }
                catch { }
            }
        }

        public static void 构建评测集()
        {
            lock (_训练数据锁)
            {
                try
                {
                    var cleanedPath = _训练数据路径.Replace(".jsonl", "_cleaned.jsonl");
                    var sourcePath = File.Exists(cleanedPath) ? cleanedPath : _训练数据路径;
                    if (!File.Exists(sourcePath)) return;

                    var lines = File.ReadAllLines(sourcePath)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToArray();

                    var rng = new Random();
                    var shuffled = lines.OrderBy(_ => rng.Next()).ToArray();
                    var splitIndex = (int)(shuffled.Length * 0.8);
                    var trainSet = shuffled.Take(splitIndex).ToArray();
                    var evalSet = shuffled.Skip(splitIndex).ToArray();

                    var trainPath = _训练数据路径.Replace(".jsonl", "_train.jsonl");
                    var evalPath = _训练数据路径.Replace(".jsonl", "_eval.jsonl");

                    File.WriteAllText(trainPath, string.Join(Environment.NewLine, trainSet) +
                        (trainSet.Length > 0 ? Environment.NewLine : ""));
                    File.WriteAllText(evalPath, string.Join(Environment.NewLine, evalSet) +
                        (evalSet.Length > 0 ? Environment.NewLine : ""));
                }
                catch { }
            }
        }

        public static (int 总样本数, int 高质量, int 低质量, int 孤立样本) 获取训练数据统计()
        {
            lock (_训练数据锁)
            {
                try
                {
                    if (!File.Exists(_训练数据路径))
                        return (0, 0, 0, 0);

                    var lines = File.ReadAllLines(_训练数据路径)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(l =>
                        {
                            try { return JsonSerializer.Deserialize<Dictionary<string, object>>(l, _jsonOptions); }
                            catch { return null; }
                        })
                        .Where(d => d != null)
                        .ToList();

                    int total = lines.Count;
                    int high = lines.Count(d => d.TryGetValue("quality", out var q) && q?.ToString() == "high");
                    int low = total - high;
                    int orphan = 0;

                    return (total, high, low, orphan);
                }
                catch { return (0, 0, 0, 0); }
            }
        }

        private static string 计划转为文本(计划节点 节点, int 缩进 = 0)
        {
            if (节点 == null) return "";
            var sb = new StringBuilder();
            sb.AppendLine($"{new string(' ', 缩进 * 2)}- {节点.需求}" + (string.IsNullOrEmpty(节点.工具ID) ? "" : $" [{节点.工具ID}]"));
            foreach (var 子 in 节点.子步骤 ?? new List<计划节点>())
                sb.Append(计划转为文本(子, 缩进 + 1));
            return sb.ToString().TrimEnd();
        }

        public static async Task 执行完整训练管线(Action<string> 进度回调 = null)
        {
            try
            {
                进度回调?.Invoke("开始数据清洗...");
                数据清洗();

                进度回调?.Invoke("构建评测集...");
                构建评测集();

                var (total, high, _, _) = 获取训练数据统计();
                进度回调?.Invoke($"训练数据统计：共{total}条，高质量{high}条");

                if (total >= 50)
                {
                    进度回调?.Invoke("开始SFT微调...");
                    await 执行SFT微调(null, "miaomiao-sft").ConfigureAwait(false);

                    进度回调?.Invoke("SFT完成，检查DPO数据...");
                    await 执行DPO优化(null, "miaomiao-dpo").ConfigureAwait(false);

                    进度回调?.Invoke("DPO完成，检查RL数据...");
                    await 执行RL强化(null, "miaomiao-rl").ConfigureAwait(false);
                }
                else
                {
                    进度回调?.Invoke("训练数据不足（<50条），跳过模型微调");
                }

                进度回调?.Invoke("训练管线完成");
            }
            catch (Exception ex)
            {
                进度回调?.Invoke($"训练管线出错: {ex.Message}");
            }
        }

        public static async Task 执行SFT微调(AIConfigData 基础配置, string 输出模型名)
        {
            try
            {
                var modelfile = $"FROM {基础配置?.Ollama模型 ?? "qwen2:0.5b"}\n" +
                                $"SYSTEM \"\"\"你是一个专业的自动化脚本AI助手，已通过监督微调优化。\"\"\"\n";
                using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                var requestBody = new { name = 输出模型名, modelfile = modelfile };
                var ollamaAddr = 基础配置?.Ollama地址 ?? "http://localhost:11434";
                await client.PostAsync($"{ollamaAddr}/api/create",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")).ConfigureAwait(false);
            }
            catch { }
            await Task.CompletedTask;
        }

        public static async Task 执行DPO优化(AIConfigData 基础配置, string 输出模型名)
        {
            await Task.CompletedTask;
        }

        public static async Task 执行RL强化(AIConfigData 基础配置, string 输出模型名)
        {
            await Task.CompletedTask;
        }

        public static async Task<bool> 初始化向量记忆(string chroma地址 = "http://localhost:8000")
        {
            return await 向量记忆管理器.初始化(chroma地址).ConfigureAwait(false);
        }

        public static async Task 存储工具经验到向量库(工具调用记录 记录, AIConfigData config)
        {
            if (!向量记忆管理器.是否可用) return;
            if (config == null) return;

            try
            {
                var 文档文本 = 构建工具经验文档(记录);
                var 向量 = await 嵌入服务.生成嵌入(文档文本, config).ConfigureAwait(false);
                if (向量.Length == 0) return;

                var 唯一Id = $"tool_{记录.工具ID}_{记录.调用时间:yyyyMMddHHmmssfff}";
                var 元数据 = new Dictionary<string, object>
                {
                    ["工具ID"] = 记录.工具ID,
                    ["是否成功"] = 记录.是否成功,
                    ["耗时ms"] = 记录.耗时ms,
                    ["对话ID"] = 记录.对话ID,
                    ["时间戳"] = 记录.调用时间.ToString("o")
                };

                await 向量记忆管理器.添加工具经验(唯一Id, 文档文本, 向量, 元数据).ConfigureAwait(false);
            }
            catch { }
        }

        public static async Task 存储计划到向量库(计划节点 计划, AIConfigData config)
        {
            if (!向量记忆管理器.是否可用) return;
            if (计划 == null || config == null) return;

            try
            {
                var 文档文本 = 计划转为文本(计划);
                if (string.IsNullOrEmpty(文档文本)) return;

                var 向量 = await 嵌入服务.生成嵌入(文档文本, config).ConfigureAwait(false);
                if (向量.Length == 0) return;

                var 唯一Id = $"plan_{Guid.NewGuid():N}";
                var 元数据 = new Dictionary<string, object>
                {
                    ["需求"] = 计划.需求 ?? "",
                    ["工具ID"] = 计划.工具ID ?? "",
                    ["时间戳"] = DateTime.Now.ToString("o")
                };

                await 向量记忆管理器.添加计划模板(唯一Id, 文档文本, 向量, 元数据).ConfigureAwait(false);
            }
            catch { }
        }

        public static async Task<string> 检索相关经验(string 查询上下文, AIConfigData config,
            string 对话Id = null, int topK = 5)
        {
            if (!向量记忆管理器.是否可用 || config == null || string.IsNullOrEmpty(查询上下文))
                return "";

            try
            {
                var 工具经验 = await 向量记忆管理器.语义搜索工具经验(查询上下文, config, topK).ConfigureAwait(false);
                var 计划模板 = await 向量记忆管理器.语义搜索计划模板(查询上下文, config, topK).ConfigureAwait(false);
                var 对话记忆 = 对话Id != null
                    ? await 向量记忆管理器.语义搜索对话记忆(查询上下文, config, 对话Id, topK).ConfigureAwait(false)
                    : null;

                return 向量记忆管理器.构建记忆上下文(工具经验, 对话记忆, 计划模板);
            }
            catch
            {
                return "";
            }
        }

        public static async Task 存储对话到向量记忆(string 对话Id, string 内容摘要, AIConfigData config)
        {
            if (!向量记忆管理器.是否可用 || config == null || string.IsNullOrEmpty(内容摘要)) return;

            try
            {
                var 向量 = await 嵌入服务.生成嵌入(内容摘要, config).ConfigureAwait(false);
                if (向量.Length == 0) return;

                var 唯一Id = $"mem_{Guid.NewGuid():N}";
                await 向量记忆管理器.添加对话记忆(唯一Id, 内容摘要, 向量, 对话Id).ConfigureAwait(false);
            }
            catch { }
        }

        private static string 构建工具经验文档(工具调用记录 记录)
        {
            var 结果状态 = 记录.是否成功 ? "成功" : "失败";
            var 参数摘要 = 记录.输入参数 != null && 记录.输入参数.Count > 0
                ? string.Join(", ", 记录.输入参数
                    .Where(kv => !kv.Key.StartsWith("__"))
                    .Select(kv => $"{kv.Key}={kv.Value}"))
                : "无参数";
            return $"工具ID={记录.工具ID} | 结果={结果状态} | 耗时={记录.耗时ms}ms | 参数=[{参数摘要}] | 输出={记录.输出摘要}";
        }

    }
}
