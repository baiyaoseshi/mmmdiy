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
    public class 识别文字 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 节点参数<Rectangle> 识别区域 = new 节点参数<Rectangle>();
        public string 结果变量名 = "";
        int 取样边距 = 8;

        识别文字() : base() { }

        public 识别文字(节点参数<Rectangle> 识别区域, string 结果变量名 = "", int 取样边距 = 8) : base()
        {
            this.识别区域 = 识别区域;
            this.结果变量名 = 结果变量名;
            this.取样边距 = 取样边距;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (!全局.ContainsKey("文字识别器") || 全局["文字识别器"] == null)
                全局["文字识别器"] = new 文字识别器(窗口处理器.获取窗口框(hWnd).Size, 取样边距);
            文字识别器 识别器 = 全局["文字识别器"] as 文字识别器;
            Rectangle 区域 = 从全局解析(识别区域, 全局);

            var result = 识别器.文字处理(区域, hWnd);
            string 识别结果 = result.Text;

            if (!string.IsNullOrEmpty(结果变量名))
            {
                全局[结果变量名] = 识别结果;
            }

            return !string.IsNullOrEmpty(识别结果);
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@识别文字:\n" +
                   $"识别区域[{节点参数.序列化(识别区域)}],\n" +
                   $"结果变量名[{结果变量名}],\n" +
                   $"取样边距[{取样边距}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 识别文字 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@识别文字:"))
            {
                var 节点 = new 识别文字();
                节点.识别区域 = 解析参数<Rectangle>(字符串, @"识别区域\[([^\]]+)\]");
                Regex regex = new Regex(@"结果变量名\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.结果变量名 = match.Groups[1].Value;
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

        public static 识别文字 创建节点(IntPtr hWnd)
        {
            节点参数<Rectangle> 识别区域参数;
            if (通知工具.确认弹窗("是否使用固定识别区域？"))
            {
                通知工具.吐司通知("请框选文字识别区域:");
                var 区域框 = new 获取框(hWnd).获取框信息();
                识别区域参数 = new 节点参数<Rectangle>(new Rectangle(区域框.X, 区域框.Y, 区域框.Width, 区域框.Height));
            }
            else
            {
                string 区域变量名 = 通知工具.输入弹窗("请输入识别区域变量名:", "", "");
                识别区域参数 = new 节点参数<Rectangle>(default, 区域变量名);
            }

            string 结果变量名 = 通知工具.输入弹窗("请输入存储识别结果的变量名:", "", "");

            return new 识别文字(识别区域参数, 结果变量名);
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