using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.感知库.串联;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听.获取;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙神奇工具库.感知库.识别;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 图片位置 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 串联图片 串联图;
        public string 串联图数据Json = "";
        public string 变量名 = "";

        图片位置() : base() { }

        public 图片位置(串联图片 串联图, string 变量名 = "") : base()
        {
            this.串联图 = 串联图;
            if (串联图 is 串联图片 链式)
            {
                this.串联图数据Json = 串联图片数据.从串联图创建(链式).序列化();
            }
            this.变量名 = 变量名;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            Point 找到位置 = Point.Empty;

            if (串联图 == null && !string.IsNullOrEmpty(串联图数据Json))
            {
                var 数据 = 串联图片数据.反序列化(串联图数据Json);
                串联图 = 数据?.还原为串联图();
            }

            if (串联图 != null)
            {
                var 结果 = 串联图.搜索(hWnd, 全局);
                if (结果.Count > 0)
                {
                    var 第一个匹配 = 结果[0];
                    找到位置 = new Point(第一个匹配.X + 第一个匹配.Width / 2, 第一个匹配.Y + 第一个匹配.Height / 2);
                }
            }

            if (!string.IsNullOrEmpty(变量名))
            {
                全局[变量名] = 找到位置;
            }

            return 找到位置 != Point.Empty;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            if (串联图 is 串联图片 链式 && string.IsNullOrEmpty(串联图数据Json))
            {
                串联图数据Json = 串联图片数据.从串联图创建(链式).序列化();
            }
            return "@图片位置:\n" +
                   $"串联图数据Json[{串联图数据Json}],\n" +
                   $"Point变量名[{变量名}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 图片位置 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@图片位置:"))
            {
                var 节点 = new 图片位置();
                Regex regex = new Regex(@"串联图数据Json\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success && !string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    节点.串联图数据Json = match.Groups[1].Value;
                    var 数据 = 串联图片数据.反序列化(节点.串联图数据Json);
                    节点.串联图 = 数据?.还原为串联图();
                }
                regex = new Regex(@"Point变量名\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.变量名 = match.Groups[1].Value;
                }
                return 节点;
            }
            return null;
        }

        public static 图片位置 创建节点(IntPtr hWnd)
        {
            var 串联图 = 串联图片.创建(hWnd, out Rectangle 目标框);

            string 变量名 = 通知工具.输入弹窗("请输入变量名:", "", "");

            return new 图片位置(串联图, 变量名);
        }
    }
}