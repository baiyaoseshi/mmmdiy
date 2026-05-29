using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 长按按键 : 控制节点
    {
        public 节点参数<string> 按键 = new 节点参数<string>();
        public 节点参数<int> 长按时间 = new 节点参数<int>();

        长按按键() : base() { }

        public 长按按键(节点参数<string> 按键, 节点参数<int> 长按时间) : base()
        {
            this.按键 = 按键;
            this.长按时间 = 长按时间;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            VirtualKeyCode 键码 = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), 从全局解析(按键, 全局));
            int 长按时间值 = 从全局解析(长按时间, 全局);
            string 锁定键 = $"按键锁定_{键码}";
            if (全局.ContainsKey(锁定键) && (bool)全局[锁定键])
            {
                return false;
            }

            全局[锁定键] = true;

            var 按键副本 = 按键;
            var 长按时间副本 = 长按时间;

            Task.Run(() =>
            {
                try
                {
                    var 键码值 = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), 从全局解析(按键副本, 全局));
                    按键控制器.长按键(键码值);
                    等待(从全局解析(长按时间副本, 全局));
                    按键控制器.释放键(键码值);
                }
                finally
                {
                    if (全局.ContainsKey(锁定键))
                    {
                        全局[锁定键] = false;
                    }
                }
            });

            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@长按按键:\n" +
                   $"按键[{节点参数.序列化(按键)}],\n" +
                   $"长按时间[{节点参数.序列化(长按时间)}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 长按按键 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@长按按键:"))
            {
                var 节点 = new 长按按键();
                Regex regex = new Regex(@"按键\[([^\]]*)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.按键 = 节点参数.反序列化<string>(match.Groups[1].Value);
                }
                regex = new Regex(@"长按时间\[([^\]]*)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.长按时间 = 节点参数.反序列化<int>(match.Groups[1].Value);
                }
                return 节点;
            }
            return null;
        }

        public static 长按按键 创建节点(IntPtr hWnd)
        {
            节点参数<string> 按键参数;
            if (通知工具.确认弹窗("是否使用固定按键？"))
            {
                通知工具.吐司通知("请按下要长按的按键...");
                var 监听 = new 按键监听();
                VirtualKeyCode? 监听到的键码 = null;
                Stopwatch stopwatch = new Stopwatch();
                监听.注册事件处理程序(按键监听.键盘事件.按下, (ref 按键监听.KeyboardHookStruct k) =>
                {
                    stopwatch.Start();
                });
                int 时间 = 0;
                监听.注册事件处理程序(按键监听.键盘事件.释放, (ref 按键监听.KeyboardHookStruct k) =>
                {
                    时间 = (int)stopwatch.ElapsedMilliseconds;
                    监听到的键码 = k.GetVirtualKeyCode();
                });
                监听.安装钩子();
                while (监听到的键码 == null)
                {
                    System.Threading.Thread.Sleep(10);
                }
                监听.卸载钩子();
                按键参数 = new 节点参数<string>(监听到的键码.Value.ToString());
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入按键变量名:", "", "");
                按键参数 = new 节点参数<string>(default, 变量名);
            }

            节点参数<int> 时间参数;
            if (通知工具.确认弹窗("是否使用固定长按时间？"))
            {
                string 时间文本 = 通知工具.输入弹窗("请输入长按时间(毫秒):", "", "");
                int 时间值 = int.Parse(时间文本);
                时间参数 = new 节点参数<int>(时间值);
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入长按时间变量名:", "", "");
                时间参数 = new 节点参数<int>(default, 变量名);
            }
            return new 长按按键(按键参数, 时间参数);
        }
    }
}