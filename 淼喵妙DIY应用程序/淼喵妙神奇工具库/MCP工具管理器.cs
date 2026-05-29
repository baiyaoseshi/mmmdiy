using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using McpDotNet.Protocol.Types;

namespace 淼喵妙神奇工具库
{
    public class MCPToolDefinition
    {
        public string 名称 { get; set; } = "";
        public string 工具ID { get; set; } = "";
        public string 描述 { get; set; } = "";
        public string 脚本路径 { get; set; } = "";
        public string 分类路径 { get; set; } = "";
        public string 分类AI规则 { get; set; } = "";
        public List<string> 祖先AI规则列表 { get; set; } = new List<string>();
        public List<string> 祖先分类路径列表 { get; set; } = new List<string>();
        public object 参数Schema { get; set; }
        public bool 已有统计数据 { get; set; }
        public string 脚本Id { get; set; } = "";
    }

    public static class MCP工具管理器
    {
        private static readonly string 用户文档路径 = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly AsyncLocal<StringBuilder> _输出收集器 = new AsyncLocal<StringBuilder>();

        public static bool 是否在MCP上下文中 => _输出收集器.Value != null;

        public static void 清空MCP上下文()
        {
            _输出收集器.Value = null;
        }

        public static void 追加输出(string 文本)
        {
            var sb = _输出收集器.Value;
            if (sb != null)
                sb.AppendLine(文本);
        }

        public static string 获取收集器输出()
        {
            var sb = _输出收集器.Value;
            _输出收集器.Value = null;
            if (sb == null || sb.Length == 0) return "";
            return sb.ToString();
        }

        // 保留用于向后兼容，新工具ID生成已改用GUID-based方案
        public static string 生成ASCII工具名(string 原始名称)
        {
            if (string.IsNullOrEmpty(原始名称)) return "tool_empty";

            var asciiChars = new List<char>();
            foreach (char c in 原始名称)
            {
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_' || c == '-')
                    asciiChars.Add(c);
            }

            if (asciiChars.Count > 0)
            {
                string result = new string(asciiChars.ToArray()).ToLowerInvariant();
                if (result.Length > 64) result = result.Substring(0, 64);
                return result;
            }

            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(原始名称));
            return "tool_" + Convert.ToHexStringLower(hash).Substring(0, 12);
        }

        public static List<MCPToolDefinition> 加载工具列表(List<string> 分类列表)
        {
            var tools = new List<MCPToolDefinition>();
            if (分类列表 == null || 分类列表.Count == 0) return tools;

            var allData = 加载UserData();
            if (allData == null) return tools;

            foreach (var cat in allData.TaskCategories ?? new List<对象型TaskCategory>())
            {
                递归收集分类脚本(cat, "", 分类列表, tools, new List<string>(), new List<string>());
            }

            var 已用ID = new HashSet<string>();
            for (int i = 0; i < tools.Count; i++)
            {
                string 基础ID = tools[i].工具ID;
                string 唯一ID = 基础ID;
                int 后缀 = 1;
                while (已用ID.Contains(唯一ID))
                {
                    唯一ID = 基础ID + "_" + 后缀;
                    后缀++;
                }
                已用ID.Add(唯一ID);
                tools[i].工具ID = 唯一ID;
            }

            return tools;
        }

        private static void 递归收集分类脚本(对象型TaskCategory category, string 前缀, List<string> 目标分类, List<MCPToolDefinition> 工具列表, List<string> 祖先规则, List<string> 祖先分类路径)
        {
            string 完整名 = string.IsNullOrEmpty(前缀) ? category.CategoryName : $"{前缀}/{category.CategoryName}";
            if (目标分类.Contains(完整名))
            {
                foreach (var 脚本路径 in category.TaskPaths ?? new List<string>())
                {
                    if (!File.Exists(脚本路径)) continue;

                    var (名称, 备注) = 解析脚本信息(脚本路径);

                    string 脚本Id;
                    try
                    {
                        var script = new 键鼠库.动作.自动任务脚本(IntPtr.Zero, File.ReadAllText(脚本路径));
                        脚本Id = script.脚本Id;
                        if (string.IsNullOrEmpty(脚本Id))
                            脚本Id = Guid.NewGuid().ToString("N");
                    }
                    catch
                    {
                        脚本Id = Guid.NewGuid().ToString("N");
                    }

                    string 基础ID = "tool_" + 脚本Id.Substring(0, 12);

                    工具列表.Add(new MCPToolDefinition
                    {
                        名称 = 名称,
                        工具ID = 基础ID,
                        描述 = string.IsNullOrEmpty(备注) ? $"执行脚本 {名称}" : 备注,
                        脚本路径 = 脚本路径,
                        分类路径 = 完整名,
                        分类AI规则 = category.AI规则 ?? "",
                        祖先AI规则列表 = new List<string>(祖先规则),
                        祖先分类路径列表 = new List<string>(祖先分类路径),
                        脚本Id = 脚本Id
                    });
                }
            }
            if (category.SubCategories != null)
            {
                var newAncestorRules = new List<string>(祖先规则);
                var newAncestorPaths = new List<string>(祖先分类路径);
                if (!string.IsNullOrEmpty(category.AI规则))
                {
                    newAncestorRules.Add(category.AI规则);
                    newAncestorPaths.Add(完整名);
                }
                foreach (var sub in category.SubCategories)
                {
                    递归收集分类脚本(sub, 完整名, 目标分类, 工具列表, newAncestorRules, newAncestorPaths);
                }
            }
        }

        public static object 构建工具定义列表(List<MCPToolDefinition> tools)
        {
            var definitions = new List<object>();
            foreach (var tool in tools)
            {
                string 分类标记 = string.IsNullOrEmpty(tool.分类路径) || 通用MCP工具.内置工具ID列表.Contains(tool.工具ID)
                    ? "" : $" [分类: {tool.分类路径}]";
                string 描述 = $"{tool.名称}{分类标记}: {tool.描述}";

                var (有印象, 置信度, _) = AI使用经验管理器.获取工具印象状态(tool.工具ID);
                if (有印象 && 置信度 >= 0.5f)
                {
                    var 印象 = AI使用经验管理器.获取工具印象(tool.工具ID);
                    if (!string.IsNullOrEmpty(印象.当前印象) && 印象.置信度 >= 0.5f)
                    {
                        描述 += $"【印象】{印象.当前印象}（置信度: {印象.置信度 * 100:F0}%）";
                    }
                }

                var 功能定义 = new Dictionary<string, object>
                {
                    ["name"] = tool.工具ID,
                    ["description"] = 描述
                };
                if (tool.参数Schema != null)
                    功能定义["parameters"] = tool.参数Schema;

                definitions.Add(new Dictionary<string, object>
                {
                    ["type"] = "function",
                    ["function"] = 功能定义
                });
            }
            return definitions;
        }

        public static string 构建Ollama工具描述(List<MCPToolDefinition> tools)
        {
            if (tools.Count == 0) return "";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("\n可用工具：");
            sb.AppendLine("- 脚本类工具调用格式：<tool_call>工具ID</tool_call>");
            sb.AppendLine("- 带参数的工具调用格式：<tool_call>工具ID|{\"参数名\":\"参数值\"}</tool_call>");
            sb.AppendLine();
            foreach (var tool in tools)
            {
                if (通用MCP工具.内置工具ID列表.Contains(tool.工具ID))
                {
                    sb.AppendLine($"- {tool.工具ID} [带参数]: {tool.名称} - {tool.描述}");
                }
                else
                {
                    string 分类标记 = string.IsNullOrEmpty(tool.分类路径) ? "" : $" [分类: {tool.分类路径}]";
                    sb.AppendLine($"- {tool.工具ID}{分类标记}: {tool.名称} - {tool.描述}");
                }
            }
            return sb.ToString();
        }

        public static Tool 转换为McpTool(MCPToolDefinition def)
        {
            return new Tool
            {
                Name = def.工具ID,
                Description = $"{def.名称}: {def.描述}",
                InputSchema = null
            };
        }

        public static List<Tool> 导出Mcp工具列表(List<MCPToolDefinition> tools)
        {
            return tools.Select(转换为McpTool).ToList();
        }

        public static string 执行工具(string 工具标识, List<MCPToolDefinition> tools, Dictionary<string, object> 参数 = null, Action<string> 进度回调 = null, string 对话ID = null, int 对话轮次 = 0, 计划节点 当前计划树 = null, List<计划节点> 树修改 = null)
        {
            if (通用MCP工具.内置工具ID列表.Contains(工具标识))
            {
                string 对话Id = AI配置管理器.当前对话Id上下文?.Value;
                return 通用MCP工具.执行内置工具(工具标识, 参数 ?? new Dictionary<string, object>(), tools, 对话Id);
            }

            var tool = tools.FirstOrDefault(t => t.工具ID == 工具标识 || t.名称 == 工具标识);
            if (tool == null || !File.Exists(tool.脚本路径))
                return $"错误: 找不到工具 {工具标识}";

            bool 隐藏进度 = false;
            if (参数 != null && 参数.TryGetValue("__隐藏进度", out var 隐藏值) && 隐藏值 is bool b && b)
            {
                隐藏进度 = true;
                参数.Remove("__隐藏进度");
            }

            var 计时器 = Stopwatch.StartNew();
            try
            {
                string 内容 = File.ReadAllText(tool.脚本路径);
                _输出收集器.Value = new StringBuilder();
                var 脚本 = new 键鼠库.动作.自动任务脚本(IntPtr.Zero, 内容);
                键鼠库.动作.任务控制管理器.实例.重置取消();
                脚本.执行(进度回调: 隐藏进度 ? null : 进度回调);
                计时器.Stop();
                string 基本结果 = $"工具 {tool.名称} 执行成功";
                string 收集输出 = 获取收集器输出();
                if (!string.IsNullOrEmpty(收集输出))
                    基本结果 += $"\n\n[脚本输出]\n{收集输出}";

                if (AI配置管理器.获取启用增量记录())
                {
                    AI使用经验管理器.追加调用记录(new 工具调用记录
                    {
                        工具ID = tool.工具ID,
                        调用时间 = DateTime.Now,
                        输入参数 = 参数 ?? new Dictionary<string, object>(),
                        是否成功 = true,
                        耗时ms = 计时器.ElapsedMilliseconds,
                        输出摘要 = 收集输出 ?? 基本结果,
                        对话ID = 对话ID ?? "",
                        对话轮次 = 对话轮次,
                        调用时计划树 = 当前计划树,
                        调用后树修改 = 树修改 ?? new List<计划节点>()
                    });
                }

                return 基本结果;
            }
            catch (Exception ex)
            {
                计时器.Stop();
                if (AI配置管理器.获取启用增量记录())
                {
                    AI使用经验管理器.追加调用记录(new 工具调用记录
                    {
                        工具ID = tool.工具ID,
                        调用时间 = DateTime.Now,
                        输入参数 = 参数 ?? new Dictionary<string, object>(),
                        是否成功 = false,
                        耗时ms = 计时器.ElapsedMilliseconds,
                        输出摘要 = ex.Message,
                        对话ID = 对话ID ?? "",
                        对话轮次 = 对话轮次,
                        调用时计划树 = 当前计划树,
                        调用后树修改 = 树修改 ?? new List<计划节点>()
                    });
                }
                return $"工具 {tool.名称} 执行失败: {ex.Message}";
            }
            finally
            {
                _输出收集器.Value = null;
            }
        }

        public static List<string> 解析ToolCall响应(string responseText, List<MCPToolDefinition> tools, Action<string> 进度回调 = null, string 对话ID = null, int 对话轮次 = 0, 计划节点 当前计划树 = null, List<计划节点> 树修改 = null)
        {
            var results = new List<string>();

            var regex = new Regex(@"<tool_call>(.*?)</tool_call>", RegexOptions.IgnoreCase);
            foreach (Match match in regex.Matches(responseText))
            {
                string captured = match.Groups[1].Value.Trim();
                string toolName;
                Dictionary<string, object> args = null;

                int pipeIndex = captured.IndexOf('|');
                if (pipeIndex >= 0)
                {
                    toolName = captured.Substring(0, pipeIndex).Trim();
                    string argsJson = captured.Substring(pipeIndex + 1).Trim();
                    if (!string.IsNullOrEmpty(argsJson) && argsJson.StartsWith("{"))
                    {
                        try
                        {
                            args = JsonSerializer.Deserialize<Dictionary<string, object>>(argsJson);
                        }
                        catch { }
                    }
                }
                else
                {
                    toolName = captured;
                }

                results.Add(执行工具(toolName, tools, args, 进度回调, 对话ID, 对话轮次, 当前计划树, 树修改));
            }

            return results;
        }

        private static (string, string) 解析脚本信息(string 脚本路径)
        {
            try
            {
                string 内容 = File.ReadAllText(脚本路径);
                string 脚本名 = Path.GetFileNameWithoutExtension(脚本路径);
                string 备注 = "";

                Regex 脚本备注Regex = new Regex(@"进程名\[([^\]]*)\]窗口标题\[([^\]]*)\](.*)");
                Match 脚本备注匹配 = 脚本备注Regex.Match(内容);
                if (脚本备注匹配.Success)
                {
                    备注 = 脚本备注匹配.Groups[3].Value.Trim();
                }

                if (string.IsNullOrEmpty(备注))
                {
                    Regex 节点备注Regex = new Regex(@"节点备注\[([^\]]*)\]");
                    Match 节点备注匹配 = 节点备注Regex.Match(内容);
                    if (节点备注匹配.Success)
                    {
                        备注 = 节点备注匹配.Groups[1].Value;
                    }
                }

                return (脚本名, 备注);
            }
            catch
            {
                return (Path.GetFileNameWithoutExtension(脚本路径), "");
            }
        }

        private class 对象型UserData
        {
            public List<对象型TaskCategory> TaskCategories { get; set; }
        }

        private class 对象型TaskCategory
        {
            public string CategoryName { get; set; }
            public string AI规则 { get; set; }
            public List<string> TaskPaths { get; set; }
            public List<对象型TaskCategory> SubCategories { get; set; }
        }

        private static 对象型UserData 加载UserData()
        {
            try
            {
                string path = Path.Combine(用户文档路径, "..", "AppData", "Roaming", "淼喵妙脚本DIY", "userdata.json");
                path = Path.GetFullPath(path);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<对象型UserData>(json);
                }
            }
            catch { }
            return new 对象型UserData();
        }
    }
}
