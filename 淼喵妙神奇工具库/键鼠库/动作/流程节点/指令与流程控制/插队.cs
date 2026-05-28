using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 插队 : 控制节点
    {
        public string 脚本路径 = "";

        插队() : base() { }

        public 插队(string 脚本路径) : base()
        {
            this.脚本路径 = 脚本路径;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (string.IsNullOrEmpty(脚本路径) || !File.Exists(脚本路径))
            {
                return false;
            }

            任务控制管理器.实例.触发插队请求(脚本路径);
            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@插队:\n" +
                   $"脚本路径[{脚本路径}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 插队 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@插队:"))
            {
                var 节点 = new 插队();
                Regex regex = new Regex(@"脚本路径\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.脚本路径 = match.Groups[1].Value;
                }
                return 节点;
            }
            return null;
        }

        public static 插队 创建节点(IntPtr hWnd)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "脚本文件 (*.script)|*.script|所有文件 (*.*)|*.*",
                Title = "选择要插队执行的脚本"
            };

            if (dialog.ShowDialog() == true)
            {
                return new 插队(dialog.FileName);
            }
            return null;
        }
    }
}
