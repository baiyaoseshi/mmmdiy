using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsInput;
using static 淼喵妙神奇工具库.键鼠库.监听.按键监听;

namespace 淼喵妙神奇工具库.键鼠库.监听
{
    public class 按键监听 : IDisposable
    {
        public delegate void 按键事件处理程序(ref KeyboardHookStruct keyboardStruct);

        [StructLayout(LayoutKind.Sequential)]
        public struct KeyboardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
            public VirtualKeyCode GetVirtualKeyCode()
            {
                return (VirtualKeyCode)vkCode;
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private Dictionary<uint, List<按键事件处理程序>> 按键事件处理程序字典;
        private bool isDisposed = false;
        private Thread? _monitorThread;
        private bool _isRunning = false;

        public 按键监听()
        {
            按键事件处理程序字典 = new Dictionary<uint, List<按键事件处理程序>>();
        }

        public void 安装钩子()
        {
            if (_isRunning) return;
            _isRunning = true;
            _monitorThread = new Thread(MonitorKeys);
            _monitorThread.IsBackground = true;
            _monitorThread.Start();
        }

        public void 卸载钩子()
        {
            _isRunning = false;
            if (_monitorThread != null)
            {
                _monitorThread.Join(100);
                _monitorThread = null;
            }
        }

        private void MonitorKeys()
        {
            var keyStates = new Dictionary<int, bool>();
            for (int i = 0; i <= 255; i++)
            {
                keyStates[i] = (GetAsyncKeyState(i) & 0x8000) != 0;
            }

            while (_isRunning && !isDisposed)
            {
                for (int vkCode = 0; vkCode <= 255; vkCode++)
                {
                    // 跳过鼠标按键，只监听键盘按键
                    // 鼠标按键虚拟键码：0x01(左键), 0x02(右键), 0x04(中键), 0x05(侧键1), 0x06(侧键2)
                    if (vkCode >= 0x01 && vkCode <= 0x06)
                    {
                        continue;
                    }

                    bool isPressed = (GetAsyncKeyState(vkCode) & 0x8000) != 0;
                    if (isPressed != keyStates[vkCode])
                    {
                        keyStates[vkCode] = isPressed;

                        uint eventType = isPressed ? (uint)键盘事件.按下 : (uint)键盘事件.释放;

                        List<按键事件处理程序>? handlers = null;
                        lock (按键事件处理程序字典)
                        {
                            if (按键事件处理程序字典.TryGetValue(eventType, out var list))
                            {
                                handlers = new List<按键事件处理程序>(list);
                            }
                        }

                        if (handlers != null && handlers.Count > 0)
                        {
                            var keyboardStruct = new KeyboardHookStruct
                            {
                                vkCode = vkCode,
                                time = Environment.TickCount
                            };

                            foreach (var handler in handlers)
                            {
                                try
                                {
                                    handler(ref keyboardStruct);
                                }
                                catch { }
                            }
                        }
                    }
                }
                Thread.Sleep(10);
            }
        }

        public void 注册事件处理程序(键盘事件 事件, 按键事件处理程序 处理程序)
        {
            uint keyEvent = (uint)事件;
            lock (按键事件处理程序字典)
            {
                if (!按键事件处理程序字典.ContainsKey(keyEvent))
                {
                    按键事件处理程序字典[keyEvent] = new List<按键事件处理程序>();
                }
                按键事件处理程序字典[keyEvent].Add(处理程序);
            }
        }

        public enum 键盘事件 : uint
        {
            按下 = 0x0100,
            释放 = 0x0101,
            系统按下 = 0x0104,
            系统释放 = 0x0105,
        }

        public void Dispose()
        {
            isDisposed = true;
            卸载钩子();
        }
    }
}