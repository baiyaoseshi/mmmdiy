using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WindowsInput;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 单击按键 : 控制节点
    {
        public 节点参数<string> 按键 = new 节点参数<string>();

        单击按键() : base() { }

        public 单击按键(节点参数<string> 按键) : base()
        {
            this.按键 = 按键;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            VirtualKeyCode 键码 = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), 从全局解析(按键, 全局));
            string 锁定键 = $"按键锁定_{键码}";
            if (全局.ContainsKey(锁定键) && (bool)全局[锁定键])
            {
                return false;
            }

            按键控制器.单击键(键码);
            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@单击按键:\n" +
                   $"按键[{节点参数.序列化(按键)}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 单击按键 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@单击按键:"))
            {
                var 节点 = new 单击按键();
                Regex regex = new Regex(@"按键\[([^\]]*)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.按键 = 节点参数.反序列化<string>(match.Groups[1].Value);
                }
                return 节点;
            }
            return null;
        }

        public static 单击按键 创建节点(IntPtr hWnd)
        {
            if (通知工具.确认弹窗("是否使用固定按键？"))
            {
                通知工具.吐司通知("请按下要监听的按键...");
                var 监听 = new 按键监听();
                VirtualKeyCode? 监听到的键码 = null;
                监听.注册事件处理程序(按键监听.键盘事件.按下, (ref 按键监听.KeyboardHookStruct k) =>
                {
                    监听到的键码 = k.GetVirtualKeyCode();
                });
                监听.安装钩子();
                while (监听到的键码 == null)
                {
                    System.Threading.Thread.Sleep(10);
                }
                监听.卸载钩子();
                return new 单击按键(new 节点参数<string>(监听到的键码.Value.ToString()));
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入按键变量名:", "", "");
                return new 单击按键(new 节点参数<string>(default, 变量名));
            }
        }
    }
}