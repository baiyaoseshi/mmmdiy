using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 计算函数 : 控制节点
    {
        public string 代码 = "";
        public List<string> 输入变量名列表 = new List<string>();
        public string 输出变量名 = "";

        计算函数() : base() { }

        public 计算函数(string 代码, List<string> 输入变量名列表, string 输出变量名) : base()
        {
            this.代码 = 代码;
            this.输入变量名列表 = 输入变量名列表 ?? new List<string>();
            this.输出变量名 = 输出变量名;
        }

        public override void 初始化()
        {
            if (!string.IsNullOrEmpty(代码))
            {
                获取或编译委托(代码, 输入变量名列表);
            }
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            try
            {
                var 变量字典 = new Dictionary<string, object>();
                
                foreach (var 变量名 in 输入变量名列表)
                {
                    if (全局.ContainsKey(变量名))
                    {
                        变量字典[变量名] = 全局[变量名];
                    }
                    else
                    {
                        通知工具.吐司通知($"变量 '{变量名}' 不存在于全局中");
                        return false;
                    }
                }

                object 结果 = 执行计算(代码, 变量字典);

                if (!string.IsNullOrEmpty(输出变量名))
                {
                    全局[输出变量名] = 结果;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static ScriptOptions _缓存选项;
        private static ScriptOptions 脚本选项
        {
            get
            {
                if (_缓存选项 == null)
                {
                    _缓存选项 = ScriptOptions.Default
                        .AddReferences(typeof(System.Drawing.Rectangle).Assembly)
                        .AddReferences(typeof(System.Math).Assembly)
                        .AddReferences(typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly)
                        .AddImports("System")
                        .AddImports("System.Collections.Generic")
                        .AddImports("System.Drawing")
                        .AddImports("System.Linq");
                }
                return _缓存选项;
            }
        }

        private static readonly Dictionary<string, Func<Dictionary<string, object>, object>> _委托缓存 = new();

        private object 执行计算(string 代码, Dictionary<string, object> 变量字典)
        {
            try
            {
                var 委托 = 获取或编译委托(代码, 变量字典.Keys.ToList());
                return 委托(变量字典);
            }
            catch
            {
                return 尝试简单解析(代码);
            }
        }

        private static Func<Dictionary<string, object>, object> 获取或编译委托(string 代码, List<string> 变量名列表)
        {
            string 缓存键 = 变量名列表.Count == 0
                ? 代码
                : $"{代码}|{string.Join(",", 变量名列表.OrderBy(k => k))}";

            if (!_委托缓存.TryGetValue(缓存键, out var 委托))
            {
                var 脚本代码 = new StringBuilder();
                脚本代码.AppendLine("new Func<Dictionary<string, object>, object>(vars => {");
                foreach (var 变量名 in 变量名列表)
                {
                    脚本代码.AppendLine($"    dynamic {变量名} = vars[\"{变量名}\"];");
                }
                脚本代码.AppendLine(代码);
                脚本代码.Append("})");

                委托 = CSharpScript.EvaluateAsync<Func<Dictionary<string, object>, object>>(
                    脚本代码.ToString(), 脚本选项).GetAwaiter().GetResult();
                _委托缓存[缓存键] = 委托;
            }

            return 委托;
        }

        private object 尝试简单解析(string 代码)
        {
            代码 = 代码.Trim();

            if (代码.StartsWith("return ") && 代码.EndsWith(";"))
            {
                代码 = 代码.Substring(7, 代码.Length - 8).Trim();
            }

            if (代码.StartsWith("new Rectangle(") && 代码.EndsWith(")"))
            {
                string 内容 = 代码.Substring(16, 代码.Length - 17);
                string[] 部分 = 内容.Split(',');
                if (部分.Length == 4)
                {
                    return new Rectangle(int.Parse(部分[0].Trim()), 
                                         int.Parse(部分[1].Trim()),
                                         int.Parse(部分[2].Trim()), 
                                         int.Parse(部分[3].Trim()));
                }
            }
            else if (代码.StartsWith("new Point(") && 代码.EndsWith(")"))
            {
                string 内容 = 代码.Substring(10, 代码.Length - 11);
                string[] 部分 = 内容.Split(',');
                if (部分.Length == 2)
                {
                    return new Point(int.Parse(部分[0].Trim()), int.Parse(部分[1].Trim()));
                }
            }
            else if (代码.StartsWith("new Size(") && 代码.EndsWith(")"))
            {
                string 内容 = 代码.Substring(9, 代码.Length - 10);
                string[] 部分 = 内容.Split(',');
                if (部分.Length == 2)
                {
                    return new Size(int.Parse(部分[0].Trim()), int.Parse(部分[1].Trim()));
                }
            }
            else if (代码.EndsWith("f"))
            {
                if (float.TryParse(代码.TrimEnd('f'), out float f))
                    return f;
            }
            else if (代码.Contains('.') || 代码.Contains('e') || 代码.Contains('E'))
            {
                if (double.TryParse(代码, out double d))
                    return d;
                if (float.TryParse(代码, out float f))
                    return f;
            }
            else if (int.TryParse(代码, out int i))
            {
                return i;
            }

            return 代码;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            string 变量列表 = string.Join(";", 输入变量名列表);
            return "@计算函数:\n" +
                   $"代码[{代码}],\n" +
                   $"输入变量[{变量列表}],\n" +
                   $"输出变量名[{输出变量名}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 计算函数 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@计算函数:"))
            {
                var 节点 = new 计算函数();
                节点.代码 = 提取嵌套括号内容(字符串, "代码[");

                Regex regex = new Regex(@"输入变量\[([^\]]*)\]");
                var match = regex.Match(字符串);
                if (match.Success && !string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    节点.输入变量名列表 = match.Groups[1].Value.Split(';').ToList();
                }
                regex = new Regex(@"输出变量名\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.输出变量名 = match.Groups[1].Value;
                }
                return 节点;
            }
            return null;
        }

        private static string 提取嵌套括号内容(string 字符串, string 前缀)
        {
            int 起始 = 字符串.IndexOf(前缀);
            if (起始 < 0) return "";
            int 位置 = 起始 + 前缀.Length;
            int 深度 = 1;
            while (位置 < 字符串.Length && 深度 > 0)
            {
                if (字符串[位置] == '[') 深度++;
                else if (字符串[位置] == ']') 深度--;
                if (深度 > 0) 位置++;
            }
            return 字符串.Substring(起始 + 前缀.Length, 位置 - 起始 - 前缀.Length);
        }

        public static 计算函数 创建节点(IntPtr hWnd)
        {
            string 代码 = 通知工具.输入弹窗("请输入计算代码(C#表达式):", "", "");
            string 输入变量字符串 = 通知工具.输入弹窗("请输入输入变量名(用分号分隔，如: x;y;z):", "", "");
            List<string> 输入变量名列表 = !string.IsNullOrEmpty(输入变量字符串)
                ? 输入变量字符串.Split(';').ToList()
                : new List<string>();
            string 输出变量名 = 通知工具.输入弹窗("请输入输出变量名:", "", "");
            return new 计算函数(代码, 输入变量名列表, 输出变量名);
        }
    }
}