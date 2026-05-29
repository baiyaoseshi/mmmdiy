using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WindowsInput;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 快捷键 : 控制节点
    {
        public 节点参数<string> 按键组合 = new 节点参数<string>();

        快捷键() : base()
        {
        }

        public 快捷键(节点参数<string> 按键组合) : base()
        {
            this.按键组合 = 按键组合;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            string 组合值 = 从全局解析(按键组合, 全局);
            string[] parts = 组合值.Split('|');
            VirtualKeyCode 主键 = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), parts[0]);
            VirtualKeyCode? 修改键1 = parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) ? (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), parts[1]) : (VirtualKeyCode?)null;
            VirtualKeyCode? 修改键2 = parts.Length > 2 && !string.IsNullOrEmpty(parts[2]) ? (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), parts[2]) : (VirtualKeyCode?)null;
            VirtualKeyCode? 修改键3 = parts.Length > 3 && !string.IsNullOrEmpty(parts[3]) ? (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), parts[3]) : (VirtualKeyCode?)null;

            if (全局.ContainsKey($"按键锁定_{主键}") && (bool)全局[$"按键锁定_{主键}"])
            {
                return false;
            }
            if (修改键1.HasValue && 全局.ContainsKey($"按键锁定_{修改键1.Value}") && (bool)全局[$"按键锁定_{修改键1.Value}"])
            {
                return false;
            }
            if (修改键2.HasValue && 全局.ContainsKey($"按键锁定_{修改键2.Value}") && (bool)全局[$"按键锁定_{修改键2.Value}"])
            {
                return false;
            }
            if (修改键3.HasValue && 全局.ContainsKey($"按键锁定_{修改键3.Value}") && (bool)全局[$"按键锁定_{修改键3.Value}"])
            {
                return false;
            }

            if (修改键1.HasValue && 修改键2.HasValue && 修改键3.HasValue)
            {
                按键控制器.快捷键(主键, 修改键1.Value, 修改键2.Value, 修改键3.Value);
            }
            else if (修改键1.HasValue && 修改键2.HasValue)
            {
                按键控制器.快捷键(主键, 修改键1.Value, 修改键2.Value);
            }
            else if (修改键1.HasValue)
            {
                按键控制器.快捷键(主键, 修改键1.Value);
            }
            else
            {
                按键控制器.单击键(主键);
            }
            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@快捷键:\n" +
                   $"按键组合[{节点参数.序列化(按键组合)}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 快捷键 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@快捷键:"))
            {
                var 节点 = new 快捷键();
                Regex regex = new Regex(@"按键组合\[([^\]]*)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.按键组合 = 节点参数.反序列化<string>(match.Groups[1].Value);
                }
                return 节点;
            }
            return null;
        }

        public static 快捷键 创建节点(IntPtr hWnd)
        {
            if (通知工具.确认弹窗("是否使用固定按键组合？"))
            {
                通知工具.吐司通知("请按下快捷键组合(如Ctrl+C)...");
                var 监听 = new 按键监听();
                var 按下的键 = new List<VirtualKeyCode>();
                监听.注册事件处理程序(按键监听.键盘事件.按下, (ref 按键监听.KeyboardHookStruct k) =>
                {
                    var vk = k.GetVirtualKeyCode();
                    if (!按下的键.Contains(vk))
                        按下的键.Add(vk);
                });
                var 释放的键 = new List<VirtualKeyCode>();
                监听.注册事件处理程序(按键监听.键盘事件.释放, (ref 按键监听.KeyboardHookStruct k) =>
                {
                    var vk = k.GetVirtualKeyCode();
                    if (!释放的键.Contains(vk))
                        释放的键.Add(vk);
                });
                监听.安装钩子();
                while (按下的键.Count == 0 || 按下的键.Count > 释放的键.Count)
                {
                    System.Threading.Thread.Sleep(10);
                }
                监听.卸载钩子();

                string 组合值;
                if (按下的键.Count == 1)
                    组合值 = $"{按下的键[0]}|||";
                else if (按下的键.Count == 2)
                    组合值 = $"{按下的键[1]}|{按下的键[0]}||";
                else if (按下的键.Count == 3)
                    组合值 = $"{按下的键[2]}|{按下的键[0]}|{按下的键[1]}|";
                else
                    组合值 = $"{按下的键[^1]}|{按下的键[0]}|{按下的键[1]}|{按下的键[2]}";

                return new 快捷键(new 节点参数<string>(组合值));
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入按键组合变量名:", "", "");
                return new 快捷键(new 节点参数<string>(default, 变量名));
            }
        }
    }
}