using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 打开应用 : 控制节点
    {
        public string 应用路径 = "";
        public string 启动参数 = "";

        打开应用() : base() { }

        public 打开应用(string 应用路径, string 启动参数 = "") : base()
        {
            this.应用路径 = 应用路径;
            this.启动参数 = 启动参数;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = 应用路径;
                psi.Arguments = 启动参数;
                psi.UseShellExecute = true;
                Process.Start(psi);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@打开应用:\n" +
                   $"应用路径[{应用路径}],\n" +
                   $"启动参数[{启动参数}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 打开应用 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@打开应用:"))
            {
                var 节点 = new 打开应用();
                Regex regex = new Regex(@"应用路径\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.应用路径 = match.Groups[1].Value;
                }
                regex = new Regex(@"启动参数\[([^\]]*)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.启动参数 = match.Groups[1].Value;
                }
                return 节点;
            }
            return null;
        }

        public static 打开应用 创建节点(IntPtr hWnd)
        {
            string 应用路径 = 通知工具.输入弹窗("请输入应用路径:", "", "");
            string 启动参数 = 通知工具.输入弹窗("请输入启动参数(可选):", "", "");
            return new 打开应用(应用路径, 启动参数);
        }
    }
}