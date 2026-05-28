using System;
using System.Drawing;
using System.Runtime.InteropServices;
using 淼喵妙神奇工具库.感知库;

namespace 淼喵妙神奇工具库.键鼠库.监听.获取
{
    public class 获取窗口 : 获取交互基类
    {
        private IntPtr _resultHwnd;
        private IntPtr _currentHwnd;

        /// <summary>
        /// 获取根窗口句柄（顶层窗口）- WindowFromPoint 可能返回子窗口，
        /// 需通过 GetAncestor 获取其所属的顶层窗口，以确保标题/进程获取正确
        /// </summary>
        private static IntPtr 获取根窗口(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return IntPtr.Zero;
            return GetAncestor(hWnd, 2 /*GA_ROOT*/);
        }

        public IntPtr 获取窗口句柄()
        {
            启动获取();
            等待完成();
            if (!_获取成功)
            {
                Dispose();
                return IntPtr.Zero;
            }
            var result = _resultHwnd;
            Dispose();
            return result;
        }

        protected override void 处理鼠标移动(Point pos)
        {            
            var hwnd = WindowFromPoint(pos);
            // 滤镜窗口移除 WS_EX_TRANSPARENT 后会拦截鼠标，
            // WindowFromPoint 可能返回滤镜窗口自身，需用 Z序遍历绕过
            if (hwnd == _overlayWindow)
                hwnd = 获取滤镜下方窗口(pos);
            if (hwnd != _currentHwnd)
            {
                // 获取顶层根窗口，避免获取到子窗口导致窗口标题/信息错误
                _currentHwnd = 获取根窗口(hwnd);
                绘制原色彩窗口(_currentHwnd);
            }
        }

        /// <summary>
        /// 通过 Z序遍历绕过自身滤镜窗口，获取鼠标下方的实际窗口句柄
        /// </summary>
        /// <param name="pos">鼠标屏幕坐标</param>
        /// <returns>鼠标下方第一个可见窗口的句柄，未找到返回 IntPtr.Zero</returns>
        private IntPtr 获取滤镜下方窗口(Point pos)
        {
            IntPtr hwndNext = GetWindow(_overlayWindow, GW_HWNDNEXT);
            while (hwndNext != IntPtr.Zero)
            {
                if (IsWindowVisible(hwndNext) && GetWindowRect(hwndNext, out RECT rect))
                {
                    if (pos.X >= rect.Left && pos.X < rect.Right &&
                        pos.Y >= rect.Top && pos.Y < rect.Bottom)
                    {
                        return hwndNext;
                    }
                }
                hwndNext = GetWindow(hwndNext, GW_HWNDNEXT);
            }
            return IntPtr.Zero;
        }

        protected override void 处理左键按下() { }

        protected override void 处理左键释放()
        {
            _resultHwnd = _currentHwnd;
            完成();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
    }
}