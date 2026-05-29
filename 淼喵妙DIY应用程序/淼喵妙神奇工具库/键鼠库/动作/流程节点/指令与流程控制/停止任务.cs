using System;
using System.Collections.Generic;
using System.Text;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 停止任务 : 控制节点
    {
        public 停止任务() : base()
        {
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@停止任务:\n";
        }

        public static 停止任务 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@停止任务:"))
                return new 停止任务();
            return null;
        }

        public static 停止任务 创建节点(IntPtr hWnd)
        {
            return new 停止任务();
        }
    }
}