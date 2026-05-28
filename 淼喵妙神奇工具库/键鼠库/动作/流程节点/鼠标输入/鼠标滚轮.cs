using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 鼠标滚轮 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 节点参数<int> 滚动量 = new 节点参数<int>(1);

        鼠标滚轮() : base()
        {
        }

        public 鼠标滚轮(节点参数<int> 滚动量) : base()
        {
            this.滚动量 = 滚动量;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            var 控制器 = new 鼠标控制器(hWnd);
            控制器.滚轮(从全局解析(滚动量, 全局));
            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@鼠标滚轮:\n" +
                   $"滚动量[{节点参数.序列化(滚动量)}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 鼠标滚轮 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@鼠标滚轮:"))
            {
                var 节点 = new 鼠标滚轮();
                Regex regex = new Regex(@"滚动量\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.滚动量 = 节点参数.反序列化<int>(match.Groups[1].Value);
                }
                return 节点;
            }
            return null;
        }

        public static 鼠标滚轮 创建节点(IntPtr hWnd)
        {
            节点参数<int> 滚动量参数;
            if (通知工具.确认弹窗("是否使用固定滚动量？"))
            {
                string 输入 = 通知工具.输入弹窗("请输入滚动量(正数向上,负数向下):", "", "");
                int 滚动量值 = 1;
                if (!string.IsNullOrEmpty(输入))
                    int.TryParse(输入, out 滚动量值);
                滚动量参数 = new 节点参数<int>(滚动量值);
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入滚动量变量名:", "", "");
                滚动量参数 = new 节点参数<int>(default, 变量名);
            }
            return new 鼠标滚轮(滚动量参数);
        }
    }
}