using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 调整窗口尺寸 : 控制节点
    {
        public 节点参数<System.Drawing.Size> 窗口尺寸 = new 节点参数<System.Drawing.Size>(new System.Drawing.Size(800, 600));

        调整窗口尺寸() : base() { }

        public 调整窗口尺寸(节点参数<System.Drawing.Size> 窗口尺寸) : base()
        {
            this.窗口尺寸 = 窗口尺寸;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            try
            {
                if (hWnd != IntPtr.Zero)
                {
                    System.Drawing.Size 尺寸 = 从全局解析(窗口尺寸, 全局);
                    int 样式 = GetWindowLong(hWnd, GWL_STYLE);
                    bool 最大化 = (样式 & WS_MAXIMIZE) != 0;
                    
                    if (最大化)
                    {
                        ShowWindow(hWnd, SW_RESTORE);
                    }
                    
                    SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 尺寸.Width, 尺寸.Height, SWP_NOMOVE | SWP_NOZORDER);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@调整窗口尺寸:\n" +
                   $"窗口尺寸[{节点参数.序列化(窗口尺寸)}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 调整窗口尺寸 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@调整窗口尺寸:"))
            {
                var 节点 = new 调整窗口尺寸();
                Regex regex = new Regex(@"窗口尺寸\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.窗口尺寸 = 节点参数.反序列化<System.Drawing.Size>(match.Groups[1].Value);
                }
                return 节点;
            }
            return null;
        }

        public static 调整窗口尺寸 创建节点(IntPtr hWnd)
        {
            节点参数<System.Drawing.Size> 尺寸参数;
            if (通知工具.确认弹窗("是否使用固定窗口尺寸？"))
            {
                string 宽度输入 = 通知工具.输入弹窗("请输入窗口宽度:", "", "");
                string 高度输入 = 通知工具.输入弹窗("请输入窗口高度:", "", "");
                
                if (int.TryParse(宽度输入, out int 宽度) && int.TryParse(高度输入, out int 高度))
                {
                    尺寸参数 = new 节点参数<System.Drawing.Size>(new System.Drawing.Size(宽度, 高度));
                }
                else
                {
                    通知工具.吐司通知("无效的尺寸输入");
                    return null;
                }
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入窗口尺寸变量名:", "", "");
                尺寸参数 = new 节点参数<System.Drawing.Size>(default, 变量名);
            }
            return new 调整窗口尺寸(尺寸参数);
        }

        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZE = 0x01000000;
        private const int SW_RESTORE = 9;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    }
}