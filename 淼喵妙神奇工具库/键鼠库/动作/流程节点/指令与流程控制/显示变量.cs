using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using Mat = OpenCvSharp.Mat;
using OpenCvSharp.Extensions;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 显示变量 : 控制节点
    {
        public override bool 需要窗口句柄 => false;

        public string 变量名 = "";

        显示变量() : base() { }

        public 显示变量(string 变量名) : base()
        {
            this.变量名 = 变量名;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (!全局.ContainsKey(变量名))
            {
                通知工具.信息弹窗($"变量 \"{变量名}\" 不存在");
                return true;
            }

            var 值 = 全局[变量名];

            var 图片列表 = 提取图片列表(值);

            if (图片列表 != null && 图片列表.Count > 0)
            {
                通知工具.图片查看器(变量名, 图片列表);
            }
            else
            {
                string 显示文本 = 格式化显示值(值);
                通知工具.信息弹窗($"{变量名} = {显示文本}");
            }
            return true;
        }

        private static List<Bitmap> 提取图片列表(object 值)
        {
            if (值 is Mat mat)
            {
                try { return new List<Bitmap> { mat.ToBitmap() }; }
                catch { return null; }
            }
            if (值 is Bitmap bmp)
            {
                return new List<Bitmap> { new Bitmap(bmp) };
            }
            if (值 is IList 列表 && 列表.Count > 0)
            {
                var 首项 = 列表[0];
                if (首项 is Mat)
                {
                    var 结果 = new List<Bitmap>();
                    foreach (var item in 列表)
                    {
                        try { 结果.Add(((Mat)item).ToBitmap()); }
                        catch { return null; }
                    }
                    return 结果;
                }
                if (首项 is Bitmap)
                {
                    var 结果 = new List<Bitmap>();
                    foreach (var item in 列表)
                        结果.Add(new Bitmap((Bitmap)item));
                    return 结果;
                }
            }
            return null;
        }

        private static string 格式化显示值(object 值)
        {
            if (值 == null) return "null";
            if (值 is string s) return $"\"{s}\"";
            if (值 is System.Collections.IDictionary 字典)
            {
                var 条目 = new List<string>();
                foreach (System.Collections.DictionaryEntry kv in 字典)
                    条目.Add($"  {格式化单个值(kv.Key)}: {格式化单个值(kv.Value)}");
                return "{\n" + string.Join(",\n", 条目) + "\n}";
            }
            if (值 is IList 列表)
            {
                var 元素 = new List<string>();
                foreach (var item in 列表)
                    元素.Add(格式化单个值(item));
                return $"[{string.Join(", ", 元素)}]";
            }
            return 格式化单个值(值);
        }

        private static string 格式化单个值(object 值)
        {
            if (值 == null) return "null";
            if (值 is Mat mat)
                return $"[图片: {mat.Width}×{mat.Height}]";
            if (值 is Bitmap bmp)
                return $"[图片: {bmp.Width}×{bmp.Height}]";
            if (值 is Rectangle r)
                return $"({r.X},{r.Y},{r.Width},{r.Height})";
            if (值 is Point p)
                return $"({p.X},{p.Y})";
            if (值 is Size sz)
                return $"{sz.Width}×{sz.Height}";
            if (值 is bool b)
                return b ? "true" : "false";
            return 值.ToString();
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@显示变量:\n" +
                   $"变量名[{变量名}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 显示变量 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@显示变量:"))
            {
                var 节点 = new 显示变量();
                Regex regex = new Regex(@"变量名\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.变量名 = match.Groups[1].Value;
                }
                return 节点;
            }
            return null;
        }

        public static 显示变量 创建节点(IntPtr hWnd)
        {
            string 变量名 = 通知工具.输入弹窗("请输入要显示的变量名:", "", "");
            return new 显示变量(变量名);
        }
    }
}
