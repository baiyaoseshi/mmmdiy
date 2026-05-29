using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;

namespace 淼喵妙神奇工具库.感知库
{
    public static partial class 窗口处理器
    {
        private static string 部分标题 = "";
        private static List<Process> 进程列表 = new List<Process>();
        public static List<Process> 检索窗口进程(string 部分标题)
        {
            窗口处理器.部分标题 = 部分标题;
            进程列表 = new List<Process>();
            EnumWindows(new EnumWindowsProc(EnumWindowsCallback), new IntPtr(Marshal.GetFunctionPointerForDelegate(new EnumWindowsProc(EnumWindowsCallback))));
            return 进程列表;
        }
        public static string 获取应用程序路径(Process 进程)
        {
            return 进程.MainModule?.FileName ?? "";
        }
        public static List<Process> 检索进程(string 路径)
        {
            进程列表 = new List<Process>();
            Process[] 进程数组 = Process.GetProcesses();
            foreach (Process 进程 in 进程数组)
                if (进程.MainModule?.FileName.Equals(路径) ?? false)
                    进程列表.Add(进程);
            return 进程列表;
        }

        private const int GWL_STYLE = -16;
        private const int WS_VISIBLE = 0x10000000;
        private const int WS_MINIMIZE = 0x20000000;
        public static bool 激活窗口(Process 进程)
        {
            IntPtr hWnd = 进程.MainWindowHandle;
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
                return false;

            IntPtr foregroundWindow = GetForegroundWindow();
            IntPtr style = GetWindowLongPtr(hWnd, GWL_STYLE);
            long styleValue = style.ToInt64();
            if (foregroundWindow == hWnd
                && (styleValue & WS_VISIBLE) != 0 && (styleValue & WS_MINIMIZE) == 0)
                return true;
            if ((styleValue & WS_VISIBLE) == 0 || (styleValue & WS_MINIMIZE) != 0)
                return false;
            // 尝试激活窗口
            bool result = SetForegroundWindow(hWnd);
            return result;
        }

        /// <summary>
        /// 确保目标窗口处于前台（置顶），如果目标窗口存在且不是当前前台窗口，
        /// 则先调用 SetForegroundWindow 置顶并等待 100ms 让窗口完成置顶动画
        /// </summary>
        /// <param name="hWnd">目标窗口句柄</param>
        public static void 确保窗口前台(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
                return;

            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow != hWnd)
            {
                SetForegroundWindow(hWnd);
                Thread.Sleep(100);
            }
        }

        public static void 初始化屏幕参数(IntPtr hWnd)
        {
            double dpi = -1;
            if (Environment.OSVersion.Version >= new Version(6, 3))
            {
                IntPtr monitor = MonitorFromWindow(hWnd, 2 /*MONITOR_DEFAULTTONEAREST*/);
                if (monitor != IntPtr.Zero)
                {
                    uint dpiX, dpiY;
                    if (GetDpiForMonitor(monitor, MonitorDpiType.MDT_Effective_DPI, out dpiX, out dpiY) == 0)
                    {
                        dpi = dpiX / 96.0; // 96是100%缩放的标准DPI
                    }
                }
            }
            if (dpi < 0)
            {
                输出库.通知工具.信息弹窗("无法获取DPI缩放比例");
                throw new InvalidOperationException("无法获取DPI缩放比例");
            }
            screenHeight = SystemParameters.PrimaryScreenHeight * dpi;
            screenWidth = SystemParameters.PrimaryScreenWidth * dpi;
        }
        public static Rectangle 获取窗口框(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero || !IsWindow(hWnd))
            {
                return Rectangle.Empty;
            }

            if (GetWindowRect(hWnd, out RECT rect))
            {
                return new Rectangle(
                    rect.Left,
                    rect.Top ,
                    rect.Right - rect.Left,
                    rect.Bottom - rect.Top
                );
            }

            int error = Marshal.GetLastWin32Error();
            淼喵妙神奇工具库.输出库.通知工具.信息弹窗($"获取窗口矩形失败，错误代码: 0x{error:X8}");
            return Rectangle.Empty;
        }
        public static string 获取窗口标题(IntPtr hWnd)
        {
            StringBuilder 窗口标题 = new StringBuilder(256);
            int 长度 = GetWindowText(hWnd, 窗口标题, 窗口标题.Capacity);
            if (长度 > 0)
                return 窗口标题.ToString();
            return "";
        }
        
        public static IntPtr 查找窗口(string 窗口标题)
        {
            if (string.IsNullOrEmpty(窗口标题))
                return IntPtr.Zero;

            IntPtr 找到的窗口 = IntPtr.Zero;
            
            EnumWindows((hWnd, lParam) =>
            {
                string 当前标题 = 获取窗口标题(hWnd);
                if (!string.IsNullOrEmpty(当前标题) && 当前标题.Contains(窗口标题) && IsWindowVisible(hWnd))
                {
                    找到的窗口 = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            
            return 找到的窗口;
        }
        
        private static bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            StringBuilder 窗口标题 = new StringBuilder(256);
            int 长度 = GetWindowText(hWnd, 窗口标题, 窗口标题.Capacity);

            if (长度 > 0 && 窗口标题.ToString().Contains(部分标题) && IsWindowVisible(hWnd) && !IsIconic(hWnd))
            {
                uint 进程ID;
                GetWindowThreadProcessId(hWnd, out 进程ID);

                Process 进程 = Process.GetProcessById((int)进程ID);
                if (!进程列表.Contains(进程))
                    进程列表.Add(进程);
            }

            return true;
        }
    }
}
