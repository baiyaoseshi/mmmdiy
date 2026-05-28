using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 远程指令 : 控制节点
    {
        public override bool 需要窗口句柄 => false;

        public string 变量名 = "远程指令";
        public int 超时秒数 = 30;

        远程指令() : base() { }

        public 远程指令(string 变量名, int 超时秒数) : base()
        {
            this.变量名 = 变量名;
            this.超时秒数 = 超时秒数;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            int 超时毫秒 = 超时秒数 > 0 ? 超时秒数 * 1000 : -1;
            if (远程指令服务器.取计划指令(超时毫秒, out string 指令))
            {
                全局[变量名] = 指令;
                return true;
            }
            return false;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@远程指令:\n" +
                   $"变量名[{变量名}],\n" +
                   $"超时秒数[{超时秒数}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 远程指令 解析(string 字符串, IntPtr hWnd)
        {
            if (!字符串.StartsWith("@远程指令:")) return null;

            var 节点 = new 远程指令();

            var 变量名匹配 = Regex.Match(字符串, @"变量名\[([^\]]+)\]");
            if (变量名匹配.Success) 节点.变量名 = 变量名匹配.Groups[1].Value;

            var 超时匹配 = Regex.Match(字符串, @"超时秒数\[([^\]]+)\]");
            if (超时匹配.Success && int.TryParse(超时匹配.Groups[1].Value, out int t))
                节点.超时秒数 = t;

            return 节点;
        }

        public static 远程指令 创建节点(IntPtr hWnd)
        {
            string 变量名 = 通知工具.输入弹窗("请输入接收变量名（默认：远程指令）:", "", "");
            if (string.IsNullOrEmpty(变量名)) 变量名 = "远程指令";

            string 超时秒数Str = 通知工具.输入弹窗("请输入超时秒数（0=不超时，默认：30）:", "", "");
            if (!int.TryParse(超时秒数Str, out int 超时秒数)) 超时秒数 = 30;

            return new 远程指令(变量名, 超时秒数);
        }
    }
}
