using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听.获取;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 鼠标拖拽 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 节点参数<Point> 起点 = new 节点参数<Point>();
        public 节点参数<Point> 终点 = new 节点参数<Point>();
        public 节点参数<int> 等待时间 = new 节点参数<int>(100);

        鼠标拖拽() : base()
        {
        }

        public 鼠标拖拽(节点参数<Point> 起点, 节点参数<Point> 终点, 节点参数<int> 等待时间) : base()
        {
            this.起点 = 起点;
            this.终点 = 终点;
            this.等待时间 = 等待时间;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (全局.ContainsKey("鼠标锁定") && (bool)全局["鼠标锁定"])
            {
                return false;
            }

            全局["鼠标锁定"] = true;

            var 起点副本 = 起点;
            var 终点副本 = 终点;
            var 等待副本 = 等待时间;

            Task.Run(() =>
            {
                try
                {
                    var 窗口框 = 窗口处理器.获取窗口框(hWnd);
                    Point 起点值 = 从全局解析(起点副本, 全局);
                    Point 终点值 = 从全局解析(终点副本, 全局);
                    var 起点点 = new Point(起点值.X + 窗口框.X, 起点值.Y + 窗口框.Y);
                    var 终点点 = new Point(终点值.X + 窗口框.X, 终点值.Y + 窗口框.Y);
                    var 控制器 = new 鼠标控制器(hWnd);
                    控制器.拖拽(起点点, 终点点);
                }
                finally
                {
                    if (全局.ContainsKey("鼠标锁定"))
                    {
                        全局["鼠标锁定"] = false;
                    }
                }
            });

            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@鼠标拖拽:\n" +
                   $"起点[{节点参数.序列化(起点)}],\n" +
                   $"终点[{节点参数.序列化(终点)}],\n" +
                   $"等待时间[{节点参数.序列化(等待时间)}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 鼠标拖拽 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@鼠标拖拽:"))
            {
                var 节点 = new 鼠标拖拽();
                节点.起点 = 解析参数<Point>(字符串, @"起点\[([^\]]+)\]");
                节点.终点 = 解析参数<Point>(字符串, @"终点\[([^\]]+)\]");
                节点.等待时间 = 解析参数<int>(字符串, @"等待时间\[([^\]]+)\]");
                return 节点;
            }
            return null;
        }

        private static 节点参数<T> 解析参数<T>(string 字符串, string 正则)
        {
            Match match = Regex.Match(字符串, 正则);
            if (match.Success)
                return 节点参数.反序列化<T>(match.Groups[1].Value);
            return new 节点参数<T>();
        }

        public static 鼠标拖拽 创建节点(IntPtr hWnd)
        {
            节点参数<Point> 起点参数;
            if (通知工具.确认弹窗("是否使用固定起点？"))
            {
                通知工具.吐司通知("请框选拖拽区域:");
                var 框 = new 获取框(hWnd).获取框信息();
                var 窗口框 = 窗口处理器.获取窗口框(hWnd);
                起点参数 = new 节点参数<Point>(new Point(框.X, 框.Y));
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入起点变量名:", "", "");
                起点参数 = new 节点参数<Point>(default, 变量名);
            }

            节点参数<Point> 终点参数;
            if (通知工具.确认弹窗("是否使用固定终点？"))
            {
                通知工具.吐司通知("请再次框选拖拽终点区域:");
                var 框 = new 获取框(hWnd).获取框信息();
                终点参数 = new 节点参数<Point>(new Point(框.X + 框.Width, 框.Y + 框.Height));
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入终点变量名:", "", "");
                终点参数 = new 节点参数<Point>(default, 变量名);
            }

            节点参数<int> 等待参数;
            if (通知工具.确认弹窗("是否使用固定等待时间？"))
            {
                int 等待时间 = int.Parse(通知工具.输入弹窗("请输入等待时间(毫秒):", "", ""));
                等待参数 = new 节点参数<int>(等待时间);
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入等待时间变量名:", "", "");
                等待参数 = new 节点参数<int>(default, 变量名);
            }

            return new 鼠标拖拽(起点参数, 终点参数, 等待参数);
        }
    }
}