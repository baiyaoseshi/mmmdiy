using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 选定执行 : 控制节点
    {
        private class 对象型UserData
        {
            public List<对象型TaskCategory> TaskCategories { get; set; }
        }

        private class 对象型TaskCategory
        {
            public string CategoryName { get; set; }
            public List<string> TaskPaths { get; set; }
            public List<对象型TaskCategory> SubCategories { get; set; }
        }

        public string 选项列表 = "";

        选定执行() : base() { }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (string.IsNullOrEmpty(选项列表))
            {
                return false;
            }

            try
            {
                var 条目列表 = 选项列表.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var userdata路径 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "淼喵妙脚本DIY", "userdata.json");

                if (!File.Exists(userdata路径))
                {
                    return false;
                }

                var userdata = JsonSerializer.Deserialize<对象型UserData>(File.ReadAllText(userdata路径));
                if (userdata?.TaskCategories == null)
                {
                    return false;
                }

                var 脚本字典 = new Dictionary<string, string>();
                foreach (var 分类 in userdata.TaskCategories)
                {
                    递归收集脚本(分类, 脚本字典);
                }

                var 选项显示列表 = new List<string>();
                var 显示到键映射 = new List<string>();

                foreach (var 条目 in 条目列表)
                {
                    var 部分 = 条目.Split(':', 2);
                    if (部分.Length < 2) continue;
                    var 显示文本 = $"[{部分[0]}] {部分[1]}";
                    选项显示列表.Add(显示文本);
                    显示到键映射.Add(条目);
                }

                if (选项显示列表.Count == 0)
                {
                    return false;
                }

                var 选中 = 通知工具.选项弹窗("请选择要执行的脚本:", 选项显示列表);
                if (string.IsNullOrEmpty(选中))
                {
                    return false;
                }

                var 选中索引 = 选项显示列表.IndexOf(选中);
                if (选中索引 < 0 || 选中索引 >= 显示到键映射.Count)
                {
                    return false;
                }

                var 选中键 = 显示到键映射[选中索引];
                if (!脚本字典.TryGetValue(选中键, out var 脚本路径) || !File.Exists(脚本路径))
                {
                    return false;
                }

                string 脚本内容 = File.ReadAllText(脚本路径);
                var 子脚本 = new 自动任务脚本(hWnd, 脚本内容);
                return 子脚本.执行();
            }
            catch
            {
                return false;
            }
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@选定执行:\n" +
                   $"选项列表[{选项列表}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 选定执行 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@选定执行:"))
            {
                var 节点 = new 选定执行();
                Regex regex = new Regex(@"选项列表\[([^\]]*)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.选项列表 = match.Groups[1].Value;
                }
                控制节点.解析基类字段(节点, 字符串, null);
                return 节点;
            }
            return null;
        }

        public static 选定执行 创建节点(IntPtr hWnd)
        {
            var userdata路径 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "淼喵妙脚本DIY", "userdata.json");

            if (!File.Exists(userdata路径))
            {
                通知工具.信息弹窗("没有可用的脚本分类，请先在脚本管理中添加分类和脚本");
                return null;
            }

            var userdata = JsonSerializer.Deserialize<对象型UserData>(File.ReadAllText(userdata路径));
            if (userdata?.TaskCategories == null || userdata.TaskCategories.Count == 0)
            {
                通知工具.信息弹窗("没有可用的脚本分类，请先在脚本管理中添加分类和脚本");
                return null;
            }

            var 选项显示列表 = new List<string>();
            var 显示到标识映射 = new List<string>();

            foreach (var 分类 in userdata.TaskCategories)
            {
                递归收集选项(分类, 选项显示列表, 显示到标识映射);
            }

            if (选项显示列表.Count == 0)
            {
                通知工具.信息弹窗("没有可用的脚本分类，请先在脚本管理中添加分类和脚本");
                return null;
            }

            var builder = new StringBuilder();

            while (true)
            {
                var 选中 = 通知工具.选项弹窗("请选择要添加的脚本:", 选项显示列表);
                if (string.IsNullOrEmpty(选中))
                {
                    break;
                }

                var 索引 = 选项显示列表.IndexOf(选中);
                if (索引 < 0 || 索引 >= 显示到标识映射.Count)
                {
                    break;
                }

                builder.Append(显示到标识映射[索引]);
                builder.Append(';');

                if (!通知工具.确认弹窗("是否继续添加可选脚本?"))
                {
                    break;
                }
            }

            var 选项列表字符串 = builder.ToString().TrimEnd(';');
            if (string.IsNullOrEmpty(选项列表字符串))
            {
                return null;
            }

            return new 选定执行 { 选项列表 = 选项列表字符串 };
        }

        private static void 递归收集脚本(对象型TaskCategory 分类, Dictionary<string, string> 脚本字典)
        {
            if (分类.TaskPaths != null)
            {
                foreach (var 路径 in 分类.TaskPaths)
                {
                    var 文件名 = Path.GetFileNameWithoutExtension(路径);
                    var 键 = $"{分类.CategoryName}:{文件名}";
                    if (!脚本字典.ContainsKey(键))
                        脚本字典[键] = 路径;
                }
            }
            if (分类.SubCategories != null)
            {
                foreach (var sub in 分类.SubCategories)
                {
                    递归收集脚本(sub, 脚本字典);
                }
            }
        }

        private static void 递归收集选项(对象型TaskCategory 分类, List<string> 选项显示列表, List<string> 显示到标识映射)
        {
            if (分类.TaskPaths != null)
            {
                foreach (var 路径 in 分类.TaskPaths)
                {
                    var 文件名 = Path.GetFileNameWithoutExtension(路径);
                    选项显示列表.Add($"[{分类.CategoryName}] {文件名}");
                    显示到标识映射.Add($"{分类.CategoryName}:{文件名}");
                }
            }
            if (分类.SubCategories != null)
            {
                foreach (var sub in 分类.SubCategories)
                {
                    递归收集选项(sub, 选项显示列表, 显示到标识映射);
                }
            }
        }
    }
}
