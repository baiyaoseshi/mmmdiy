using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听.获取;
using 淼喵妙神奇工具库.感知库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 鼠标悬停 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 节点参数<Point> 悬停坐标 = new 节点参数<Point>();

        鼠标悬停() : base()
        {
        }

        public 鼠标悬停(节点参数<Point> 悬停坐标) : base()
        {
            this.悬停坐标 = 悬停坐标;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (全局.ContainsKey("鼠标锁定") && (bool)全局["鼠标锁定"])
            {
                return false;
            }

            var 控制器 = new 鼠标控制器(hWnd);
            控制器.移动鼠标(从全局解析(悬停坐标, 全局));
            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@鼠标悬停:\n" +
                   $"悬停坐标[{节点参数.序列化(悬停坐标)}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 鼠标悬停 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@鼠标悬停:"))
            {
                var 节点 = new 鼠标悬停();
                Regex regex = new Regex(@"悬停坐标\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.悬停坐标 = 节点参数.反序列化<Point>(match.Groups[1].Value);
                }
                return 节点;
            }
            return null;
        }

        public static 鼠标悬停 创建节点(IntPtr hWnd)
        {
            节点参数<Point> 悬停坐标参数;
            if (通知工具.确认弹窗("是否使用固定悬停坐标？"))
            {
                通知工具.吐司通知("请左键获取点位:");
                var 点 = new 获取点(hWnd).获取点信息();
                悬停坐标参数 = new 节点参数<Point>(new Point(点.X, 点.Y));
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入悬停坐标变量名:", "", "");
                悬停坐标参数 = new 节点参数<Point>(default, 变量名);
            }

            return new 鼠标悬停(悬停坐标参数);
        }
    }
}