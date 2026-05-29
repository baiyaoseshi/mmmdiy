using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using 淼喵妙神奇工具库.感知库;

namespace 淼喵妙神奇工具库.键鼠库.监听.获取
{
    public abstract class 获取交互基类 : IDisposable
    {
        protected IntPtr _overlayWindow = IntPtr.Zero;
        protected Bitmap _screenSnapshot;
        protected Bitmap _overlayBitmap; // 分层窗口的显示位图
        protected bool _isCompleted;
        protected bool _获取成功;
        protected int _screenWidth;
        protected int _screenHeight;
        protected byte _maskAlpha = 100;
        protected Rectangle _lastDirtyRect = Rectangle.Empty;
        protected IntPtr _targetWindow = IntPtr.Zero;
        protected string _registeredClass;
        protected readonly AutoResetEvent _waitEvent = new AutoResetEvent(false);
        private Thread _uiThread;
        private WndProcDelegate _wndProcDelegate; // 保存委托引用，防止GC回收
        [DllImport("user32.dll")] protected static extern bool GetCursorPos(out Point lpPoint);
        [DllImport("user32.dll")] protected static extern IntPtr WindowFromPoint(Point point);
        [DllImport("user32.dll")] protected static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] protected static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [StructLayout(LayoutKind.Sequential)] protected struct RECT { public int Left, Top, Right, Bottom; }

        protected const uint GW_HWNDNEXT = 2;
        protected const int VK_LBUTTON = 0x01;

        protected 获取交互基类()
        {
            _screenWidth = GetSystemMetrics(0);  // SM_CXSCREEN
            _screenHeight = GetSystemMetrics(1); // SM_CYSCREEN
        }

        protected void 启动获取()
        {
            _isCompleted = false;
            _lastDirtyRect = Rectangle.Empty;

            // 截图前确保目标窗口处于前台，防止全屏截图拍到被遮挡的内容
            if (_targetWindow != IntPtr.Zero)
                窗口处理器.确保窗口前台(_targetWindow);

            _screenSnapshot = new Bitmap(_screenWidth, _screenHeight);
            using (var g = Graphics.FromImage(_screenSnapshot))
                g.CopyFromScreen(0, 0, 0, 0, new Size(_screenWidth, _screenHeight));

            _uiThread = new Thread(RunUIThread);
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.IsBackground = true;
            _uiThread.Start();
        }
        private void RunUIThread()
        {
            try
            {
                _registeredClass = $"CaptureWindow_{Thread.CurrentThread.ManagedThreadId}";
                IntPtr classNamePtr = IntPtr.Zero;
                try
                {
                    _wndProcDelegate = new WndProcDelegate(DefWindowProc);
                    classNamePtr = Marshal.StringToHGlobalUni(_registeredClass);
                    var wc = new WNDCLASSEX
                    {
                        cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                        style = CS_HREDRAW | CS_VREDRAW,
                        lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                        hInstance = GetModuleHandle(IntPtr.Zero),
                        hCursor = LoadCursor(IntPtr.Zero, IDC_CROSS),
                        lpszClassName = classNamePtr
                    };
                    if (RegisterClassEx(ref wc) == 0)
                        return;

                    _overlayWindow = CreateWindowEx(
                        WS_EX_LAYERED | WS_EX_TOPMOST,
                        _registeredClass, "", WS_POPUP,
                        0, 0, _screenWidth, _screenHeight,
                        IntPtr.Zero, IntPtr.Zero, GetModuleHandle(IntPtr.Zero), IntPtr.Zero);
                }
                finally
                {
                    if (classNamePtr != IntPtr.Zero)
                        Marshal.FreeHGlobal(classNamePtr);
                }

                if (_overlayWindow != IntPtr.Zero)
                {
                    ShowWindow(_overlayWindow, 3);
                    UpdateOverlayWindow();
                    _获取成功 = true;
                    MessageLoop();
                }
            }
            finally
            {
                CleanupResources();
                完成();
            }
        }

        private void MessageLoop()
        {
            bool pressed = false;
            while (!_isCompleted)
            {
                while (PeekMessage(out MSG msg, IntPtr.Zero, 0, 0, 1))
                {
                    DispatchMessage(ref msg);
                }

                if (GetCursorPos(out var pos))
                {
                    处理鼠标移动(pos);
                }

                var state = GetAsyncKeyState(VK_LBUTTON);
                bool down = (state & 0x8000) != 0;

                if (down && !pressed)
                {
                    pressed = true;
                    处理左键按下();
                }
                else if (!down && pressed)
                {
                    pressed = false;
                    处理左键释放();
                }

                Thread.Sleep(5);
            }
        }

        private void UpdateOverlayWindow()
        {
            if (_overlayWindow == IntPtr.Zero || _screenSnapshot == null) return;

            // 创建或更新overlay位图
            if (_overlayBitmap == null)
            {
                _overlayBitmap = new Bitmap(_screenWidth, _screenHeight);
            }

            using (var g = Graphics.FromImage(_overlayBitmap))
            {
                // 绘制屏幕快照
                g.DrawImage(_screenSnapshot, 0, 0);
                // 绘制蒙版
                using (var brush = new SolidBrush(Color.FromArgb(_maskAlpha, 0, 0, 0)))
                    g.FillRectangle(brush, 0, 0, _screenWidth, _screenHeight);
            }

            UpdateLayeredWindowFromBitmap();
        }

        private void UpdateLayeredWindowFromBitmap()
        {
            if (_overlayWindow == IntPtr.Zero || _overlayBitmap == null) return;

            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
            IntPtr hBitmap = _overlayBitmap.GetHbitmap();
            IntPtr hOld = SelectObject(hdcMem, hBitmap);

            var ptSrc = new POINT();
            var ptDst = new POINT();
            var size = new SIZE { cx = _screenWidth, cy = _screenHeight };
            var blend = new BLENDFUNCTION { SourceConstantAlpha = 255, AlphaFormat = 1 };

            UpdateLayeredWindow(_overlayWindow, hdcScreen, ref ptDst, ref size, hdcMem, ref ptSrc, 0, ref blend, 2);

            SelectObject(hdcMem, hOld);
            DeleteObject(hBitmap);
            DeleteDC(hdcMem);
            ReleaseDC(IntPtr.Zero, hdcScreen);
        }

        private void CleanupResources()
        {
            _screenSnapshot?.Dispose();
            _overlayBitmap?.Dispose();
            if (_overlayWindow != IntPtr.Zero)
            {
                DestroyWindow(_overlayWindow);
                _overlayWindow = IntPtr.Zero;
            }
            if (_registeredClass != null)
            {
                UnregisterClass(_registeredClass, GetModuleHandle(IntPtr.Zero));
                _registeredClass = null;
            }
        }

        public void 等待完成() => _waitEvent.WaitOne();

        protected abstract void 处理鼠标移动(Point pos);
        protected abstract void 处理左键按下();
        protected abstract void 处理左键释放();

        protected void 完成()
        {
            _isCompleted = true;
            _waitEvent.Set();
        }

        protected void 绘制原色彩圆形(Point center, int radius)
        {
            if (_isCompleted || _screenSnapshot == null) return;
            var rect = new Rectangle(
                Math.Max(0, center.X - radius), Math.Max(0, center.Y - radius),
                Math.Min(radius * 2, _screenWidth - (center.X - radius)),
                Math.Min(radius * 2, _screenHeight - (center.Y - radius)));
            DrawOriginalColorArea(rect);
            _lastDirtyRect = rect;
        }

        protected void 绘制原色彩矩形(Rectangle rect)
        {
            if (_isCompleted || _screenSnapshot == null || rect.Width <= 0 || rect.Height <= 0) return;
            DrawOriginalColorArea(rect);
            _lastDirtyRect = rect;
        }

        protected void 绘制原色彩窗口(IntPtr hwnd)
        {
            if (_isCompleted || _screenSnapshot == null || hwnd == IntPtr.Zero) return;
            if (GetWindowRect(hwnd, out var wr))
            {
                var rect = new Rectangle(wr.Left, wr.Top, wr.Right - wr.Left, wr.Bottom - wr.Top);
                DrawOriginalColorArea(rect);
                _lastDirtyRect = rect;
            }
        }
        Point 矩形顶点(Rectangle rect, int index)
        {
            switch (index)
            {
                case 0:
                    return new Point(rect.X, rect.Y);
                case 1:
                    return new Point(rect.X + rect.Width, rect.Y);
                case 2:
                    return new Point(rect.X + rect.Width, rect.Y + rect.Height);
                case 3:
                    return new Point(rect.X, rect.Y + rect.Height);
                default:
                    return new Point();
            }
        }
        Rectangle 对角矩形(Point p1, Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int width = Math.Abs(p1.X - p2.X);
            int height = Math.Abs(p1.Y - p2.Y);
            if (width == 0 || height == 0)
                return Rectangle.Empty;
            return new Rectangle(x, y, width, height);            
        }
        List<Rectangle> 矩形相减(Rectangle rect, Rectangle excludeRect)
        {
            if (rect.IsEmpty)
                return new List<Rectangle>();
            if (excludeRect.IsEmpty)
                return new List<Rectangle>(){rect};
            var res = new List<Rectangle>();
            int a = Math.Max(rect.X, excludeRect.X), b = Math.Max(rect.Y, excludeRect.Y),
            c = Math.Min(rect.X + rect.Width, excludeRect.X + excludeRect.Width) - a,
            d = Math.Min(rect.Y + rect.Height, excludeRect.Y + excludeRect.Height) - b;
            if (c <= 0 || d <= 0)
                return new List<Rectangle>(){rect};
            var uRect = new Rectangle(a, b, c, d);
            for (int i = 0; i < 4; i++)
            {
                var p1 = 矩形顶点(rect, i);
                var p2 = 矩形顶点(uRect, (i + 1) % 4);
                var temp = 对角矩形(p1, p2);
                if (temp.IsEmpty)
                    continue;
                res.Add(temp);
            }
            return res;
        }
        private void DrawOriginalColorArea(Rectangle rect)
        {
            if (_overlayBitmap == null || _screenSnapshot == null) return;

            using (var g = Graphics.FromImage(_overlayBitmap))
            {
                List<Rectangle> rects;
                // 1. 先恢复上一个脏区域（包括边框的1像素）
                if (!_lastDirtyRect.IsEmpty)
                {
                    using (var brush = new SolidBrush(Color.FromArgb(_maskAlpha, 0, 0, 0)))
                    {
                        rects = 矩形相减(_lastDirtyRect, rect);
                        foreach (var r in rects)
                            g.FillRectangle(brush, r);
                    }
                }

                // 2. 绘制原始颜色区域
                rects = 矩形相减(rect, _lastDirtyRect);
                foreach (var r in rects)
                    g.DrawImage(_screenSnapshot, r, r, GraphicsUnit.Pixel);
            }

            // 更新分层窗口（一次调用即可）
            UpdateLayeredWindowFromBitmap();
        }

        public void Dispose()
        {
            _isCompleted = true;

            if (_uiThread != null && _uiThread.IsAlive)
            {
                try { _uiThread.Join(3000); }
                catch { }
            }

            _waitEvent.Dispose();
        }

        #region Win32 API
        private const uint WS_EX_LAYERED = 0x80000;
        private const uint WS_EX_TOPMOST = 0x8;
        private const uint WS_EX_TRANSPARENT = 0x20;
        private const uint WS_POPUP = 0x80000000;
        private const uint CS_HREDRAW = 0x0002;
        private const uint CS_VREDRAW = 0x0001;
        private static readonly IntPtr IDC_CROSS = new IntPtr(32515);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra, cbWndExtra;
            public IntPtr hInstance, hIcon, hCursor, hbrBackground;
            public IntPtr lpszMenuName;
            public IntPtr lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE { public int cx, cy; }

        [StructLayout(LayoutKind.Sequential)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpWndClass);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
            int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize,
            IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr hInstance, IntPtr lpCursorName);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(IntPtr lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref MSG lpMsg);
        #endregion
    }
}