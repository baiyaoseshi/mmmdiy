using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙神奇工具库.感知库.识别;
using OpenCvSharp;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听.获取;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 区域截图 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 节点参数<Rectangle> 截图区域 = new 节点参数<Rectangle>();
        public string 变量名 = "";
        int 取样边距 = 8;
        public int 变化阈值 = 10;

        区域截图() : base() { }

        public 区域截图(节点参数<Rectangle> 截图区域, string 变量名, int 取样边距 = 8, int 变化阈值 = 10) : base()
        {
            this.截图区域 = 截图区域;
            this.变量名 = 变量名;
            this.取样边距 = 取样边距;
            this.变化阈值 = 变化阈值;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (!全局.ContainsKey("取像识别器") || 全局["取像识别器"] == null)
                全局["取像识别器"] = new 取像识别器(hWnd, 取样边距);
            取像识别器 取像器 = 全局["取像识别器"] as 取像识别器;

            Rectangle 区域 = 从全局解析(截图区域, 全局);
            Mat 截图 = 取像器.取像(区域, hWnd, 变化阈值);

            if (!string.IsNullOrEmpty(变量名))
            {
                全局[变量名] = 截图;
            }
            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@区域截图:\n" +
                   $"截图区域[{节点参数.序列化(截图区域)}],\n" +
                   $"变量名[{变量名}],\n" +
                   $"取样边距[{取样边距}],\n" +
                   $"变化阈值[{变化阈值}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 区域截图 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@区域截图:"))
            {
                var 节点 = new 区域截图();
                节点.截图区域 = 解析参数<Rectangle>(字符串, @"截图区域\[([^\]]+)\]");
                Regex regex = new Regex(@"变量名\[([^\]]+)\]");
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
                regex = new Regex(@"变化阈值\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.变化阈值 = int.Parse(match.Groups[1].Value);
                }
                return 节点;
            }
            return null;
        }

        public static 区域截图 创建节点(IntPtr hWnd)
        {
            节点参数<Rectangle> 截图区域参数;
            if (通知工具.确认弹窗("是否使用固定截图区域？"))
            {
                通知工具.吐司通知("请框选截图区域:");
                var 区域框 = new 获取框(hWnd).获取框信息();
                截图区域参数 = new 节点参数<Rectangle>(new Rectangle(区域框.X, 区域框.Y, 区域框.Width, 区域框.Height));
            }
            else
            {
                string 区域变量名 = 通知工具.输入弹窗("请输入截图区域变量名:", "", "");
                截图区域参数 = new 节点参数<Rectangle>(default, 区域变量名);
            }

            string 变量名 = 通知工具.输入弹窗("请输入变量名:", "", "");

            return new 区域截图(截图区域参数, 变量名);
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