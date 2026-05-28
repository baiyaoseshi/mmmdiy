using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.感知库.串联;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听.获取;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙神奇工具库.感知库.识别;
using System.Drawing;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 点击串联图片 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 串联图片 串联图;
        public string 串联图数据Json = "";
        public int 偏移X = 0;
        public int 偏移Y = 0;
        public 鼠标按键类型 鼠标键 = 鼠标按键类型.左键;

        点击串联图片() : base() { }

        public 点击串联图片(串联图片 串联图, int 偏移X = 0, int 偏移Y = 0, 鼠标按键类型 鼠标键 = 鼠标按键类型.左键) : base()
        {
            this.串联图 = 串联图;
            if (串联图 is 串联图片 链式)
            {
                this.串联图数据Json = 串联图片数据.从串联图创建(链式).序列化();
            }
            this.偏移X = 偏移X;
            this.偏移Y = 偏移Y;
            this.鼠标键 = 鼠标键;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (全局.ContainsKey("鼠标锁定") && (bool)全局["鼠标锁定"])
            {
                return false;
            }
            if (串联图 == null && !string.IsNullOrEmpty(串联图数据Json))
            {
                var 数据 = 串联图片数据.反序列化(串联图数据Json);
                串联图 = 数据?.还原为串联图();
            }
            if (串联图 == null)
            {
                return false;
            }

            var 结果 = 串联图.搜索(hWnd, 全局);
            if (结果.Count == 0)
            {
                return false;
            }

            var 匹配位置 = 结果[0];
            int X = 匹配位置.X + 匹配位置.Width / 2 + 偏移X;
            int Y = 匹配位置.Y + 匹配位置.Height / 2 + 偏移Y;

            var 控制器 = new 鼠标控制器(hWnd);

            switch (鼠标键)
            {
                case 鼠标按键类型.左键:
                    控制器.左键单击(new System.Drawing.Point(X, Y));
                    break;
                case 鼠标按键类型.右键:
                    控制器.右键单击(new System.Drawing.Point(X, Y));
                    break;
                case 鼠标按键类型.中键:
                    控制器.中键单击(new System.Drawing.Point(X, Y));
                    break;
            }

            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            if (串联图 is 串联图片 链式 && string.IsNullOrEmpty(串联图数据Json))
            {
                串联图数据Json = 串联图片数据.从串联图创建(链式).序列化();
            }
            return "@点击联想图片:\n" +
                   $"串联图数据Json[{串联图数据Json}],\n" +
                   $"偏移X[{偏移X}],\n" +
                   $"偏移Y[{偏移Y}],\n" +
                   $"鼠标键[{鼠标键}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 点击串联图片 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@点击联想图片:"))
            {
                var 节点 = new 点击串联图片();
                Regex regex = new Regex(@"串联图数据Json\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success && !string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    节点.串联图数据Json = match.Groups[1].Value;
                    var 数据 = 串联图片数据.反序列化(节点.串联图数据Json);
                    节点.串联图 = 数据?.还原为串联图();
                }
                regex = new Regex(@"偏移X\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.偏移X = int.Parse(match.Groups[1].Value);
                }
                regex = new Regex(@"偏移Y\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.偏移Y = int.Parse(match.Groups[1].Value);
                }
                regex = new Regex(@"鼠标键\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.鼠标键 = (鼠标按键类型)Enum.Parse(typeof(鼠标按键类型), match.Groups[1].Value);
                }
                return 节点;
            }
            return null;
        }

        public static 点击串联图片 创建节点(IntPtr hWnd)
        {
            var 图片 = 串联图片.创建(hWnd, out Rectangle 目标框);

            通知工具.吐司通知("请左键偏移点位:");
            var 偏移点位 = new 获取点(hWnd).获取点信息();
            int X = 偏移点位.X;
            int Y = 偏移点位.Y;

            int 偏移X = X - (目标框.X + 目标框.Width / 2);
            int 偏移Y = Y - (目标框.Y + 目标框.Height / 2);

            var 按键选项 = new List<string> { "左键", "右键", "中键" };
            string 选择 = 通知工具.选项弹窗("请选择鼠标按键类型:", 按键选项);
            鼠标按键类型 鼠标键 = (鼠标按键类型)Enum.Parse(typeof(鼠标按键类型), 选择);

            return new 点击串联图片(图片, 偏移X, 偏移Y, 鼠标键);
        }
    }
}