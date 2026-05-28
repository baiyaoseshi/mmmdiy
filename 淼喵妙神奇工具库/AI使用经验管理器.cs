using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

    public class 工具经验条目
    {
        public string 规则 { get; set; } = "";
        public DateTime 更新时间 { get; set; }
    }

    public class 经验存储根
    {
        public Dictionary<string, 工具经验条目> 工具经验 { get; set; } = new Dictionary<string, 工具经验条目>();
        public string 微调后评审模型名 { get; set; } = "";
    }

    public class 统计存储根
    {
        public List<工具调用记录> 工具调用记录 { get; set; } = new List<工具调用记录>();
    }

    public static class AI使用经验管理器
    {
        private static readonly string _数据目录 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "淼喵妙脚本DIY");
        private static readonly string _经验文件路径 = Path.Combine(_数据目录, "ai_experience.json");
        private static readonly string _统计文件路径 = Path.Combine(_数据目录, "ai_tool_statistics.json");
        private static readonly object _统计锁 = new object();
        private static readonly object _经验锁 = new object();

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null
        };

        public static 经验存储根 加载经验()
        {
            lock (_经验锁)
            {
                try
                {
                    if (File.Exists(_经验文件路径))
                    {
                        string json = File.ReadAllText(_经验文件路径);
                        return JsonSerializer.Deserialize<经验存储根>(json, _jsonOptions) ?? new 经验存储根();
                    }
                }
                catch { }
                return new 经验存储根();
            }
        }

        public static void 保存经验(经验存储根 经验)
        {
            lock (_经验锁)
            {
                try
                {
                    if (!Directory.Exists(_数据目录))
                        Directory.CreateDirectory(_数据目录);

                    string json = JsonSerializer.Serialize(经验, _jsonOptions);
                    File.WriteAllText(_经验文件路径, json);
                }
                catch { }
            }
        }

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

        public static void 更新工具经验(string 工具ID, string 规则)
        {
            lock (_经验锁)
            {
                try
                {
                    var 经验 = 加载经验();
                    经验.工具经验[工具ID] = new 工具经验条目
                    {
                        规则 = 规则,
                        更新时间 = DateTime.Now
                    };
                    保存经验(经验);
                }
                catch { }
            }
        }

        public static 工具经验条目 获取工具经验(string 工具ID)
        {
            try
            {
                var 经验 = 加载经验();
                return 经验.工具经验.TryGetValue(工具ID, out var 条目) ? 条目 : null;
            }
            catch
            {
                return null;
            }
        }

        public static (bool 已有使用经验, string 经验更新时间, bool 已有统计数据) 获取经验状态(string 工具ID)
        {
            try
            {
                var 经验 = 加载经验();
                var 统计 = 加载统计();

                bool 有经验 = 经验.工具经验.TryGetValue(工具ID, out var 条目);
                bool 有统计 = 统计.工具调用记录.Any(r => r.工具ID == 工具ID);

                return (有经验, 有经验 ? 条目.更新时间.ToString("yyyy-MM-dd HH:mm:ss") : "", 有统计);
            }
            catch
            {
                return (false, "", false);
            }
        }

        public static void 清空工具经验(string 工具ID)
        {
            try
            {
                var 经验 = 加载经验();
                经验.工具经验.Remove(工具ID);
                保存经验(经验);

                var 统计 = 加载统计();
                统计.工具调用记录.RemoveAll(r => r.工具ID == 工具ID);
                保存统计(统计);
            }
            catch { }
        }

        public static void 清空所有经验()
        {
            try
            {
                File.Delete(_经验文件路径);
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

        public static string 导出微调数据()
        {
            try
            {
                var 统计 = 加载统计();
                var 记录列表 = 统计.工具调用记录;
                if (记录列表.Count == 0) return "";

                var 按对话分组 = 记录列表.GroupBy(r => r.对话ID).ToList();

                var jsonlLines = new List<string>();

                foreach (var 对话组 in 按对话分组)
                {
                    var 记录 = 对话组.OrderBy(r => r.调用时间).ToList();
                    if (记录.Count == 0) continue;

                    var instructionSb = new StringBuilder();
                    instructionSb.AppendLine("根据以下工具调用记录，学习完成各类任务的最佳工具选择和执行顺序：");
                    instructionSb.AppendLine();

                    var inputSb = new StringBuilder();
                    var outputSb = new StringBuilder();

                    for (int i = 0; i < 记录.Count; i++)
                    {
                        var r = 记录[i];
                        inputSb.AppendLine($"任务{i + 1}: {r.调用时计划树?.需求 ?? "未知需求"}");
                        inputSb.Append($"可用工具: {r.工具ID}");

                        outputSb.AppendLine($"步骤{i + 1}: 调用 {r.工具ID}");
                        outputSb.AppendLine($"  成功: {(r.是否成功 ? "是" : "否")}");
                        outputSb.AppendLine($"  耗时: {r.耗时ms}ms");
                        outputSb.AppendLine($"  结果: {r.输出摘要}");
                        outputSb.AppendLine();
                    }

                    var entry = new
                    {
                        instruction = instructionSb.ToString().Trim(),
                        input = inputSb.ToString().Trim(),
                        output = outputSb.ToString().Trim()
                    };

                    jsonlLines.Add(JsonSerializer.Serialize(entry, _jsonOptions));
                }

                string jsonl路径 = Path.Combine(_数据目录, "ai_finetune_data.jsonl");
                File.WriteAllText(jsonl路径, string.Join("\n", jsonlLines), Encoding.UTF8);
                return jsonl路径;
            }
            catch
            {
                return "";
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

        public static string 构建经验系统消息(List<string> 工具ID列表)
        {
            if (工具ID列表 == null || 工具ID列表.Count == 0) return "";

            try
            {
                var 经验 = 加载经验();
                var sb = new StringBuilder();

                sb.AppendLine("【工具使用经验】");
                sb.AppendLine("以下是基于历史使用记录总结的工具使用经验，请参考：");
                sb.AppendLine();

                bool 有经验 = false;
                foreach (var 工具ID in 工具ID列表)
                {
                    if (经验.工具经验.TryGetValue(工具ID, out var 条目) && !string.IsNullOrEmpty(条目.规则))
                    {
                        sb.AppendLine($"### {工具ID}");
                        sb.AppendLine(条目.规则);
                        sb.AppendLine();
                        有经验 = true;
                    }
                }

                if (!有经验)
                    return "";

                sb.AppendLine("以上经验仅供参考，请根据当前实际情况灵活使用。");
                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        public static async Task<(bool 可行, string 建议)> 评审计划(计划节点 计划, List<string> 工具ID列表)
        {
            if (计划 == null) return (false, "计划为空");

            try
            {
                var 全局配置 = AI配置管理器.获取全局配置();
                if (全局配置 == null || (全局配置.提供者类型 == "Ollama本地" && string.IsNullOrEmpty(全局配置.Ollama模型)))
                    return (true, null);

                var 统计 = 加载统计();
                var 经验 = 加载经验();

                string 微调模型 = 经验.微调后评审模型名;
                var 评审配置 = new AIConfigData
                {
                    提供者类型 = 全局配置.提供者类型,
                    Ollama地址 = 全局配置.Ollama地址,
                    Ollama模型 = !string.IsNullOrEmpty(微调模型) ? 微调模型 : 全局配置.Ollama模型,
                    远程API地址 = 全局配置.远程API地址,
                    加密API密钥 = 全局配置.加密API密钥,
                    远程模型 = 全局配置.远程模型
                };

                var 相似案例 = 检索相似工具序列(工具ID列表, 统计.工具调用记录, 5);

                var 计划描述 = 计划转为文本(计划);

                var 评审Prompt = new StringBuilder();
                评审Prompt.AppendLine("你是一个任务计划评审专家。请评审以下工具调用计划是否可行。");
                评审Prompt.AppendLine();
                评审Prompt.AppendLine("## 当前计划");
                评审Prompt.AppendLine(计划描述);
                评审Prompt.AppendLine();

                if (相似案例.Count > 0)
                {
                    评审Prompt.AppendLine("## 历史相似案例");
                    foreach (var 案例 in 相似案例)
                    {
                        评审Prompt.AppendLine($"- 工具: {案例.工具ID}, 成功: {(案例.是否成功 ? "是" : "否")}, 耗时: {案例.耗时ms}ms, 摘要: {案例.输出摘要}");
                    }
                    评审Prompt.AppendLine();
                }

                评审Prompt.AppendLine("请用以下格式回答：");
                评审Prompt.AppendLine("可行的理由或不可行的原因：");
                评审Prompt.AppendLine("[建议的具体修改方案]");

                var 消息历史 = new List<AIChatMessage>
                {
                    new AIChatMessage { 角色 = "系统", 内容 = 评审Prompt.ToString() },
                    new AIChatMessage { 角色 = "用户", 内容 = "请评审此计划" }
                };

                string 回复 = await AI配置管理器.调用AI(评审配置, 消息历史, "").ConfigureAwait(false);

                if (string.IsNullOrEmpty(回复))
                    return (true, null);

                bool 可行 = !回复.Contains("不可行") && !回复.Contains("无法执行");
                string 建议 = 回复.Length > 500 ? 回复.Substring(0, 500) : 回复;

                return (可行, 建议);
            }
            catch
            {
                return (true, null);
            }
        }

        private static string 计划转为文本(计划节点 计划, int 缩进 = 0)
        {
            if (计划 == null) return "";

            var sb = new StringBuilder();
            string 前缀 = new string(' ', 缩进 * 2);
            sb.AppendLine($"{前缀}- 需求: {计划.需求}");
            if (!string.IsNullOrEmpty(计划.工具ID))
            {
                sb.AppendLine($"{前缀}  工具: {计划.工具ID}");
            }

            foreach (var 子 in 计划.子步骤)
            {
                sb.Append(计划转为文本(子, 缩进 + 1));
            }

            return sb.ToString();
        }

        private static List<工具调用记录> 检索相似工具序列(List<string> 目标工具列表, List<工具调用记录> 所有记录, int 最大数量)
        {
            if (目标工具列表 == null || 目标工具列表.Count == 0 || 所有记录 == null || 所有记录.Count == 0)
                return new List<工具调用记录>();

            var 目标集 = new HashSet<string>(目标工具列表);

            var 相似度 = 所有记录
                .Select(r =>
                {
                    int 匹配数 = 目标集.Contains(r.工具ID) ? 1 : 0;
                    if (r.调用时计划树 != null)
                    {
                        var 计划工具列表 = 收集计划工具ID(r.调用时计划树);
                        int 重叠 = 计划工具列表.Intersect(目标集).Count();
                        匹配数 += 重叠;
                    }
                    return (记录: r, 得分: 匹配数);
                })
                .Where(x => x.得分 > 0)
                .OrderByDescending(x => x.得分)
                .ThenByDescending(x => x.记录.调用时间)
                .Take(最大数量)
                .Select(x => x.记录)
                .ToList();

            return 相似度;
        }

        private static List<string> 收集计划工具ID(计划节点 计划)
        {
            var 结果 = new List<string>();
            if (计划 == null) return 结果;

            if (!string.IsNullOrEmpty(计划.工具ID))
                结果.Add(计划.工具ID);

            foreach (var 子 in 计划.子步骤)
                结果.AddRange(收集计划工具ID(子));

            return 结果;
        }

        public static async Task 触发经验总结(AIConversation 对话, AIConfigData 总结AI配置)
        {
            if (对话 == null || 总结AI配置 == null) return;
            if (!AI配置管理器.获取启用自主学习()) return;
            try
            {
                var 统计 = 加载统计();
                var 对话记录 = 统计.工具调用记录
                    .Where(r => r.对话ID == 对话.Id)
                    .OrderBy(r => r.调用时间)
                    .ToList();

                if (对话记录.Count == 0) return;

                var 记录摘要 = new StringBuilder();
                记录摘要.AppendLine("以下是一个对话中的所有工具调用记录：");
                记录摘要.AppendLine();
                foreach (var 记录 in 对话记录)
                {
                    记录摘要.AppendLine($"工具: {记录.工具ID}");
                    记录摘要.AppendLine($"时间: {记录.调用时间:yyyy-MM-dd HH:mm:ss}");
                    记录摘要.AppendLine($"成功: {(记录.是否成功 ? "是" : "否")}");
                    记录摘要.AppendLine($"耗时: {记录.耗时ms}ms");
                    记录摘要.AppendLine($"结果: {记录.输出摘要}");
                    if (记录.调用时计划树 != null)
                    {
                        记录摘要.AppendLine($"计划: {记录.调用时计划树.需求}");
                    }
                    记录摘要.AppendLine();
                }

                var 总结Prompt = new StringBuilder();
                总结Prompt.AppendLine("你是一个工具使用经验总结专家。请根据以下工具调用记录，总结每个工具的使用经验。");
                总结Prompt.AppendLine();
                总结Prompt.AppendLine("输出格式要求：");
                总结Prompt.AppendLine("@@工具ID@@");
                总结Prompt.AppendLine("经验内容（包含使用场景、注意事项、最佳实践等）");
                总结Prompt.AppendLine("@@工具ID@@");
                总结Prompt.AppendLine("经验内容");
                总结Prompt.AppendLine();
                总结Prompt.AppendLine("请针对每个工具分别总结，使用上述格式。");
                总结Prompt.AppendLine();
                总结Prompt.AppendLine(记录摘要.ToString());

                var 消息历史 = new List<AIChatMessage>
                {
                    new AIChatMessage { 角色 = "系统", 内容 = 总结Prompt.ToString() },
                    new AIChatMessage { 角色 = "用户", 内容 = "请总结这些工具的使用经验" }
                };

                string 回复 = await AI配置管理器.调用AI(总结AI配置, 消息历史, "").ConfigureAwait(false);

                if (string.IsNullOrEmpty(回复)) return;

                var 匹配列表 = Regex.Matches(回复, @"@@([^@]+)@@\s*([\s\S]*?)(?=@@|$)", RegexOptions.None, TimeSpan.FromSeconds(2));

                foreach (Match m in 匹配列表)
                {
                    string 工具ID = m.Groups[1].Value.Trim();
                    string 经验内容 = m.Groups[2].Value.Trim();
                    if (!string.IsNullOrEmpty(工具ID) && !string.IsNullOrEmpty(经验内容))
                    {
                        更新工具经验(工具ID, 经验内容);
                    }
                }
            }
            catch { }
        }

        public static async Task 执行微调()
        {
            if (!AI配置管理器.获取启用自主学习()) return;
            try
            {
                var 统计 = 加载统计();
                if (统计.工具调用记录.Count < 20)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI使用经验管理器] 统计记录不足20条（当前{统计.工具调用记录.Count}条），跳过微调");
                    return;
                }

                var 经验 = 加载经验();
                var 全局配置 = AI配置管理器.获取全局配置();

                if (全局配置.提供者类型 != "Ollama本地")
                {
                    System.Diagnostics.Debug.WriteLine("[AI使用经验管理器] 当前未使用Ollama本地模型，跳过微调");
                    return;
                }

                string 基础模型 = 全局配置.Ollama模型;
                string 微调模型名 = $"{基础模型}-fine-tuned-{DateTime.Now:yyyyMMddHHmmss}";

                var 经验Prompt = 构建微调系统Prompt(经验, 统计);
                string 临时Modelfile路径 = Path.Combine(_数据目录, $"modelfile_{微调模型名}.txt");

                var modelfile内容 = new StringBuilder();
                modelfile内容.AppendLine($"FROM {基础模型}");
                modelfile内容.AppendLine($"SYSTEM \"\"\"{经验Prompt}\"\"\"");
                File.WriteAllText(临时Modelfile路径, modelfile内容.ToString(), Encoding.UTF8);

                var 进程信息 = new ProcessStartInfo
                {
                    FileName = "ollama",
                    Arguments = $"create {微调模型名} -f \"{临时Modelfile路径}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var 进程 = Process.Start(进程信息);
                if (进程 == null)
                {
                    System.Diagnostics.Debug.WriteLine("[AI使用经验管理器] 无法启动 ollama create 进程");
                    return;
                }

                string 输出 = await 进程.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                string 错误 = await 进程.StandardError.ReadToEndAsync().ConfigureAwait(false);
                await 进程.WaitForExitAsync().ConfigureAwait(false);

                if (进程.ExitCode == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[AI使用经验管理器] 模型 {微调模型名} 创建成功");

                    try { File.Delete(临时Modelfile路径); } catch { }

                    经验.微调后评审模型名 = 微调模型名;
                    保存经验(经验);

                    // TODO: 实验覆盖区域 ─────────────────────────────────
                    // 当前实现是**简单版**微调：
                    // 1. 收集所有工具使用经验（来自 ai_experience.json）
                    // 2. 收集所有工具调用统计（来自 ai_tool_statistics.json）
                    // 3. 构建增强版 System Prompt（含经验规则 + 工具调用统计摘要）
                    // 4. 通过 `ollama create` 创建新模型（FROM基础模型 + SYSTEM提示词）
                    //
                    // 进阶替代方案（用户可按需替换此处）：
                    // ─────────────────────────────────────────────────
                    // A. LoRA微调（推荐，效果最好）：
                    //    - 使用 `导出微调数据()` 获取 JSONL 训练数据
                    //    - 通过 unsloth/axolotl 等框架进行 QLoRA 微调
                    //    - 训练完成后生成 GGUF 格式，再用 ollama create 导入
                    //
                    // B. Unsloth + Ollama 集成：
                    //    unsloth: https://github.com/unslothai/unsloth
                    //    教程: https://docs.unsloth.ai/basics/tutorial-how-to-finetune-llama-3-and-use-in-ollama
                    //
                    // C. 使用 OpenAI 兼容 API 的微调服务：
                    //    - 直接调用 OpenAI Fine-tuning API
                    //    - 或使用 DeepSeek / 阿里云等平台的微调服务
                    //
                    // D. RAG 替代微调：
                    //    - 将经验数据存入向量数据库（如 ChromaDB）
                    //    - 每次调用时检索相关经验注入 context
                    //    优点：无需重训模型，经验即时更新
                    // ─────────────────────────────────────────────────
                    // 要替换此处逻辑，请修改以下内容：
                    // 1. 将训练数据（JSONL）发送到微调服务
                    // 2. 获得新模型名后调用：
                    //    经验.微调后评审模型名 = "新模型名";
                    //    保存经验(经验);
                    // 3. 如需通过 AI配置管理器更新全局配置，使用：
                    //    var cfg = AI配置管理器.获取全局配置();
                    //    cfg.Ollama模型 = 微调模型名;
                    //    AI配置管理器.更新全局配置(cfg);
                    // TODO: 实验覆盖区域结束 ────────────────────────────
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AI使用经验管理器] ollama create 失败: {错误}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AI使用经验管理器] 微调异常: {ex.Message}");
            }
        }

        private static string 构建微调系统Prompt(经验存储根 经验, 统计存储根 统计)
        {
            var sb = new StringBuilder();
            sb.AppendLine("你是一个经过经验微调的AI助手。以下是基于历史使用数据总结的规则和模式，请在回答时严格遵守。");
            sb.AppendLine();

            if (经验.工具经验.Count > 0)
            {
                sb.AppendLine("## 工具使用经验");
                foreach (var kv in 经验.工具经验)
                {
                    sb.AppendLine($"### {kv.Key}");
                    sb.AppendLine(kv.Value.规则);
                    sb.AppendLine();
                }
            }

            if (统计.工具调用记录.Count > 0)
            {
                sb.AppendLine("## 历史调用统计");
                var 按工具分组 = 统计.工具调用记录
                    .GroupBy(r => r.工具ID)
                    .Select(g => new
                    {
                        工具ID = g.Key,
                        总调用 = g.Count(),
                        成功率 = (double)g.Count(r => r.是否成功) / g.Count() * 100,
                        平均耗时 = g.Average(r => r.耗时ms)
                    })
                    .OrderByDescending(x => x.总调用)
                    .ToList();

                foreach (var item in 按工具分组)
                {
                    sb.AppendLine($"- {item.工具ID}: 调用{item.总调用}次, 成功率{item.成功率:F1}%, 平均耗时{item.平均耗时:F0}ms");
                }
                sb.AppendLine();
            }

            sb.AppendLine("请在每次回答时参考以上规则和统计信息，选择最合适的工具和调用方式。");

            return sb.ToString();
        }
    }
}
