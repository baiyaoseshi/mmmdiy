using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.键鼠库.动作;
using 淼喵妙神奇工具库.键鼠库.动作.流程节点;

namespace 淼喵妙神奇工具库
{
    public static class 通用MCP工具
    {
        public static readonly string 日志目录 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

        public static readonly string[] 内置工具ID列表 = { "ask_vision_ai", "modify_script_remark", "write_log", "read_log", "wait_until_time", "wait_for_event", "web_search", "expand_script", "search_scripts", "list_node_types", "get_node_creation_rules", "create_working_script", "add_node", "remove_node", "modify_node", "execute_node", "save_script", "list_all_scripts", "classify_script", "edit_category_rule", "create_category", "find_window", "wait_for_ai_reply" };

        public static string 执行内置工具(string 工具ID, Dictionary<string, object> 参数, List<MCPToolDefinition> 当前工具列表 = null, string 对话Id = null)
        {
            return 工具ID switch
            {
                "ask_vision_ai" => 询问视觉AI(参数),
                "modify_script_remark" => 修改脚本备注(参数, 当前工具列表),
                "write_log" => 写日志(参数),
                "read_log" => 读日志(参数),
                "wait_until_time" => 等待时间(参数, 对话Id),
                "wait_for_event" => 等待事件(参数, 对话Id),
                "web_search" => 联网搜索(参数),
                "expand_script" => 展开脚本(参数, 当前工具列表),
                "search_scripts" => 搜索脚本(参数, 当前工具列表),
                "list_node_types" => 列出节点类型(),
                "get_node_creation_rules" => 获取节点创建规则(),
                "create_working_script" => 创建工作脚本(参数),
                "add_node" => 添加节点(参数),
                "remove_node" => 移除节点(参数),
                "modify_node" => 修改节点(参数),
                "execute_node" => 执行节点(参数),
                "save_script" => 保存脚本(参数),
                "list_all_scripts" => 列出所有脚本(),
                "classify_script" => 分类脚本(参数),
                "edit_category_rule" => 编辑分类规则(参数),
                "create_category" => 创建分类(参数),
                "find_window" => 查找窗口框(参数),
                "wait_for_ai_reply" => 等待AI回复(参数, 对话Id),
                _ => $"错误: 未知的内置工具 '{工具ID}'"
            };
        }

        public static List<MCPToolDefinition> 获取通用工具列表()
        {
            return new List<MCPToolDefinition>
            {
                new MCPToolDefinition
                {
                    名称 = "询问视觉AI",
                    工具ID = "ask_vision_ai",
                    描述 = "截取当前屏幕并调用视觉AI分析，将视觉AI回复直接返回。参数: 提示词(必填,传给视觉AI的分析Prompt)",
                    脚本路径 = "",
                    参数Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["提示词"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "传给视觉AI的分析提示词"
                            }
                        },
                        ["required"] = new List<string> { "提示词" }
                    }
                },
                new MCPToolDefinition
                {
                    名称 = "修改脚本备注",
                    工具ID = "modify_script_remark",
                    描述 = "修改当前MCP工具组中某个脚本的备注。参数: 脚本名或路径(必填), 新备注(必填)",
                    脚本路径 = "",
                    参数Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["脚本名或路径"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "脚本文件名（不含.script扩展名）或完整路径"
                            },
                            ["新备注"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "新的备注内容"
                            }
                        },
                        ["required"] = new List<string> { "脚本名或路径", "新备注" }
                    }
                },
                new MCPToolDefinition
                {
                    名称 = "写日志",
                    工具ID = "write_log",
                    描述 = "将日志写入应用logs目录。参数: 日志名(必填), 内容(必填)",
                    脚本路径 = "",
                    参数Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["日志名"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "日志文件名（如 'debug_20250101.log'）"
                            },
                            ["内容"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "要写入的日志内容"
                            }
                        },
                        ["required"] = new List<string> { "日志名", "内容" }
                    }
                },
                new MCPToolDefinition
                {
                    名称 = "读日志",
                    工具ID = "read_log",
                    描述 = "读取应用logs目录中的日志文件,超8000字符自动截断。参数: 日志名(必填)",
                    脚本路径 = "",
                    参数Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["日志名"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "要读取的日志文件名"
                            }
                        },
                        ["required"] = new List<string> { "日志名" }
                    }
                },
                new MCPToolDefinition
                {
                    名称 = "等待时间",
                    工具ID = "wait_until_time",
                    描述 = "设置绝对时间点等待,时间到自动通知。参数: 绝对时间(必填,格式yyyy-MM-dd HH:mm:ss)",
                    脚本路径 = "",
                    参数Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["绝对时间"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "绝对时间点，格式 yyyy-MM-dd HH:mm:ss，如 '2026-05-24 14:30:00'"
                            }
                        },
                        ["required"] = new List<string> { "绝对时间" }
                    }
                },
                new MCPToolDefinition
                {
                    名称 = "等待事件",
                    工具ID = "wait_for_event",
                    描述 = "监听触发任务,事件触发后自动通知。参数: 事件名称(必填), 超时分钟(可选,默认30)",
                    脚本路径 = "",
                    参数Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["事件名称"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "系统触发任务的名称，需完全匹配"
                            },
                            ["超时分钟"] = new Dictionary<string, object>
                            {
                                ["type"] = "integer",
                                ["description"] = "等待超时时间（分钟），默认30分钟"
                            }
                        },
                        ["required"] = new List<string> { "事件名称" }
                    }
                },
                new MCPToolDefinition
                {
                    名称 = "联网搜索",
                    工具ID = "web_search",
                    描述 = "联网搜索实时信息。参数: 查询关键词(必填), 最大结果数(可选,默认5,范围1-10)",
                    脚本路径 = "",
                    参数Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["查询关键词"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "搜索关键词，支持中英文"
                            },
                            ["最大结果数"] = new Dictionary<string, object>
                            {
                                ["type"] = "integer",
                                ["description"] = "返回的最大结果数量，默认5条，范围1-10"
                            }
                        },
                        ["required"] = new List<string> { "查询关键词" }
                    }
                },
                new MCPToolDefinition
                {
                    名称 = "展开查看脚本",
                    工具ID = "expand_script",
                    描述 = "展开查看已保存脚本的完整内部结构（节点及参数）。参数: 脚本标识(必填,文件名不含扩展名)",
                    脚本路径 = "",
                    参数Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["脚本标识"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "脚本文件名（不含.script扩展名），如'每日签到'"
                            }
                        },
                        ["required"] = new List<string> { "脚本标识" }
                    }
                },
                new MCPToolDefinition
                {
                    名称 = "搜索脚本",
                    工具ID = "search_scripts",
                    描述 = "按关键词搜索已保存脚本。参数: 关键词(必填), 搜索范围(可选,'名称备注'/'完整内容'), 最大结果数(可选,默认5)",
                    脚本路径 = "",
                    参数Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["关键词"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "搜索关键词，如'签到'或'单击按键'"
                            },
                            ["搜索范围"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "搜索范围：'名称备注'（默认，只搜脚本名和备注）或 '完整内容'（读取脚本文件全文搜索）"
                            },
                            ["最大结果数"] = new Dictionary<string, object>
                            {
                                ["type"] = "integer",
                                ["description"] = "返回的最大脚本数量，默认5条"
                            }
                        },
                        ["required"] = new List<string> { "关键词" }
                    }
                },
                new MCPToolDefinition { 名称 = "列出节点类型", 工具ID = "list_node_types", 描述 = "列出所有可用节点类型及关键字段", 脚本路径 = "" },
                new MCPToolDefinition { 名称 = "节点创建规则", 工具ID = "get_node_creation_rules", 描述 = "获取节点创建规则表", 脚本路径 = "" },
                new MCPToolDefinition { 名称 = "创建工作脚本", 工具ID = "create_working_script", 描述 = "初始化工作脚本。参数: 进程名(必填), 窗口标题(必填)", 脚本路径 = "", 参数Schema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object> { ["进程名"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "目标进程名" }, ["窗口标题"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "目标窗口标题" } }, ["required"] = new List<string> { "进程名", "窗口标题" } } },
                new MCPToolDefinition { 名称 = "添加节点", 工具ID = "add_node", 描述 = "向工作脚本追加节点。参数: 节点类型(必填), 其他字段按需传入", 脚本路径 = "", 参数Schema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object> { ["节点类型"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "节点类名" }, ["节点名字"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "显示名称" }, ["节点备注"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "备注" }, ["成功后等待"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "成功后等待(ms)" }, ["失败后等待"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "失败后等待(ms)" } }, ["required"] = new List<string> { "节点类型" } } },
                new MCPToolDefinition { 名称 = "移除节点", 工具ID = "remove_node", 描述 = "移除指定索引节点并修正跳转引用。参数: 索引(必填,从0开始)", 脚本路径 = "", 参数Schema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object> { ["索引"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "节点位置，从0开始" } }, ["required"] = new List<string> { "索引" } } },
                new MCPToolDefinition { 名称 = "修改节点", 工具ID = "modify_node", 描述 = "修改节点字段值。参数: 索引(必填), 参数字典(必填,JSON对象)", 脚本路径 = "", 参数Schema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object> { ["索引"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "节点索引" }, ["参数字典"] = new Dictionary<string, object> { ["type"] = "object", ["description"] = "修改的字段键值对" } }, ["required"] = new List<string> { "索引", "参数字典" } } },
                new MCPToolDefinition { 名称 = "执行节点", 工具ID = "execute_node", 描述 = "单独执行指定节点用于测试。参数: 索引(必填)", 脚本路径 = "", 参数Schema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object> { ["索引"] = new Dictionary<string, object> { ["type"] = "integer", ["description"] = "节点索引" } }, ["required"] = new List<string> { "索引" } } },
                new MCPToolDefinition { 名称 = "保存脚本", 工具ID = "save_script", 描述 = "保存工作脚本为.script文件。参数: 脚本名(必填), 备注(可选), 分类名(可选,默认'未分类')", 脚本路径 = "", 参数Schema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object> { ["脚本名"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "文件名（不含扩展名）" }, ["备注"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "脚本备注" }, ["分类名"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "所属分类" } }, ["required"] = new List<string> { "脚本名" } } },
                new MCPToolDefinition { 名称 = "列出所有脚本", 工具ID = "list_all_scripts", 描述 = "列出所有已保存脚本及分类信息", 脚本路径 = "" },
                new MCPToolDefinition { 名称 = "分类脚本", 工具ID = "classify_script", 描述 = "将脚本移到目标分类。参数: 脚本名或路径(必填), 目标分类(必填)", 脚本路径 = "", 参数Schema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object> { ["脚本名或路径"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "脚本名或完整路径" }, ["目标分类"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "目标分类名" } }, ["required"] = new List<string> { "脚本名或路径", "目标分类" } } },
                new MCPToolDefinition { 名称 = "编辑分类规则", 工具ID = "edit_category_rule", 描述 = "编辑分类的AI规则。参数: 分类名(必填), AI规则(必填)", 脚本路径 = "", 参数Schema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object> { ["分类名"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "分类名" }, ["AI规则"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "新规则内容" } }, ["required"] = new List<string> { "分类名", "AI规则" } } },
                new MCPToolDefinition { 名称 = "创建分类", 工具ID = "create_category", 描述 = "创建新脚本分类。参数: 分类名(必填), 父分类(可选)", 脚本路径 = "", 参数Schema = new Dictionary<string, object> { ["type"] = "object", ["properties"] = new Dictionary<string, object> { ["分类名"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "分类名" }, ["父分类"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "父分类名" } }, ["required"] = new List<string> { "分类名" } } },
                new MCPToolDefinition
                {
                    名称 = "查找窗口",
                    工具ID = "find_window",
                    描述 = "根据窗口标题查找窗口坐标。参数: 窗口标题(必填,部分匹配)",
                    脚本路径 = "",
                    参数Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["窗口标题"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "目标窗口标题，部分匹配"
                            }
                        },
                        ["required"] = new List<string> { "窗口标题" }
                    }
                },
                new MCPToolDefinition
                {
                    名称 = "等待AI回复",
                    工具ID = "wait_for_ai_reply",
                    描述 = "等待另一个对话的AI完成输出并返回最后消息。参数: 对话名(必填), 超时分钟(可选,默认30)",
                    脚本路径 = "",
                    参数Schema = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = new Dictionary<string, object>
                        {
                            ["对话名"] = new Dictionary<string, object>
                            {
                                ["type"] = "string",
                                ["description"] = "要等待的目标对话名称"
                            },
                            ["超时分钟"] = new Dictionary<string, object>
                            {
                                ["type"] = "integer",
                                ["description"] = "等待超时时间（分钟），默认30分钟"
                            }
                        },
                        ["required"] = new List<string> { "对话名" }
                    }
                },
            };
        }

        private static string 修改脚本备注(Dictionary<string, object> 参数, List<MCPToolDefinition> 当前工具列表)
        {
            if (!参数.TryGetValue("新备注", out var 新备注Obj))
                return "错误: 缺少参数 '新备注'";
            string 新备注 = 新备注Obj?.ToString() ?? "";

            string 脚本标识 = "";
            if (参数.TryGetValue("脚本名或路径", out var 标识Obj) && 标识Obj != null)
                脚本标识 = 标识Obj.ToString();

            if (string.IsNullOrEmpty(脚本标识))
                return "错误: 缺少参数 '脚本名或路径'";

            string 脚本路径 = 在工具组中查找脚本(脚本标识, 当前工具列表);
            if (脚本路径 == null || !File.Exists(脚本路径))
                return $"错误: 在当前的MCP工具组中找不到脚本 '{脚本标识}'。可用脚本仅限当前对话加载的MCP工具组中的脚本。";

            try
            {
                string 内容 = File.ReadAllText(脚本路径);
                var regex = new Regex(@"^(进程名\[[^\]]*\]窗口标题\[[^\]]*\])([^\r\n@]*)", RegexOptions.Multiline);
                var match = regex.Match(内容);
                if (!match.Success)
                    return "错误: 脚本格式不正确，无法解析头部信息";

                string 头部前缀 = match.Groups[1].Value;
                string 剩余内容 = "";
                int 匹配结束位置 = match.Index + match.Length;
                if (匹配结束位置 < 内容.Length)
                    剩余内容 = 内容.Substring(匹配结束位置);

                string 新内容 = 头部前缀 + 新备注 + 剩余内容;
                File.WriteAllText(脚本路径, 新内容);

                string 脚本名 = Path.GetFileNameWithoutExtension(脚本路径);
                return $"已成功将脚本 '{脚本名}' 的备注修改为: {新备注}";
            }
            catch (Exception ex)
            {
                return $"修改脚本备注失败: {ex.Message}";
            }
        }

        private static string 写日志(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("日志名", out var 日志名Obj) || string.IsNullOrEmpty(日志名Obj?.ToString()))
                return "错误: 缺少参数 '日志名'";
            if (!参数.TryGetValue("内容", out var 内容Obj) || 内容Obj == null)
                return "错误: 缺少参数 '内容'";

            string 日志名 = 日志名Obj.ToString();
            string 内容 = 内容Obj.ToString();

            if (!Directory.Exists(日志目录))
                Directory.CreateDirectory(日志目录);

            string 文件路径 = Path.Combine(日志目录, 日志名);
            File.WriteAllText(文件路径, 内容);

            return $"已成功写入日志: {文件路径}";
        }

        private static string 读日志(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("日志名", out var 日志名Obj) || string.IsNullOrEmpty(日志名Obj?.ToString()))
                return "错误: 缺少参数 '日志名'";

            string 日志名 = 日志名Obj.ToString();
            string 文件路径 = Path.Combine(日志目录, 日志名);

            if (!File.Exists(文件路径))
                return $"错误: 日志文件不存在: {文件路径}";

            try
            {
                string 内容 = File.ReadAllText(文件路径);
                if (内容.Length > 8000)
                    内容 = 内容.Substring(0, 8000) + $"\n\n... (日志过长，已截断，完整日志共 {内容.Length} 字符)";
                return 内容;
            }
            catch (Exception ex)
            {
                return $"读取日志失败: {ex.Message}";
            }
        }

        private static string 等待时间(Dictionary<string, object> 参数, string 对话Id)
        {
            if (!参数.TryGetValue("绝对时间", out var 时间Obj) || string.IsNullOrEmpty(时间Obj?.ToString()))
                return "错误: 缺少参数 '绝对时间'";

            string 时间字符串 = 时间Obj.ToString();

            if (!DateTime.TryParseExact(时间字符串, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var 目标时间))
                return $"错误: 时间格式不正确，请使用 yyyy-MM-dd HH:mm:ss 格式，例如 '2026-05-24 14:30:00'";

            if (目标时间 <= DateTime.Now)
                return $"错误: 指定时间已过期，当前时间 {DateTime.Now:yyyy-MM-dd HH:mm:ss}，指定时间 {目标时间:yyyy-MM-dd HH:mm:ss}";

            if (string.IsNullOrEmpty(对话Id))
                return "错误: 无法获取当前对话信息";

            等待管理器.创建时间等待(对话Id, 目标时间);

            return $"启动成功正在等待，将于 {目标时间:yyyy-MM-dd HH:mm:ss} 触发。在此期间您可以继续其他操作。";
        }

        private static string 等待事件(Dictionary<string, object> 参数, string 对话Id)
        {
            if (!参数.TryGetValue("事件名称", out var 名称Obj) || string.IsNullOrEmpty(名称Obj?.ToString()))
                return "错误: 缺少参数 '事件名称'";

            string 事件名称 = 名称Obj.ToString();
            int 超时分钟 = 30;

            if (参数.TryGetValue("超时分钟", out var 超时Obj) && 超时Obj != null)
            {
                if (超时Obj is int i)
                    超时分钟 = i;
                else if (int.TryParse(超时Obj.ToString(), out var parsed))
                    超时分钟 = parsed;
            }

            if (string.IsNullOrEmpty(对话Id))
                return "错误: 无法获取当前对话信息";

            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "淼喵妙脚本DIY", "userdata.json");
            if (!File.Exists(appData))
                return "错误: 用户数据文件不存在";

            string json = File.ReadAllText(appData);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("触发任务列表", out var tasks))
                return "错误: 触发任务列表为空";

            bool 找到 = false;
            foreach (var task in tasks.EnumerateArray())
            {
                if (task.TryGetProperty("任务名", out var nameProp) &&
                    string.Equals(nameProp.GetString()?.Trim(), 事件名称?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    找到 = true;
                    break;
                }
            }

            if (!找到)
                return $"错误: 找不到名为「{事件名称}」的触发事件，请检查事件名称是否正确。";

            等待管理器.创建事件等待(对话Id, 事件名称, 超时分钟);

            return $"启动成功正在等待，正在监听事件「{事件名称}」。在此期间您可以继续其他操作。";
        }

        private static string 等待AI回复(Dictionary<string, object> 参数, string 对话Id)
        {
            if (!参数.TryGetValue("对话名", out var 名称Obj) || string.IsNullOrEmpty(名称Obj?.ToString()))
                return "错误: 缺少参数 '对话名'";

            string 对话名 = 名称Obj.ToString();
            int 超时分钟 = 30;

            if (参数.TryGetValue("超时分钟", out var 超时Obj) && 超时Obj != null)
            {
                if (超时Obj is int i)
                    超时分钟 = i;
                else if (int.TryParse(超时Obj.ToString(), out var parsed))
                    超时分钟 = parsed;
            }

            if (string.IsNullOrEmpty(对话Id))
                return "错误: 无法获取当前对话信息";

            var 目标对话 = AI配置管理器.按名称查找对话(对话名);
            if (目标对话 == null)
                return $"错误: 找不到名为「{对话名}」的对话，请检查对话名称是否正确。";

            等待管理器.创建AI对话完成等待(对话Id, 对话名, 超时分钟);

            return $"启动成功正在等待，等待对话「{对话名}」的AI完成回复。在此期间您可以继续其他操作。";
        }

        private static string 联网搜索(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("查询关键词", out var queryObj) || string.IsNullOrEmpty(queryObj?.ToString()))
                return "错误: 缺少参数 '查询关键词'";

            string query = queryObj.ToString();
            int maxResults = 5;
            if (参数.TryGetValue("最大结果数", out var maxObj) && maxObj != null)
            {
                if (maxObj is int i)
                    maxResults = Math.Clamp(i, 1, 10);
                else if (int.TryParse(maxObj.ToString(), out var parsed))
                    maxResults = Math.Clamp(parsed, 1, 10);
            }

            var 全局配置 = AI配置管理器.获取全局配置();
            string apiKey = AI配置管理器.解密密钥(全局配置.加密GoogleAPI密钥);
            string cx = 全局配置.Google搜索引擎ID;

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(cx))
                return "联网搜索未配置，请在全局设置中填写 Google API Key 和搜索引擎 ID (CX)。\n获取方式：\n1. Google Cloud Console 启用 Custom Search API → 创建 API Key\n2. Programmable Search Engine 创建搜索引擎 → 获取 CX";

            try
            {
                var service = new Google.Apis.CustomSearchAPI.v1.CustomSearchAPIService(new Google.Apis.Services.BaseClientService.Initializer
                {
                    ApiKey = apiKey,
                    ApplicationName = "MiaoMiaoScriptDIY"
                });

                var listRequest = service.Cse.List();
                listRequest.Cx = cx;
                listRequest.Q = query;
                listRequest.Num = maxResults;

                var search = listRequest.Execute();

                var sb = new StringBuilder();
                sb.AppendLine($"🔍 搜索: {query}");
                sb.AppendLine();

                if (search.Items == null || search.Items.Count == 0)
                {
                    sb.AppendLine("未找到相关搜索结果，建议尝试其他关键词。");
                }
                else
                {
                    sb.AppendLine($"📝 共 {search.Items.Count} 条结果");
                    sb.AppendLine();

                    int count = 0;
                    foreach (var item in search.Items)
                    {
                        count++;
                        sb.AppendLine($"{count}. {item.Title}");
                        if (!string.IsNullOrEmpty(item.Snippet))
                            sb.AppendLine($"   {item.Snippet}");
                        if (!string.IsNullOrEmpty(item.Link))
                            sb.AppendLine($"   {item.Link}");
                        sb.AppendLine();
                        if (count >= maxResults) break;
                    }
                }

                string result = sb.ToString().TrimEnd();
                if (result.Length > 6000)
                    result = result.Substring(0, 6000) + $"\n\n... (搜索结果过长，已截断，共 {result.Length} 字符)";
                return result;
            }
            catch (Exception ex)
            {
                return $"搜索失败: {ex.Message}";
            }
        }

        private static string 询问视觉AI(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("提示词", out var 提示词Obj) || string.IsNullOrEmpty(提示词Obj?.ToString()))
                return "错误: 缺少参数 '提示词'";

            string 提示词 = 提示词Obj.ToString();

            var 当前对话 = AI配置管理器.获取当前对话();
            AIConfigData 视觉配置;
            if (当前对话 != null && !string.IsNullOrEmpty(当前对话.多模态AI配置Id))
                视觉配置 = AI配置管理器.根据Id获取配置(当前对话.多模态AI配置Id);
            else
                视觉配置 = AI配置管理器.获取视觉AI配置();

            if (!AI配置管理器.配置是否有效(视觉配置))
                return "视觉AI未配置。请在全局设置（🌐按钮）中配置「视觉 AI 配置」，需要多模态模型（如 gpt-4o、qwen-vl-max、llava 等）。";

            var 截图 = AI配置管理器.捕获屏幕截图();
            if (截图 == null)
                return "无法截取屏幕，请检查系统权限。";

            try
            {
                var 结果 = AI配置管理器.调用AI分析截图(视觉配置, 截图, 提示词).GetAwaiter().GetResult();
                if (string.IsNullOrEmpty(结果))
                {
                    string 模型信息 = 视觉配置.提供者类型 == "Ollama本地"
                        ? $"Ollama / {视觉配置.Ollama模型}"
                        : $"远程API / {视觉配置.远程模型}";
                    string 地址信息 = 视觉配置.提供者类型 == "Ollama本地"
                        ? $"地址: {视觉配置.Ollama地址}"
                        : $"地址: {视觉配置.远程API地址}";
                    return $"视觉AI分析失败。\n当前配置：{模型信息}\n{地址信息}\n\n可能原因：\n1. 模型不支持多模态（图片输入）—— 如当前模型是纯文本模型，需换成 qwen-vl-max、gpt-4o、llava 等视觉模型\n2. API 地址需包含完整路径（如末尾加 /chat/completions）\n3. API 密钥无效或余额不足\n\n请在全局设置（🌐按钮）中检查「视觉 AI 配置」。";
                }
                return $"[屏幕截图分析]\n{结果}";
            }
            catch (Exception ex)
            {
                return $"截图分析出错: {ex.Message}";
            }
        }

        private static readonly string[] 图片字段名列表 = { "模板数据", "模板图像", "图片数据", "截图数据" };

        private static string 格式化字段值(string 字段名, string 字段值)
        {
            if (string.IsNullOrEmpty(字段值))
                return "(空)";

            if (字段名 == "条件列表")
                return 格式化条件列表(字段值);

            if (字段值.Length > 300)
            {
                if (图片字段名列表.Contains(字段名, StringComparer.OrdinalIgnoreCase))
                    return "[图片数据已省略]";

                bool 疑似Base64 = 字段值.Length > 500 &&
                    Regex.IsMatch(字段值, @"^[A-Za-z0-9+/=\r\n\s]+$") &&
                    (字段值.Length % 4 == 0 || 字段值.EndsWith("="));
                if (疑似Base64)
                    return $"[数据过长已省略，原长度: {字段值.Length} 字符]";

                return 字段值.Substring(0, 200) + $"... (数据过长已截断，原长度: {字段值.Length} 字符)";
            }

            return 字段值;
        }

        private static string 格式化条件列表(string json)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != System.Text.Json.JsonValueKind.Array || root.GetArrayLength() == 0)
                    return "(无)";

                var parts = new List<string>();
                foreach (var item in root.EnumerateArray())
                {
                    string 类型 = item.TryGetProperty("类型", out var t) ? t.GetString() : "";
                    bool 反转 = item.TryGetProperty("反转", out var r) && r.GetBoolean();

                    if (类型 == "变量")
                    {
                        string 变量名 = "";
                        if (item.TryGetProperty("变量数据", out var vd) && vd.TryGetProperty("变量名", out var vn))
                            变量名 = vn.GetString() ?? "";
                        parts.Add(反转
                            ? $"非变量: {(string.IsNullOrEmpty(变量名) ? "?" : 变量名)}"
                            : $"变量: {(string.IsNullOrEmpty(变量名) ? "?" : 变量名)}");
                    }
                    else if (类型 == "图片")
                    {
                        parts.Add(反转 ? "非图片出现" : "图片出现");
                    }
                    else
                    {
                        parts.Add(反转 ? "非: " + 类型 : 类型);
                    }
                }
                return string.Join(", ", parts);
            }
            catch
            {
                return "(条件数据解析失败)";
            }
        }

        private static string 展开脚本(Dictionary<string, object> 参数, List<MCPToolDefinition> 当前工具列表)
        {
            if (!参数.TryGetValue("脚本标识", out var 标识Obj) || string.IsNullOrEmpty(标识Obj?.ToString()))
                return "错误: 缺少参数 '脚本标识'";

            string 脚本标识 = 标识Obj.ToString();
            string 匹配路径 = null;
            string 匹配名称 = null;

            var 所有脚本 = 收集所有脚本信息();
            foreach (var (路径, 名称) in 所有脚本)
            {
                if (string.Equals(路径, 脚本标识, StringComparison.OrdinalIgnoreCase))
                {
                    匹配路径 = 路径;
                    匹配名称 = 名称;
                    break;
                }
            }

            if (匹配路径 == null)
            {
                foreach (var (路径, 名称) in 所有脚本)
                {
                    if (string.Equals(名称, 脚本标识, StringComparison.OrdinalIgnoreCase))
                    {
                        匹配路径 = 路径;
                        匹配名称 = 名称;
                        break;
                    }
                }
            }

            if (匹配路径 == null)
            {
                foreach (var (路径, 名称) in 所有脚本)
                {
                    if (名称.Contains(脚本标识, StringComparison.OrdinalIgnoreCase))
                    {
                        匹配路径 = 路径;
                        匹配名称 = 名称;
                        break;
                    }
                }
            }

            if (匹配路径 == null || !File.Exists(匹配路径))
                return $"未找到匹配的脚本: {脚本标识}。请检查脚本名称是否正确，或用 search_scripts 搜索已保存的脚本。";

            try
            {
                string 内容 = File.ReadAllText(匹配路径);
                内容 = 内容.Replace("\r\n", "\n");
                int firstAtIndex = 内容.IndexOf("@");
                string 头部 = firstAtIndex >= 0 ? 内容.Substring(0, firstAtIndex).Trim() : 内容.Trim();
                string 节点部分 = firstAtIndex >= 0 ? 内容.Substring(firstAtIndex + 1) : "";

                var sb = new StringBuilder();

                if (!string.Equals(匹配名称, 脚本标识, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(匹配路径, 脚本标识, StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"(模糊匹配: {脚本标识} → {匹配名称})");
                    sb.AppendLine();
                }

                var 头部分解 = Regex.Match(头部, @"^进程名\[([^\]]*)\]窗口标题\[([^\]]*)\](.*)$");
                if (头部分解.Success)
                {
                    sb.AppendLine($"脚本名: {匹配名称}");
                    sb.AppendLine($"目标进程: {头部分解.Groups[1].Value}");
                    sb.AppendLine($"目标窗口: {头部分解.Groups[2].Value}");
                    string 备注 = 头部分解.Groups[3].Value.Trim();
                    sb.AppendLine($"备注: {(string.IsNullOrEmpty(备注) ? "(无)" : 备注)}");
                }
                else
                {
                    sb.AppendLine($"脚本名: {匹配名称}");
                    sb.AppendLine($"头部: {头部}");
                }
                sb.AppendLine();
                sb.AppendLine("--- 节点列表 ---");
                sb.AppendLine();

                if (string.IsNullOrWhiteSpace(节点部分))
                {
                    sb.AppendLine("(该脚本暂无节点)");
                    return sb.ToString();
                }

                string[] 节点段 = 节点部分.Split(new[] { "\n@" }, StringSplitOptions.RemoveEmptyEntries);
                int 节点序号 = 0;

                foreach (var 段 in 节点段)
                {
                    string clean = 段.Trim();
                    if (string.IsNullOrEmpty(clean))
                        continue;

                    var labelMatch = Regex.Match(clean, @"^([^:\n]+):");
                    if (!labelMatch.Success)
                        continue;

                    string 标签名 = labelMatch.Groups[1].Value.Trim();
                    sb.AppendLine($"节点 {节点序号}: [{标签名}]");

                    string 字段部分 = clean.Substring(labelMatch.Length).Trim();
                    var fieldMatches = Regex.Matches(字段部分, @"([^,\[\n\r]+)\[([^\]]*)\]");

                    foreach (Match fm in fieldMatches)
                    {
                        string 字段名 = fm.Groups[1].Value.Trim();
                        string 字段值 = fm.Groups[2].Value;
                        if (string.IsNullOrEmpty(字段名))
                            continue;

                        string 格式化值 = 格式化字段值(字段名, 字段值);
                        sb.AppendLine($"  {字段名}: {格式化值}");
                    }

                    sb.AppendLine();
                    节点序号++;
                }

                sb.AppendLine($"--- 共 {节点序号} 个节点 ---");

                string result = sb.ToString();
                if (result.Length > 18000)
                    result = result.Substring(0, 18000) + $"\n\n... (输出过长已截断)";
                return result;
            }
            catch (Exception ex)
            {
                return $"展开脚本失败: {ex.Message}";
            }
        }

        private static string 搜索脚本(Dictionary<string, object> 参数, List<MCPToolDefinition> 当前工具列表)
        {
            if (!参数.TryGetValue("关键词", out var kwObj) || string.IsNullOrEmpty(kwObj?.ToString()))
                return "错误: 缺少参数 '关键词'";

            string 关键词 = kwObj.ToString();

            string 搜索范围 = "名称备注";
            if (参数.TryGetValue("搜索范围", out var scopeObj) && scopeObj != null)
                搜索范围 = scopeObj.ToString();

            int 最大结果数 = 5;
            if (参数.TryGetValue("最大结果数", out var maxObj) && maxObj != null)
            {
                if (maxObj is int i)
                    最大结果数 = Math.Max(1, i);
                else if (int.TryParse(maxObj.ToString(), out var parsed))
                    最大结果数 = Math.Max(1, parsed);
            }

            bool 搜索完整内容 = string.Equals(搜索范围, "完整内容", StringComparison.OrdinalIgnoreCase);
            var 结果 = new List<string>();
            int 总数 = 0;

            var 所有脚本 = 收集所有脚本信息();
            foreach (var (路径, 名称) in 所有脚本)
            {
                if (!File.Exists(路径))
                    continue;

                bool 匹配 = 名称.Contains(关键词, StringComparison.OrdinalIgnoreCase);

                if (!匹配)
                {
                    try
                    {
                        string 第一行 = File.ReadLines(路径).FirstOrDefault() ?? "";
                        var 备注Match = Regex.Match(第一行, @"^进程名\[[^\]]*\]窗口标题\[[^\]]*\](.*)$");
                        if (备注Match.Success)
                        {
                            string 备注 = 备注Match.Groups[1].Value;
                            int atIndex = 备注.IndexOf('@');
                            if (atIndex >= 0)
                                备注 = 备注.Substring(0, atIndex).Trim();
                            if (备注.Contains(关键词, StringComparison.OrdinalIgnoreCase))
                                匹配 = true;
                        }
                    }
                    catch { }
                }

                if (!匹配 && 搜索完整内容)
                {
                    try
                    {
                        string 全文 = File.ReadAllText(路径);
                        if (全文.Contains(关键词, StringComparison.OrdinalIgnoreCase))
                            匹配 = true;
                    }
                    catch { }
                }

                if (匹配)
                {
                    总数++;
                    if (结果.Count < 最大结果数)
                    {
                        string 备注摘要 = "(无备注)";
                        try
                        {
                            string 第一行 = File.ReadLines(路径).FirstOrDefault() ?? "";
                            var 备注Match = Regex.Match(第一行, @"^进程名\[[^\]]*\]窗口标题\[[^\]]*\](.*)$");
                            if (备注Match.Success && !string.IsNullOrWhiteSpace(备注Match.Groups[1].Value))
                            {
                                备注摘要 = 备注Match.Groups[1].Value.Trim();
                                int atIndex = 备注摘要.IndexOf('@');
                                if (atIndex >= 0)
                                    备注摘要 = 备注摘要.Substring(0, atIndex).Trim();
                                if (备注摘要.Length > 40)
                                    备注摘要 = 备注摘要.Substring(0, 37) + "...";
                            }
                        }
                        catch { }

                        结果.Add($"- {名称}: {备注摘要}");
                    }
                }
            }

            if (总数 == 0)
                return $"在已保存的脚本中未找到匹配「{关键词}」的结果。";

            var sb = new StringBuilder();
            if (搜索完整内容)
                sb.AppendLine($"搜索「{关键词}」(完整内容，共 {总数} 条):");
            else
                sb.AppendLine($"搜索「{关键词}」(共 {总数} 条):");
            sb.AppendLine();

            foreach (var r in 结果)
                sb.AppendLine(r);

            if (总数 > 最大结果数)
                sb.AppendLine($"\n... 还有 {总数 - 最大结果数} 条匹配结果未显示，请缩小搜索范围");

            return sb.ToString();
        }

        private struct 脚本条目
        {
            public string 路径;
            public string 名称;
            public void Deconstruct(out string out路径, out string out名称)
            {
                out路径 = this.路径;
                out名称 = this.名称;
            }
        }

        private static List<脚本条目> 收集所有脚本信息()
        {
            var result = new List<脚本条目>();
            try
            {
                string 用户数据路径 = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "淼喵妙脚本DIY", "userdata.json");

                if (!File.Exists(用户数据路径))
                    return result;

                string json = File.ReadAllText(用户数据路径);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("TaskCategories", out var 分类列表))
                    return result;

                收集分类脚本路径(result, 分类列表);
            }
            catch { }
            return result;
        }

        private static void 收集分类脚本路径(List<脚本条目> result, System.Text.Json.JsonElement 分类列表)
        {
            if (分类列表.ValueKind != System.Text.Json.JsonValueKind.Array)
                return;

            foreach (var cat in 分类列表.EnumerateArray())
            {
                if (cat.TryGetProperty("TaskPaths", out var paths) && paths.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var path in paths.EnumerateArray())
                    {
                        string p = path.GetString();
                        if (!string.IsNullOrEmpty(p) && !result.Any(r => string.Equals(r.路径, p, StringComparison.OrdinalIgnoreCase)))
                            result.Add(new 脚本条目 { 路径 = p, 名称 = Path.GetFileNameWithoutExtension(p) });
                    }
                }

                if (cat.TryGetProperty("SubCategories", out var subs))
                    收集分类脚本路径(result, subs);
            }
        }

        private static string 获取脚本分类路径(string 脚本路径)
        {
            try
            {
                string 用户数据路径 = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "淼喵妙脚本DIY", "userdata.json");

                if (!File.Exists(用户数据路径))
                    return "未知";

                string json = File.ReadAllText(用户数据路径);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("TaskCategories", out var 分类列表_))
                    return "未知";

                return 查找分类路径(分类列表_, 脚本路径, "");
            }
            catch { return "未知"; }
        }

        private static string 查找分类路径(System.Text.Json.JsonElement 分类列表, string 脚本路径, string 父路径)
        {
            if (分类列表.ValueKind != System.Text.Json.JsonValueKind.Array)
                return 父路径;

            foreach (var cat in 分类列表.EnumerateArray())
            {
                string catName = cat.TryGetProperty("CategoryName", out var cn) ? cn.GetString() : "未知";
                string 当前路径 = string.IsNullOrEmpty(父路径) ? catName : $"{父路径} > {catName}";

                if (cat.TryGetProperty("TaskPaths", out var paths) && paths.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var p in paths.EnumerateArray())
                    {
                        if (string.Equals(p.GetString(), 脚本路径, StringComparison.OrdinalIgnoreCase))
                            return 当前路径;
                    }
                }

                if (cat.TryGetProperty("SubCategories", out var subs))
                {
                    string subResult = 查找分类路径(subs, 脚本路径, 当前路径);
                    if (subResult != 当前路径)
                        return subResult;
                }
            }

            return 父路径;
        }

        private static string 在工具组中查找脚本(string 脚本标识, List<MCPToolDefinition> 当前工具列表)
        {
            if (File.Exists(脚本标识))
            {
                string fullPath = Path.GetFullPath(脚本标识);
                if (当前工具列表 != null && 当前工具列表.Any(t => string.Equals(t.脚本路径, fullPath, StringComparison.OrdinalIgnoreCase)))
                    return fullPath;
                return null;
            }

            if (!脚本标识.EndsWith(".script", StringComparison.OrdinalIgnoreCase))
            {
                string 带扩展名 = 脚本标识 + ".script";
                if (File.Exists(带扩展名))
                {
                    string fullPath = Path.GetFullPath(带扩展名);
                    if (当前工具列表 != null && 当前工具列表.Any(t => string.Equals(t.脚本路径, fullPath, StringComparison.OrdinalIgnoreCase)))
                        return fullPath;
                    return null;
                }
            }

            if (当前工具列表 == null || 当前工具列表.Count == 0)
                return null;

            string 搜索名 = 脚本标识;
            if (搜索名.EndsWith(".script", StringComparison.OrdinalIgnoreCase))
                搜索名 = 搜索名.Substring(0, 搜索名.Length - ".script".Length);

            foreach (var tool in 当前工具列表)
            {
                if (string.IsNullOrEmpty(tool.脚本路径))
                    continue;
                string 文件名 = Path.GetFileNameWithoutExtension(tool.脚本路径);
                if (string.Equals(文件名, 搜索名, StringComparison.OrdinalIgnoreCase) && File.Exists(tool.脚本路径))
                    return tool.脚本路径;
            }

            return null;
        }

        private static readonly string 用户数据目录 = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "淼喵妙脚本DIY");
        private static readonly string 用户数据路径 = Path.Combine(用户数据目录, "userdata.json");

        private static string 列出节点类型()
        {
            var sb = new StringBuilder();
            sb.AppendLine("可用节点类型（按分类组织）：");
            sb.AppendLine();

            var categories = new Dictionary<string, List<string>>
            {
                ["鼠标输入"] = new List<string> { "点击图片", "点击串联图片", "点击文字", "鼠标单击", "鼠标双击", "鼠标拖拽", "鼠标滚轮", "鼠标悬停", "鼠标长按" },
                ["按键输入"] = new List<string> { "输入文本", "单击按键", "快捷键", "长按按键" },
                ["应用与窗口"] = new List<string> { "打开应用", "绑定运行应用", "调整窗口尺寸" },
                ["判断与读取"] = new List<string> { "区域截图", "图片位置", "感应屏幕", "识别文字", "文字位置", "识别方框", "识别进度条", "智能识别目标", "读取标题", "读取Excel到全局" },
                ["指令与流程控制"] = new List<string> { "停止任务", "空节点", "显示变量", "提示弹窗", "执行任务", "插队", "计算函数", "PowerShell指令", "远程指令", "从列表中按序读取" },
                ["高级拓展"] = new List<string> { "HTTP请求", "CSharp脚本", "外部脚本" }
            };

            foreach (var kv in categories)
            {
                sb.AppendLine($"【{kv.Key}】");
                foreach (var name in kv.Value)
                {
                    var type = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
                        .FirstOrDefault(t => t.Name == name && typeof(控制节点).IsAssignableFrom(t) && !t.IsAbstract);

                    if (type != null)
                    {
                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.CanWrite && p.DeclaringType == type)
                            .Select(p => $"{p.Name}({p.PropertyType.Name})");
                        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                            .Where(f => f.DeclaringType == type)
                            .Select(f => $"{f.Name}({f.FieldType.Name})");
                        var allFields = props.Concat(fields).ToList();

                        sb.Append($"  - {name}");
                        if (allFields.Count > 0)
                            sb.Append($" | 字段: {string.Join(", ", allFields)}");
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.AppendLine($"  - {name}");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("基类通用字段（所有节点均有）：");
            sb.AppendLine("  - 节点名字(string) : 节点显示名称");
            sb.AppendLine("  - 节点备注(string) : 节点备注说明");
            sb.AppendLine("  - 成功后等待(int) : 成功执行后的等待时间（毫秒）");
            sb.AppendLine("  - 失败后等待(int) : 失败后的等待时间（毫秒）");
            sb.AppendLine("  - 成功后跳转(控制节点) : 成功后的跳转目标节点引用");
            sb.AppendLine("  - 失败后跳转(控制节点) : 失败后的跳转目标节点引用");

            return sb.ToString().TrimEnd();
        }

        private static string 获取节点创建规则()
        {
            var sb = new StringBuilder();
            sb.AppendLine("节点创建规则表");
            sb.AppendLine("================");
            sb.AppendLine();
            sb.AppendLine("一、鼠标操作优先级(高→低): 点击图片(阈值0.85)→点击串联图片→点击文字→鼠标单击→双击→拖拽→滚轮→悬停→长按");
            sb.AppendLine("  等待建议: 点击类300-500ms, 拖拽/悬停500-1000ms, 滚轮200-500ms, 长按300-500ms");
            sb.AppendLine();
            sb.AppendLine("二、键盘: 输入文本/单击按键等待100-300ms, 快捷键等待200-500ms");
            sb.AppendLine();
            sb.AppendLine("三、应用窗口: 打开应用等待1-3s, 绑定窗口等待500-1000ms, 调整窗口等待300-500ms");
            sb.AppendLine();
            sb.AppendLine("四、等待策略: 操作类100-500ms, 加载类500-2000ms, 识别类100-300ms, 失败等待默认等于成功等待, 不单设等待节点");
            sb.AppendLine();
            sb.AppendLine("五、循环构建: 成功后跳转→循环首节点, 失败后跳转→退出节点, 备注标注「循环开始」「循环结束」");
            sb.AppendLine();
            sb.AppendLine("六、流程控制节点: 空节点/停止任务/显示变量/提示弹窗/计算函数/执行任务/插队/PowerShell指令/远程指令/HTTP请求/CSharp脚本");
            sb.AppendLine();
            sb.AppendLine("七、命名: 动宾结构(如'点击签到按钮'), 备注补充上下文, 避免同名");

            return sb.ToString().TrimEnd();
        }

        private static string 创建工作脚本(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("进程名", out var pn) || string.IsNullOrEmpty(pn?.ToString()))
                return "错误: 缺少参数 '进程名'";
            if (!参数.TryGetValue("窗口标题", out var wt) || string.IsNullOrEmpty(wt?.ToString()))
                return "错误: 缺少参数 '窗口标题'";

            创建脚本.创建工作脚本(pn.ToString(), wt.ToString());
            return $"工作脚本已创建: 进程名={pn}, 窗口标题={wt}";
        }

        private static string 添加节点(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("节点类型", out var typeObj) || string.IsNullOrEmpty(typeObj?.ToString()))
                return "错误: 缺少参数 '节点类型'";

            string 节点类型 = typeObj.ToString();
            string 名字 = 参数.TryGetValue("节点名字", out var nn) ? nn?.ToString() ?? "" : "";
            string 备注 = 参数.TryGetValue("节点备注", out var nr) ? nr?.ToString() ?? "" : "";
            int 成功等待 = 500;
            if (参数.TryGetValue("成功后等待", out var sw) && sw != null)
                int.TryParse(sw.ToString(), out 成功等待);
            int 失败等待 = 成功等待;
            if (参数.TryGetValue("失败后等待", out var fw) && fw != null)
                int.TryParse(fw.ToString(), out 失败等待);

            var node = 创建脚本.创建节点实例(节点类型, 成功等待, 失败等待);
            if (node == null)
                return $"错误: 找不到节点类型「{节点类型}」，请使用 list_node_types 查看可用类型";

            if (!string.IsNullOrEmpty(名字))
                node.节点名字 = 名字;
            if (!string.IsNullOrEmpty(备注))
                node.节点备注 = 备注;

            foreach (var kv in 参数)
            {
                if (kv.Key == "节点类型" || kv.Key == "节点名字" || kv.Key == "节点备注" ||
                    kv.Key == "成功后等待" || kv.Key == "失败后等待")
                    continue;

                var prop = node.GetType().GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite && kv.Value != null)
                {
                    try
                    {
                        object val = kv.Value;
                        if (prop.PropertyType != val.GetType())
                        {
                            if (prop.PropertyType == typeof(string))
                                val = val.ToString();
                            else
                                val = Convert.ChangeType(val, prop.PropertyType);
                        }
                        prop.SetValue(node, val);
                    }
                    catch { }
                }
            }

            int index = 创建脚本.追加节点(node);
            return $"节点「{节点类型}」({(string.IsNullOrEmpty(名字) ? "未命名" : 名字)}) 已追加，索引={index}";
        }

        private static string 移除节点(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("索引", out var idxObj) || idxObj == null)
                return "错误: 缺少参数 '索引'";
            if (!int.TryParse(idxObj.ToString(), out int 索引) || 索引 < 0)
                return "错误: 索引必须是非负整数";

            int count = 创建脚本.获取节点列表().Count;
            if (索引 >= count)
                return $"错误: 索引 {索引} 超出范围（共 {count} 个节点）";

            var node = 创建脚本.获取节点(索引);
            string name = node?.节点名字 ?? "未命名";
            创建脚本.移除节点(索引);
            return $"节点「{name}」(索引={索引}) 已移除，其他节点的跳转引用已自动修正";
        }

        private static string 修改节点(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("索引", out var idxObj) || idxObj == null)
                return "错误: 缺少参数 '索引'";
            if (!int.TryParse(idxObj.ToString(), out int 索引) || 索引 < 0)
                return "错误: 索引必须是非负整数";
            if (!参数.TryGetValue("参数字典", out var dictObj) || dictObj == null)
                return "错误: 缺少参数 '参数字典'";

            int count = 创建脚本.获取节点列表().Count;
            if (索引 >= count)
                return $"错误: 索引 {索引} 超出范围（共 {count} 个节点）";

            Dictionary<string, object> 修改字典;
            if (dictObj is Dictionary<string, object> d)
                修改字典 = d;
            else if (dictObj is JsonElement je && je.ValueKind == JsonValueKind.Object)
                修改字典 = JsonSerializer.Deserialize<Dictionary<string, object>>(je.GetRawText());
            else
                return "错误: '参数字典' 必须是JSON对象";

            bool ok = 创建脚本.修改节点(索引, 修改字典);
            return ok ? $"节点索引={索引} 修改成功，修改字段: {string.Join(", ", 修改字典.Keys)}" : "修改失败";
        }

        private static string 执行节点(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("索引", out var idxObj) || idxObj == null)
                return "错误: 缺少参数 '索引'";
            if (!int.TryParse(idxObj.ToString(), out int 索引) || 索引 < 0)
                return "错误: 索引必须是非负整数";

            var node = 创建脚本.获取节点(索引);
            if (node == null)
                return $"错误: 索引 {索引} 超出范围";

            try
            {
                任务控制管理器.实例.重置取消();
                var 全局 = new Dictionary<string, object>();
                var result = node.执行(IntPtr.Zero, 全局);
                bool success = result != null && !(result is 停止任务);
                return success
                    ? $"节点「{node.节点名字}」(索引={索引}) 执行成功"
                    : $"节点「{node.节点名字}」(索引={索引}) 执行失败（跳转到失败路径）";
            }
            catch (Exception ex)
            {
                return $"节点「{node.节点名字}」(索引={索引}) 执行异常: {ex.Message}";
            }
        }

        private static string 保存脚本(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("脚本名", out var nameObj) || string.IsNullOrEmpty(nameObj?.ToString()))
                return "错误: 缺少参数 '脚本名'";

            string 脚本名 = nameObj.ToString();
            string 备注 = "";
            if (参数.TryGetValue("备注", out var remarkObj) && remarkObj != null)
                备注 = remarkObj.ToString();
            string 分类名 = "未分类";
            if (参数.TryGetValue("分类名", out var catObj) && !string.IsNullOrEmpty(catObj?.ToString()))
                分类名 = catObj.ToString();

            return 创建脚本.保存脚本(脚本名, 备注, 分类名);
        }

        private static string 列出所有脚本()
        {
            var sb = new StringBuilder();
            sb.AppendLine("已保存的脚本列表：");
            sb.AppendLine();

            var userData = 读取用户数据();
            if (userData.ValueKind == JsonValueKind.Undefined)
                return "无法读取用户数据文件";

            var 分类列表 = userData.TryGetProperty("TaskCategories", out var cats) ? cats : default;
            if (分类列表.ValueKind != JsonValueKind.Array)
            {
                sb.AppendLine("（无分类数据）");
                return sb.ToString();
            }

            int total = 0;
            foreach (var cat in 分类列表.EnumerateArray())
            {
                列出分类脚本递归(sb, cat, 0, ref total);
            }

            sb.AppendLine($"共 {total} 个脚本");
            return sb.ToString().TrimEnd();
        }

        private static void 列出分类脚本递归(StringBuilder sb, JsonElement cat, int 层级, ref int total)
        {
            string catName = cat.TryGetProperty("CategoryName", out var cn) ? cn.GetString() : "未知";
            var paths = cat.TryGetProperty("TaskPaths", out var tp) ? tp : default;
            int count = paths.ValueKind == JsonValueKind.Array ? paths.GetArrayLength() : 0;

            string 缩进 = new string(' ', 层级 * 2);
            if (count > 0)
            {
                sb.AppendLine($"{缩进}【{catName}】({count}个脚本)");
                foreach (var path in paths.EnumerateArray())
                {
                    string p = path.GetString();
                    string name = Path.GetFileNameWithoutExtension(p ?? "");
                    if (File.Exists(p))
                    {
                        try
                        {
                            string firstLine = File.ReadLines(p).FirstOrDefault() ?? "";
                            var 备注Match = Regex.Match(firstLine, @"^进程名\[[^\]]*\]窗口标题\[[^\]]*\](.*)$");
                            string 备注 = "";
                            if (备注Match.Success)
                            {
                                备注 = 备注Match.Groups[1].Value;
                                int atIndex = 备注.IndexOf('@');
                                if (atIndex >= 0)
                                    备注 = 备注.Substring(0, atIndex);
                                备注 = 备注.Trim();
                            }
                            if (!string.IsNullOrEmpty(备注))
                                sb.AppendLine($"  {缩进}- {name}: {备注}");
                            else
                                sb.AppendLine($"  {缩进}- {name}");
                        }
                        catch
                        {
                            sb.AppendLine($"  {缩进}- {name}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"  {缩进}- {name} ⚠️(文件不存在)");
                    }
                    total++;
                }
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"{缩进}【{catName}】（空分类）");
                sb.AppendLine();
            }

            var 子分类 = cat.TryGetProperty("SubCategories", out var subs) ? subs : default;
            if (子分类.ValueKind == JsonValueKind.Array)
            {
                foreach (var sub in 子分类.EnumerateArray())
                {
                    列出分类脚本递归(sb, sub, 层级 + 1, ref total);
                }
            }
        }

        private static string 分类脚本(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("脚本名或路径", out var idObj) || string.IsNullOrEmpty(idObj?.ToString()))
                return "错误: 缺少参数 '脚本名或路径'";
            if (!参数.TryGetValue("目标分类", out var catObj) || string.IsNullOrEmpty(catObj?.ToString()))
                return "错误: 缺少参数 '目标分类'";

            string 脚本标识 = idObj.ToString();
            string 目标分类 = catObj.ToString();

            string 脚本路径 = 解析脚本路径(脚本标识);
            if (string.IsNullOrEmpty(脚本路径) || !File.Exists(脚本路径))
                return $"错误: 找不到脚本 '{脚本标识}'";

            var userData = 读取用户数据();
            if (userData.ValueKind == JsonValueKind.Undefined) return "无法读取用户数据文件";

            string json = userData.GetRawText();

            using var parsed = JsonDocument.Parse(json);
            var root = parsed.RootElement;

            if (!root.TryGetProperty("TaskCategories", out var cats) || cats.ValueKind != JsonValueKind.Array)
                return "错误: 用户数据中没有分类信息";

            var categories = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(cats.GetRawText());
            if (categories == null) return "无法解析分类数据";

            从分类递归移除脚本(categories, 脚本路径);

            var targetCat = 查找分类递归(categories, 目标分类);
            if (targetCat == null)
            {
                targetCat = new Dictionary<string, object>
                {
                    ["CategoryName"] = 目标分类,
                    ["CategoryColor"] = "#FF808080",
                    ["AI规则"] = "",
                    ["TaskPaths"] = new List<string> { 脚本路径 },
                    ["SubCategories"] = new List<object>()
                };
                categories.Add(targetCat);
            }
            else
            {
                if (targetCat.TryGetValue("TaskPaths", out var existingTp) && existingTp is JsonElement etpEl)
                {
                    var paths = JsonSerializer.Deserialize<List<string>>(etpEl.GetRawText()) ?? new List<string>();
                    if (!paths.Contains(脚本路径))
                        paths.Add(脚本路径);
                    targetCat["TaskPaths"] = paths;
                }
                else
                {
                    targetCat["TaskPaths"] = new List<string> { 脚本路径 };
                }
            }

            var rootDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (rootDict != null)
            {
                rootDict["TaskCategories"] = categories;
                string newJson = JsonSerializer.Serialize(rootDict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(用户数据路径, newJson);
            }

            return $"脚本 '{Path.GetFileNameWithoutExtension(脚本路径)}' 已分类到「{目标分类}」";
        }

        private static void 从分类递归移除脚本(List<Dictionary<string, object>> categories, string 脚本路径)
        {
            foreach (var cat in categories)
            {
                if (cat.TryGetValue("TaskPaths", out var tp) && tp is JsonElement tpEl && tpEl.ValueKind == JsonValueKind.Array)
                {
                    var paths = JsonSerializer.Deserialize<List<string>>(tpEl.GetRawText());
                    if (paths != null && paths.Contains(脚本路径))
                    {
                        paths.Remove(脚本路径);
                        cat["TaskPaths"] = paths;
                    }
                }

                if (cat.TryGetValue("SubCategories", out var subs))
                {
                    List<Dictionary<string, object>> subList = null;
                    if (subs is JsonElement subEl && subEl.ValueKind == JsonValueKind.Array)
                        subList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(subEl.GetRawText());
                    else if (subs is List<object> objList)
                        subList = objList.Select(o => o is JsonElement je
                            ? JsonSerializer.Deserialize<Dictionary<string, object>>(je.GetRawText())
                            : o as Dictionary<string, object>).Where(d => d != null).ToList();

                    if (subList != null)
                    {
                        从分类递归移除脚本(subList, 脚本路径);
                    }
                }
            }
        }

        private static string 编辑分类规则(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("分类名", out var catObj) || string.IsNullOrEmpty(catObj?.ToString()))
                return "错误: 缺少参数 '分类名'";
            if (!参数.TryGetValue("AI规则", out var ruleObj))
                return "错误: 缺少参数 'AI规则'";

            string 分类名 = catObj.ToString();
            string ai规则 = ruleObj?.ToString() ?? "";

            var userData = 读取用户数据();
            if (userData.ValueKind == JsonValueKind.Undefined) return "无法读取用户数据文件";

            string json = userData.GetRawText();
            using var parsed = JsonDocument.Parse(json);
            var root = parsed.RootElement;

            if (!root.TryGetProperty("TaskCategories", out var cats) || cats.ValueKind != JsonValueKind.Array)
                return "错误: 用户数据中没有分类信息";

            var categories = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(cats.GetRawText());
            if (categories == null) return "无法解析分类数据";

            bool found = false;
            var targetCat = 查找分类递归(categories, 分类名);
            if (targetCat != null)
            {
                targetCat["AI规则"] = ai规则;
                found = true;
            }

            if (!found)
                return $"错误: 找不到分类「{分类名}」";

            var rootDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (rootDict != null)
            {
                rootDict["TaskCategories"] = categories;
                string newJson = JsonSerializer.Serialize(rootDict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(用户数据路径, newJson);
            }

            return $"分类「{分类名}」的AI规则已更新为: {ai规则}";
        }

        private static string 创建分类(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("分类名", out var nameObj) || string.IsNullOrEmpty(nameObj?.ToString()))
                return "错误: 缺少参数 '分类名'";

            string 分类名 = nameObj.ToString();
            string 父分类 = "";
            if (参数.TryGetValue("父分类", out var parentObj) && !string.IsNullOrEmpty(parentObj?.ToString()))
                父分类 = parentObj.ToString();

            var userData = 读取用户数据();
            if (userData.ValueKind == JsonValueKind.Undefined) return "无法读取用户数据文件";

            string json = userData.GetRawText();
            using var parsed = JsonDocument.Parse(json);
            var root = parsed.RootElement;

            if (!root.TryGetProperty("TaskCategories", out var cats) || cats.ValueKind != JsonValueKind.Array)
                return "错误: 用户数据中没有分类信息";

            var categories = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(cats.GetRawText());
            if (categories == null) return "无法解析分类数据";

            var newCat = new Dictionary<string, object>
            {
                ["CategoryName"] = 分类名,
                ["CategoryColor"] = "#FF808080",
                ["AI规则"] = "",
                ["TaskPaths"] = new List<string>(),
                ["SubCategories"] = new List<object>()
            };

            if (!string.IsNullOrEmpty(父分类))
            {
                var parent = 查找分类递归(categories, 父分类);
                if (parent == null)
                    return $"错误: 找不到父分类「{父分类}」，将创建在根目录";

                if (!parent.ContainsKey("SubCategories"))
                    parent["SubCategories"] = new List<object>();
                if (parent["SubCategories"] is JsonElement subEl && subEl.ValueKind == JsonValueKind.Array)
                {
                    var subs = JsonSerializer.Deserialize<List<object>>(subEl.GetRawText()) ?? new List<object>();
                    subs.Add(newCat);
                    parent["SubCategories"] = subs;
                }
                else if (parent["SubCategories"] is List<object> subList)
                {
                    subList.Add(newCat);
                }
            }
            else
            {
                categories.Add(newCat);
            }

            var rootDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            if (rootDict != null)
            {
                rootDict["TaskCategories"] = categories;
                string newJson = JsonSerializer.Serialize(rootDict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(用户数据路径, newJson);
            }

            记录("create_category", $"创建分类: {分类名}, 父分类: {(string.IsNullOrEmpty(父分类) ? "根" : 父分类)}");
            return string.IsNullOrEmpty(父分类)
                ? $"分类「{分类名}」已创建在根目录"
                : $"分类「{分类名}」已创建在「{父分类}」下";
        }

        private static Dictionary<string, object> 查找分类递归(List<Dictionary<string, object>> categories, string targetName)
        {
            foreach (var cat in categories)
            {
                if (cat.TryGetValue("CategoryName", out var cn) && cn?.ToString() == targetName)
                    return cat;

                if (cat.TryGetValue("SubCategories", out var subs))
                {
                    List<Dictionary<string, object>> subList = null;
                    if (subs is JsonElement subEl && subEl.ValueKind == JsonValueKind.Array)
                        subList = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(subEl.GetRawText());
                    else if (subs is List<object> objList)
                        subList = objList.Select(o => o is JsonElement je
                            ? JsonSerializer.Deserialize<Dictionary<string, object>>(je.GetRawText())
                            : o as Dictionary<string, object>).Where(d => d != null).ToList();

                    if (subList != null)
                    {
                        var found = 查找分类递归(subList, targetName);
                        if (found != null) return found;
                    }
                }
            }
            return null;
        }

        private static JsonElement 读取用户数据()
        {
            try
            {
                if (!File.Exists(用户数据路径)) return default;
                string json = File.ReadAllText(用户数据路径);
                return JsonDocument.Parse(json).RootElement;
            }
            catch
            {
                return default;
            }
        }

        private static string 解析脚本路径(string 脚本标识)
        {
            if (File.Exists(脚本标识))
                return Path.GetFullPath(脚本标识);

            if (!脚本标识.EndsWith(".script", StringComparison.OrdinalIgnoreCase))
            {
                string withExt = 脚本标识 + ".script";
                if (File.Exists(withExt))
                    return Path.GetFullPath(withExt);
            }

            var userData = 读取用户数据();
            if (userData.ValueKind == JsonValueKind.Undefined) return null;

            string 搜索名 = 脚本标识;
            if (搜索名.EndsWith(".script", StringComparison.OrdinalIgnoreCase))
                搜索名 = 搜索名.Substring(0, 搜索名.Length - ".script".Length);

            if (userData.TryGetProperty("TaskCategories", out var cats) && cats.ValueKind == JsonValueKind.Array)
            {
                var result = 解析脚本路径递归(cats, 搜索名);
                if (result != null) return result;
            }

            return null;
        }

        private static string 解析脚本路径递归(JsonElement categories, string 搜索名)
        {
            foreach (var cat in categories.EnumerateArray())
            {
                if (cat.TryGetProperty("TaskPaths", out var paths) && paths.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in paths.EnumerateArray())
                    {
                        string path = p.GetString();
                        if (path != null && File.Exists(path) &&
                            string.Equals(Path.GetFileNameWithoutExtension(path), 搜索名, StringComparison.OrdinalIgnoreCase))
                            return path;
                    }
                }

                if (cat.TryGetProperty("SubCategories", out var subs) && subs.ValueKind == JsonValueKind.Array)
                {
                    var result = 解析脚本路径递归(subs, 搜索名);
                    if (result != null) return result;
                }
            }
            return null;
        }

        private static void 记录(string 事件名, string 详情)
        {
            try
            {
                if (!Directory.Exists(日志目录))
                    Directory.CreateDirectory(日志目录);

                string 文件名 = $"{DateTime.Now:yyyyMMdd_HHmmss}_{事件名}.json";
                string filePath = Path.Combine(日志目录, 文件名);

                var entry = new Dictionary<string, object>
                {
                    ["时间戳"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    ["事件名"] = 事件名,
                    ["详情"] = 详情
                };

                string json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
            catch { }
        }

        private static string 查找窗口框(Dictionary<string, object> 参数)
        {
            if (!参数.TryGetValue("窗口标题", out var 标题Obj) || string.IsNullOrEmpty(标题Obj?.ToString()))
                return "错误: 缺少参数 '窗口标题'";

            string 窗口标题 = 标题Obj.ToString();
            var hWnd = 淼喵妙神奇工具库.感知库.窗口处理器.查找窗口(窗口标题);

            if (hWnd == IntPtr.Zero)
                return "{\"found\": false}";

            var rect = 淼喵妙神奇工具库.感知库.窗口处理器.获取窗口框(hWnd);
            if (rect == System.Drawing.Rectangle.Empty)
                return "{\"found\": false}";

            string 完整标题 = 淼喵妙神奇工具库.感知库.窗口处理器.获取窗口标题(hWnd);

            return $"{{\"found\": true, \"title\": \"{完整标题.Replace("\"", "\\\"")}\", \"rect\": {{\"x\": {rect.X}, \"y\": {rect.Y}, \"width\": {rect.Width}, \"height\": {rect.Height}}}}}";
        }

        private static void 记录攻略(string 搜索关键词, string 搜索结果)
        {
            try
            {
                string 攻略目录 = Path.Combine(日志目录, "strategies");
                if (!Directory.Exists(攻略目录))
                    Directory.CreateDirectory(攻略目录);

                string 文件名 = $"{DateTime.Now:yyyyMMdd_HHmmss}_攻略_{搜索关键词}.txt";
                string filePath = Path.Combine(攻略目录, 文件名);

                string sanitized = 搜索结果?.Length > 50000 ? 搜索结果.Substring(0, 50000) + "\n... (已截断)" : 搜索结果;
                File.WriteAllText(filePath, sanitized);
            }
            catch { }
        }
    }
}
