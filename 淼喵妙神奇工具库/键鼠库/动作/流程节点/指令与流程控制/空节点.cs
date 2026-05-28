using System;
using System.Collections.Generic;
using System.Text;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 空节点 : 控制节点
    {
        public override bool 需要窗口句柄 => false;

        public 空节点() : base()
        {
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            return true;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@空节点:\n" + base.保存为字符串(脚本);
        }

        public static 空节点 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@空节点:"))
                return new 空节点();
            return null;
        }

        public static 空节点 创建节点(IntPtr hWnd)
        {
            return new 空节点();
        }
    }
}
