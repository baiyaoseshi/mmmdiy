using System;
using System.Diagnostics;
using 淼喵妙神奇工具库.感知库;

namespace 淼喵妙用户界面.Helpers
{
    /// <summary>
    /// 窗口查找辅助类 - 封装与窗口查找相关的底层操作
    /// </summary>
    public static class WindowFinder
    {
        /// <summary>
        /// 查找窗口的 Win32 API
        /// </summary>
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <summary>
        /// 根据进程名称查找窗口句柄
        /// </summary>
        /// <param name="processName">进程名称（不含.exe）</param>
        /// <returns>窗口句柄，未找到返回 IntPtr.Zero</returns>
        public static IntPtr FindWindowByProcessName(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return process.MainWindowHandle;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 根据窗口标题查找窗口句柄
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <returns>窗口句柄，未找到返回 IntPtr.Zero</returns>
        public static IntPtr FindWindowByTitle(string title)
        {
            return 窗口处理器.查找窗口(title);
        }

        /// <summary>
        /// 查找匹配指定标题的窗口句柄
        /// </summary>
        /// <param name="title">窗口标题</param>
        /// <returns>窗口句柄，未找到返回 IntPtr.Zero</returns>
        public static IntPtr FindWindowByTitleExact(string title)
        {
            return FindWindow(null, title);
        }
    }
}