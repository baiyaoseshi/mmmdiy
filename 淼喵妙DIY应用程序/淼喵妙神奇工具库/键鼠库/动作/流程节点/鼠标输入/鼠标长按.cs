using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听.获取;
using 淼喵妙神奇工具库.感知库;
namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 鼠标长按 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 节点参数<Point> 点击坐标 = new 节点参数<Point>();
        public 节点参数<鼠标按键类型> 按键类型 = new 节点参数<鼠标按键类型>(鼠标按键类型.左键);
        public 节点参数<int> 长按时间 = new 节点参数<int>(1000);

        鼠标长按() : base()
        {
        }

        public 鼠标长按(节点参数<Point> 点击坐标, 节点参数<鼠标按键类型> 按键类型, 节点参数<int> 长按时间) : base()
        {
            this.点击坐标 = 点击坐标;
            this.按键类型 = 按键类型;
            this.长按时间 = 长按时间;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (全局.ContainsKey("鼠标锁定") && (bool)全局["鼠标锁定"])
            {
                return false;
            }

            全局["鼠标锁定"] = true;

            var 点击坐标副本 = 点击坐标;
            var 按键类型副本 = 按键类型;
            var 长按时间副本 = 长按时间;

            Task.Run(() =>
            {
                try
                {
                    var 控制器 = new 鼠标控制器(hWnd);
                    Point 长按坐标值 = 从全局解析(点击坐标副本, 全局);
                    var 按键类型值 = 从全局解析(按键类型副本, 全局);

                    switch (按键类型值)
                    {
                        case 鼠标按键类型.左键:
                            控制器.长按左键(长按坐标值);
                            break;
                        case 鼠标按键类型.右键:
                            控制器.长按右键(长按坐标值);
                            break;
                        case 鼠标按键类型.中键:
                            控制器.长按中键(长按坐标值);
                            break;
                    }

                    等待(从全局解析(长按时间副本, 全局));

                    switch (按键类型值)
                    {
                        case 鼠标按键类型.左键:
                            控制器.释放左键();
                            break;
                        case 鼠标按键类型.右键:
                            控制器.释放右键();
                            break;
                        case 鼠标按键类型.中键:
                            控制器.释放中键();
                            break;
                    }
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
            return "@鼠标长按:\n" +
                   $"点击坐标[{节点参数.序列化(点击坐标)}],\n" +
                   $"按键类型[{节点参数.序列化(按键类型)}],\n" +
                   $"长按时间[{节点参数.序列化(长按时间)}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 鼠标长按 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@鼠标长按:"))
            {
                var 节点 = new 鼠标长按();
                节点.点击坐标 = 解析参数<Point>(字符串, @"点击坐标\[([^\]]+)\]");
                节点.按键类型 = 解析参数<鼠标按键类型>(字符串, @"按键类型\[([^\]]+)\]");
                节点.长按时间 = 解析参数<int>(字符串, @"长按时间\[([^\]]+)\]");
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

        public static 鼠标长按 创建节点(IntPtr hWnd)
        {
            节点参数<Point> 点击坐标参数;
            if (通知工具.确认弹窗("是否使用固定点击坐标？"))
            {
                通知工具.吐司通知("请点击要长按的坐标位置...");
                var 获取器 = new 获取点(hWnd);
                点击坐标参数 = new 节点参数<Point>(获取器.获取点信息());
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

            节点参数<int> 长按时间参数;
            if (通知工具.确认弹窗("是否使用固定长按时间？"))
            {
                string 时间文本 = 通知工具.输入弹窗("请输入长按时间(毫秒):", "", "");
                int 时间值 = int.Parse(时间文本);
                长按时间参数 = new 节点参数<int>(时间值);
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入长按时间变量名:", "", "");
                长按时间参数 = new 节点参数<int>(default, 变量名);
            }

            return new 鼠标长按(点击坐标参数, 按键类型参数, 长按时间参数);
        }
    }
}