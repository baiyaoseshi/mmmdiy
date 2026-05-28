using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.感知库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 绑定运行应用 : 控制节点
    {
        public string 应用路径 = "";
        public string 启动参数 = "";
        public string 窗口标题 = "";
        public int 等待超时 = 30;
        public IntPtr hWnd = IntPtr.Zero;

        绑定运行应用() : base() { }

        public 绑定运行应用(string 应用路径, string 窗口标题, string 启动参数 = "", int 等待超时 = 30) : base()
        {
            this.应用路径 = 应用路径;
            this.窗口标题 = 窗口标题;
            this.启动参数 = 启动参数;
            this.等待超时 = 等待超时;
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

                if (!string.IsNullOrEmpty(窗口标题))
                {
                    return 等待窗口出现(窗口标题, 等待超时);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool 等待窗口出现(string 标题, int 超时秒)
        {
            int 等待次数 = 0;
            int 每次等待毫秒 = 50;
            int 最大等待次数 = (超时秒 * 1000) / 每次等待毫秒;

            while (等待次数 < 最大等待次数)
            {
                hWnd = 窗口处理器.查找窗口(标题);
                if (hWnd != IntPtr.Zero)
                {
                    return true;
                }
                Thread.Sleep(每次等待毫秒);
                等待次数++;
            }
            return false;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@绑定运行应用:\n" +
                   $"应用路径[{应用路径}],\n" +
                   $"窗口标题[{窗口标题}],\n" +
                   $"启动参数[{启动参数}],\n" +
                   $"等待超时[{等待超时}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 绑定运行应用 解析(string 字符串)
        {
            if (字符串.StartsWith("@绑定运行应用:"))
            {
                var 节点 = new 绑定运行应用();
                Regex regex = new Regex(@"应用路径\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.应用路径 = match.Groups[1].Value;
                }
                regex = new Regex(@"窗口标题\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.窗口标题 = match.Groups[1].Value;
                }
                regex = new Regex(@"启动参数\[([^\]]*)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.启动参数 = match.Groups[1].Value;
                }
                regex = new Regex(@"等待超时\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    int.TryParse(match.Groups[1].Value, out 节点.等待超时);
                }
                return 节点;
            }
            return null;
        }

        public static 绑定运行应用 创建节点(IntPtr hWnd)
        {
            string 应用路径 = 通知工具.输入弹窗("请输入应用路径:", "", "");
            string 窗口标题 = 通知工具.输入弹窗("请输入要等待的窗口标题:", "", "");
            string 启动参数 = 通知工具.输入弹窗("请输入启动参数(可选):", "", "");
            string 超时输入 = 通知工具.输入弹窗("请输入等待超时时间(秒，默认30):", "", "");
            int 超时 = string.IsNullOrEmpty(超时输入) ? 30 : int.Parse(超时输入);
            return new 绑定运行应用(应用路径, 窗口标题, 启动参数, 超时);
        }
    }
}