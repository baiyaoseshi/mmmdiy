using System;
using System.Collections.Generic;
using 淼喵妙神奇工具库.感知库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 读取标题 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 读取标题() : base() { }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            string 标题 = 窗口处理器.获取窗口标题(hWnd);
            全局["窗口标题"] = 标题;
            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@读取标题:\n" + base.保存为字符串(脚本);
        }

        public static 读取标题 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@读取标题:"))
                return new 读取标题();
            return null;
        }

        public static 读取标题 创建节点(IntPtr hWnd)
        {
            return new 读取标题();
        }
    }
}
