using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 提示弹窗 : 控制节点
    {
        public override bool 需要窗口句柄 => false;

        public string 弹窗类型 = "信息";
        public string 提示信息 = "";

        提示弹窗() : base() { }

        public 提示弹窗(string 弹窗类型, string 提示信息) : base()
        {
            this.弹窗类型 = 弹窗类型;
            this.提示信息 = 提示信息;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            switch (弹窗类型)
            {
                case "信息":
                    通知工具.信息弹窗(提示信息);
                    break;
                case "吐司":
                    通知工具.吐司通知(提示信息);
                    break;
                case "错误":
                    通知工具.错误弹窗(提示信息);
                    break;
                case "警告":
                    通知工具.警告弹窗(提示信息);
                    break;
            }
            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@提示弹窗:\n" +
                   $"弹窗类型[{弹窗类型}],\n" +
                   $"提示信息[{提示信息}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 提示弹窗 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@提示弹窗:"))
            {
                var 节点 = new 提示弹窗();
                Regex regex = new Regex(@"弹窗类型\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                    节点.弹窗类型 = match.Groups[1].Value;
                regex = new Regex(@"提示信息\[([^\]]+(?:\]\[[^\]]+)*)\]");
                match = regex.Match(字符串);
                if (match.Success)
                    节点.提示信息 = match.Groups[1].Value;
                return 节点;
            }
            return null;
        }

        public static 提示弹窗 创建节点(IntPtr hWnd)
        {
            string 弹窗类型 = 通知工具.选项弹窗("请选择弹窗类型:", new List<string> { "信息", "吐司", "错误", "警告" });
            if (string.IsNullOrEmpty(弹窗类型))
                return null;
            string 提示信息 = 通知工具.输入弹窗("请输入提示信息:", "提示弹窗", "");
            return new 提示弹窗(弹窗类型, 提示信息);
        }
    }
}
