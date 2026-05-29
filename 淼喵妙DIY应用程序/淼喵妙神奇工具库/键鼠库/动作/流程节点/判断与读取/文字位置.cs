using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙神奇工具库.感知库.识别;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听.获取;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 文字位置 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 节点参数<string> 搜索文字 = new 节点参数<string>();
        public 节点参数<Rectangle> 搜索区域 = new 节点参数<Rectangle>();
        public string 变量名 = "";
        int 取样边距 = 8;

        文字位置() : base() { }

        public 文字位置(节点参数<string> 搜索文字, 节点参数<Rectangle> 搜索区域 = null, string 变量名 = "", int 取样边距 = 8) : base()
        {
            this.搜索文字 = 搜索文字;
            this.搜索区域 = 搜索区域 ?? new 节点参数<Rectangle>();
            this.变量名 = 变量名;
            this.取样边距 = 取样边距;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (!全局.ContainsKey("文字识别器") || 全局["文字识别器"] == null)
                全局["文字识别器"] = new 文字识别器(窗口处理器.获取窗口框(hWnd).Size, 取样边距);
            文字识别器 识别器 = 全局["文字识别器"] as 文字识别器;
            Rectangle 区域 = 从全局解析(搜索区域, 全局);

            var matches = 识别器.搜索文本(从全局解析(搜索文字, 全局), 区域, hWnd);

            Point 找到位置 = Point.Empty;
            if (matches.Count > 0)
            {
                var first = matches[0];
                找到位置 = new Point(first.X + first.Width / 2, first.Y + first.Height / 2);
            }

            if (!string.IsNullOrEmpty(变量名))
            {
                全局[变量名] = 找到位置;
            }

            return 找到位置 != Point.Empty;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@文字位置:\n" +
                   $"搜索文字[{节点参数.序列化(搜索文字)}],\n" +
                   $"搜索区域[{节点参数.序列化(搜索区域)}],\n" +
                   $"Point变量名[{变量名}],\n" +
                   $"取样边距[{取样边距}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 文字位置 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@文字位置:"))
            {
                var 节点 = new 文字位置();
                节点.搜索文字 = 解析参数<string>(字符串, @"搜索文字\[([^\]]+)\]");
                节点.搜索区域 = 解析参数<Rectangle>(字符串, @"搜索区域\[([^\]]+)\]");
                Regex regex = new Regex(@"Point变量名\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.变量名 = match.Groups[1].Value;
                }
                regex = new Regex(@"取样边距\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.取样边距 = int.Parse(match.Groups[1].Value);
                }
                return 节点;
            }
            return null;
        }

        public static 文字位置 创建节点(IntPtr hWnd)
        {
            节点参数<string> 搜索文字参数;
            if (通知工具.确认弹窗("是否使用固定搜索文字？"))
            {
                string 搜索文字 = 通知工具.输入弹窗("请输入要搜索的文字:", "", "");
                搜索文字参数 = new 节点参数<string>(搜索文字);
            }
            else
            {
                string 搜索文字变量名 = 通知工具.输入弹窗("请输入搜索文字变量名:", "", "");
                搜索文字参数 = new 节点参数<string>(default, 搜索文字变量名);
            }

            节点参数<Rectangle> 搜索区域参数;
            if (通知工具.确认弹窗("是否使用固定搜索区域？"))
            {
                通知工具.吐司通知("请框选文字搜索区域(可选):");
                var 区域框 = new 获取框(hWnd).获取框信息();
                搜索区域参数 = new 节点参数<Rectangle>(new Rectangle(区域框.X, 区域框.Y, 区域框.Width, 区域框.Height));
            }
            else
            {
                string 区域变量名 = 通知工具.输入弹窗("请输入搜索区域变量名:", "", "");
                搜索区域参数 = new 节点参数<Rectangle>(default, 区域变量名);
            }

            string 变量名 = 通知工具.输入弹窗("请输入存储位置的变量名(可选):", "", "");

            return new 文字位置(搜索文字参数, 搜索区域参数, 变量名);
        }

        private static 节点参数<T> 解析参数<T>(string 字符串, string pattern)
        {
            Match match = Regex.Match(字符串, pattern);
            if (match.Success)
                return 节点参数.反序列化<T>(match.Groups[1].Value);
            return new 节点参数<T>();
        }
    }
}