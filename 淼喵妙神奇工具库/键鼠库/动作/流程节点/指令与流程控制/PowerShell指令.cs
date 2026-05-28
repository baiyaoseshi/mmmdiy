using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class PowerShell指令 : 控制节点
    {
        public string 指令 = "";
        public string 输出变量名 = "";

        PowerShell指令() : base() { }

        public PowerShell指令(string 指令, string 输出变量名 = "") : base()
        {
            this.指令 = 指令;
            this.输出变量名 = 输出变量名;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = $"-Command \"{指令}\"";
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                using (Process process = Process.Start(psi))
                {
                    string 输出 = process.StandardOutput.ReadToEnd();
                    string 错误 = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(输出变量名))
                    {
                        全局[输出变量名] = 输出.Trim();
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@PowerShell指令:\n" +
                   $"指令[{指令}],\n" +
                   $"输出变量名[{输出变量名}],\n" +
                   base.保存为字符串(脚本);
        }

        public static PowerShell指令 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@PowerShell指令:"))
            {
                var 节点 = new PowerShell指令();
                Regex regex = new Regex(@"指令\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.指令 = match.Groups[1].Value;
                }
                regex = new Regex(@"输出变量名\[([^\]]*)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.输出变量名 = match.Groups[1].Value;
                }
                return 节点;
            }
            return null;
        }

        public static PowerShell指令 创建节点(IntPtr hWnd)
        {
            string 指令 = 通知工具.输入弹窗("请输入PowerShell指令:", "", "");
            string 输出变量名 = 通知工具.输入弹窗("请输入输出变量名(可选):", "", "");
            return new PowerShell指令(指令, 输出变量名);
        }
    }
}