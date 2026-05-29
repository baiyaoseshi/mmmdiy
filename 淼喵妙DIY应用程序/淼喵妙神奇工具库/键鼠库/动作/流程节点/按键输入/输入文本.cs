using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 输入文本 : 控制节点
    {
        public 节点参数<string> 文本 = new 节点参数<string>();

        输入文本() : base()
        {
        }

        public 输入文本(节点参数<string> 文本) : base()
        {
            this.文本 = 文本;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            按键控制器.输入文本(从全局解析(文本, 全局));
            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@输入文本:\n" +
                   $"文本[{节点参数.序列化(文本)}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 输入文本 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@输入文本:"))
            {
                var 节点 = new 输入文本();
                Regex regex = new Regex(@"文本\[([^\]]*)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.文本 = 节点参数.反序列化<string>(match.Groups[1].Value);
                }
                return 节点;
            }
            return null;
        }

        public static 输入文本 创建节点(IntPtr hWnd)
        {
            if (通知工具.确认弹窗("是否使用固定文本？"))
            {
                string 文本值 = 通知工具.输入弹窗("请输入要输入的文本:", "", "");
                return new 输入文本(new 节点参数<string>(文本值));
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入文本变量名:", "", "");
                return new 输入文本(new 节点参数<string>(default, 变量名));
            }
        }
    }
}