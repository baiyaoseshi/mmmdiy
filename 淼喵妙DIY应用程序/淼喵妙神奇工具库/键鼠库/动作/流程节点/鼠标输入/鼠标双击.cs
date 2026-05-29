using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听.获取;
using 淼喵妙神奇工具库.感知库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 鼠标双击 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 节点参数<Point> 点击坐标 = new 节点参数<Point>();
        public 节点参数<鼠标按键类型> 按键类型 = new 节点参数<鼠标按键类型>(鼠标按键类型.左键);

        鼠标双击() : base()
        {
        }

        public 鼠标双击(节点参数<Point> 点击坐标, 节点参数<鼠标按键类型> 按键类型) : base()
        {
            this.点击坐标 = 点击坐标;
            this.按键类型 = 按键类型;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (全局.ContainsKey("鼠标锁定") && (bool)全局["鼠标锁定"])
            {
                return false;
            }

            var 控制器 = new 鼠标控制器(hWnd);
            Point 双击坐标 = 从全局解析(点击坐标, 全局);

            switch (从全局解析(按键类型, 全局))
            {
                case 鼠标按键类型.左键:
                    控制器.双击(双击坐标);
                    break;
                case 鼠标按键类型.右键:
                    控制器.右键单击(双击坐标);
                    等待(100);
                    控制器.右键单击(双击坐标);
                    break;
                case 鼠标按键类型.中键:
                    控制器.中键单击(双击坐标);
                    等待(100);
                    控制器.中键单击(双击坐标);
                    break;
            }
            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@鼠标双击:\n" +
                   $"点击坐标[{节点参数.序列化(点击坐标)}],\n" +
                   $"按键类型[{节点参数.序列化(按键类型)}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 鼠标双击 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@鼠标双击:"))
            {
                var 节点 = new 鼠标双击();
                Regex regex = new Regex(@"点击坐标\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.点击坐标 = 节点参数.反序列化<Point>(match.Groups[1].Value);
                }
                regex = new Regex(@"按键类型\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.按键类型 = 节点参数.反序列化<鼠标按键类型>(match.Groups[1].Value);
                }
                return 节点;
            }
            return null;
        }

        public static 鼠标双击 创建节点(IntPtr hWnd)
        {
            节点参数<Point> 点击坐标参数;
            if (通知工具.确认弹窗("是否使用固定点击坐标？"))
            {
                通知工具.吐司通知("请左键获取点位:");
                var 点 = new 获取点(hWnd).获取点信息();
                点击坐标参数 = new 节点参数<Point>(new Point(点.X, 点.Y));
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入点击坐标变量名:", "", "");
                点击坐标参数 = new 节点参数<Point>(default, 变量名);
            }

            节点参数<鼠标按键类型> 按键类型参数;
            if (通知工具.确认弹窗("是否使用固定按键类型？"))
            {
                var 按键选项 = new List<string> { "左键", "右键", "中键" };
                string 选择 = 通知工具.选项弹窗("请选择鼠标按键类型:", 按键选项);
                鼠标按键类型 按键类型值 = (鼠标按键类型)Enum.Parse(typeof(鼠标按键类型), 选择);
                按键类型参数 = new 节点参数<鼠标按键类型>(按键类型值);
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入按键类型变量名:", "", "");
                按键类型参数 = new 节点参数<鼠标按键类型>(default, 变量名);
            }

            return new 鼠标双击(点击坐标参数, 按键类型参数);
        }
    }
}