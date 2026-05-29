using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace 淼喵妙神奇工具库.键鼠库.动作
{
    public static class 创建脚本
    {
        private static readonly string 脚本目录 = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "淼喵妙脚本DIY", "scripts");

        private static List<控制节点> _节点列表 = new List<控制节点>();
        private static string _进程名 = "";
        private static string _窗口标题 = "";

        public static Func<string, string, bool> 注册脚本回调;

        public static void 创建工作脚本(string 进程名, string 窗口标题)
        {
            _节点列表.Clear();
            _进程名 = 进程名 ?? "";
            _窗口标题 = 窗口标题 ?? "";
        }

        public static int 追加节点(控制节点 节点)
        {
            _节点列表.Add(节点);
            return _节点列表.Count - 1;
        }

        public static void 移除节点(int 索引)
        {
            if (索引 < 0 || 索引 >= _节点列表.Count) return;
            var 被移除 = _节点列表[索引];
            _节点列表.RemoveAt(索引);
            foreach (var n in _节点列表)
            {
                if (n.成功后跳转 == 被移除) n.成功后跳转 = null;
                if (n.失败后跳转 == 被移除) n.失败后跳转 = null;
            }
        }

        public static bool 修改节点(int 索引, Dictionary<string, object> 参数字典)
        {
            if (索引 < 0 || 索引 >= _节点列表.Count || 参数字典 == null) return false;
            var node = _节点列表[索引];
            var type = node.GetType();

            foreach (var kv in 参数字典)
            {
                var prop = type.GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        object val = kv.Value;
                        if (val != null && prop.PropertyType != val.GetType())
                            val = Convert.ChangeType(val, prop.PropertyType);
                        prop.SetValue(node, val);
                    }
                    catch { }
                    continue;
                }

                var field = type.GetField(kv.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (field != null)
                {
                    try
                    {
                        object val = kv.Value;
                        if (val != null && field.FieldType != val.GetType())
                            val = Convert.ChangeType(val, field.FieldType);
                        field.SetValue(node, val);
                    }
                    catch { }
                }
            }
            return true;
        }

        public static List<控制节点> 获取节点列表() => new List<控制节点>(_节点列表);

        public static 控制节点 获取节点(int 索引)
        {
            if (索引 < 0 || 索引 >= _节点列表.Count) return null;
            return _节点列表[索引];
        }

        public static string 保存脚本(string 脚本名, string 备注, string 分类名 = "未分类")
        {
            if (_节点列表.Count == 0)
                return "错误: 脚本没有节点，无法保存";

            Directory.CreateDirectory(脚本目录);

            var scriptName = 脚本名;
            if (string.IsNullOrEmpty(scriptName))
                scriptName = "auto_script";
            foreach (var c in Path.GetInvalidFileNameChars())
                scriptName = scriptName.Replace(c, '_');

            string filePath = Path.Combine(脚本目录, scriptName + ".script");

            var sb = new StringBuilder();
            sb.Append($"进程名[{_进程名}]窗口标题[{_窗口标题}]");
            if (!string.IsNullOrEmpty(备注))
                sb.Append(备注);

            var tempScript = new 自动任务脚本(IntPtr.Zero);
            tempScript.节点列表.AddRange(_节点列表);
            tempScript.绑定进程名 = _进程名;
            tempScript.绑定窗口标题 = _窗口标题;
            tempScript.脚本备注 = 备注 ?? "";

            foreach (var node in _节点列表)
                sb.Append(node.保存为字符串(tempScript));

            File.WriteAllText(filePath, sb.ToString());

            bool registered = 注册脚本回调?.Invoke(filePath, 分类名 ?? "未分类") ?? false;

            return $"脚本已保存: {filePath}" + (registered ? "" : " (注意: 注册回调未设置，脚本可能未在UI中显示)");
        }

        public static 控制节点 创建节点实例(string 节点类型名, int 成功后等待 = 500, int 失败后等待 = 500)
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Type.EmptyTypes; }
                })
                .FirstOrDefault(t => t.Name == 节点类型名 && typeof(控制节点).IsAssignableFrom(t) && !t.IsAbstract);

            if (type == null) return null;

            var factoryMethod = type.GetMethod("创建节点", BindingFlags.Public | BindingFlags.Static);
            if (factoryMethod != null)
            {
                return (控制节点)factoryMethod.Invoke(null, new object[] { IntPtr.Zero });
            }

            var ctor = type.GetConstructor(new[] { typeof(int), typeof(int) });
            if (ctor != null)
                return (控制节点)ctor.Invoke(new object[] { 成功后等待, 失败后等待 });

            var defaultCtor = type.GetConstructor(Type.EmptyTypes);
            if (defaultCtor != null)
            {
                var node = (控制节点)defaultCtor.Invoke(null);
                node.成功后等待 = 成功后等待;
                node.失败后等待 = 失败后等待;
                return node;
            }

            return null;
        }
    }
}
