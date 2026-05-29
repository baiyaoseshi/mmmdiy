using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class CSharp脚本 : 控制节点
    {
        public string 脚本路径 = "";
        public List<string> 输入变量名列表 = new List<string>();
        public string 输出变量名 = "";

        public override bool 需要窗口句柄 => false;

        CSharp脚本() : base() { }

        public CSharp脚本(string 脚本路径, List<string> 输入变量名列表, string 输出变量名) : base()
        {
            this.脚本路径 = 脚本路径;
            this.输入变量名列表 = 输入变量名列表 ?? new List<string>();
            this.输出变量名 = 输出变量名;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            try
            {
                var 绝对路径 = 脚本扩展工具.解析脚本路径(脚本路径);
                if (!File.Exists(绝对路径))
                {
                    通知工具.错误弹窗($"C#脚本文件不存在: {绝对路径}");
                    return false;
                }

                var 脚本内容 = File.ReadAllText(绝对路径);
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

                object 结果 = 执行脚本(脚本内容, 变量字典);

                if (!string.IsNullOrEmpty(输出变量名))
                {
                    全局[输出变量名] = 结果;
                }
                return true;
            }
            catch (Exception ex)
            {
                通知工具.错误弹窗($"C#脚本执行失败: {ex.Message}");
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

        private static object 执行脚本(string 脚本内容, Dictionary<string, object> 变量字典)
        {
            var 脚本代码 = new StringBuilder();
            脚本代码.AppendLine("new Func<Dictionary<string, object>, object>(vars => {");
            foreach (var kv in 变量字典)
            {
                脚本代码.AppendLine($"    dynamic {kv.Key} = vars[\"{kv.Key}\"];");
            }
            脚本代码.AppendLine(脚本内容);
            脚本代码.Append("})");

            var 委托 = CSharpScript.EvaluateAsync<Func<Dictionary<string, object>, object>>(
                脚本代码.ToString(), 脚本选项).GetAwaiter().GetResult();
            return 委托(变量字典);
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            string 变量列表 = string.Join(";", 输入变量名列表);
            return "@CSharp脚本:\n" +
                   $"脚本路径[{脚本路径}],\n" +
                   $"输入变量[{变量列表}],\n" +
                   $"输出变量名[{输出变量名}],\n" +
                   base.保存为字符串(脚本);
        }

        public static CSharp脚本 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@CSharp脚本:"))
            {
                var 节点 = new CSharp脚本();
                Regex regex = new Regex(@"脚本路径\[([^\]]+)\]");
                var match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.脚本路径 = match.Groups[1].Value;
                }
                regex = new Regex(@"输入变量\[([^\]]*)\]");
                match = regex.Match(字符串);
                if (match.Success && !string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    节点.输入变量名列表 = match.Groups[1].Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
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

        public static CSharp脚本 创建节点(IntPtr hWnd)
        {
            通知工具.信息弹窗(脚本扩展工具.获取目录提示文本());

            string 脚本路径 = "";
            if (通知工具.确认弹窗("是否新建C#脚本模板文件？"))
            {
                string 文件名 = 通知工具.输入弹窗("请输入脚本文件名(不含扩展名):", "新建脚本", "新CSharp脚本");
                if (string.IsNullOrEmpty(文件名))
                    return null;
                var 目录 = 脚本扩展工具.获取推荐脚本目录();
                var 完整路径 = Path.Combine(目录, 文件名 + ".csx");
                脚本扩展工具.创建CSharp模板(完整路径);
                脚本扩展工具.用关联程序打开(完整路径);
                脚本路径 = 完整路径;
            }
            else
            {
                脚本路径 = 通知工具.输入弹窗("请输入脚本路径（支持相对路径）:", "脚本路径", "");
                if (string.IsNullOrEmpty(脚本路径))
                    return null;
            }

            string 输入变量字符串 = 通知工具.输入弹窗("请输入输入变量名(用分号分隔，如: x;y;z):", "", "");
            List<string> 输入变量名列表 = !string.IsNullOrEmpty(输入变量字符串)
                ? 输入变量字符串.Split(';').ToList()
                : new List<string>();
            string 输出变量名 = 通知工具.输入弹窗("请输入输出变量名:", "", "");
            return new CSharp脚本(脚本路径, 输入变量名列表, 输出变量名);
        }
    }
}
