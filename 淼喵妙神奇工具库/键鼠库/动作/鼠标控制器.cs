using OpenCvSharp.Dnn;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using WindowsInput;
using 淼喵妙神奇工具库.感知库;
using Point = System.Drawing.Point;

namespace 淼喵妙神奇工具库.键鼠库.动作
{
    public class 鼠标控制器
    {
        internal static InputSimulator inputSimulator = new InputSimulator();
        IntPtr hWnd;
        public 鼠标控制器(IntPtr hWnd)
        {
            窗口处理器.初始化屏幕参数(hWnd);
            this.hWnd = hWnd;
        }
        public bool 移动鼠标(Point 相对坐标)
        {
            if (相对坐标.X >= 0 && 相对坐标.Y >= 0)
            {
                var 窗口框 = 窗口处理器.获取窗口框(hWnd);
                if (相对坐标.X > 窗口框.Width || 相对坐标.Y > 窗口框.Height)
                    return false;
                var target = new Point(相对坐标.X + 窗口框.X, 相对坐标.Y + 窗口框.Y);
                inputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop(target.X * 65535 / 窗口处理器.screenWidth, target.Y * 65535 / 窗口处理器.screenHeight);
                return true;
            }
            return false;
        }
        public void 左键单击(Point 相对坐标)
        {
            if (移动鼠标(相对坐标))
                inputSimulator.Mouse.LeftButtonClick();
        }
        public void 双击(Point 相对坐标)
        {
            if (移动鼠标(相对坐标))
                inputSimulator.Mouse.LeftButtonDoubleClick();
        }
        public void 右键单击(Point 相对坐标)
        {
            if (移动鼠标(相对坐标))
                inputSimulator.Mouse.RightButtonClick();
        }
        public void 中键单击(Point 相对坐标)
        {
            if (移动鼠标(相对坐标))
                inputSimulator.Mouse.MiddleButtonClick();
        }
        public void 拖拽(Point 目标, Point 终点)
        {
            // 移动鼠标到目标位置
            inputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop(目标.X * 65535 / 窗口处理器.screenWidth, 目标.Y * 65535 / 窗口处理器.screenHeight);
            // 按下左键
            inputSimulator.Mouse.LeftButtonDown();
            Thread.Sleep(100);
            // 移动鼠标到终点位置
            inputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop(终点.X * 65535 / 窗口处理器.screenWidth, 终点.Y * 65535 / 窗口处理器.screenHeight);
            Thread.Sleep(100);
            // 释放左键
            inputSimulator.Mouse.LeftButtonUp();
        }
        public void 滚轮(int 滚动量)
        {
            inputSimulator.Mouse.VerticalScroll(滚动量);
        }
        public void 长按左键(Point 相对坐标)
        {
            if (移动鼠标(相对坐标))
                inputSimulator.Mouse.LeftButtonDown();
        }
        public void 释放左键() => inputSimulator.Mouse.LeftButtonUp();
        public void 长按右键(Point 相对坐标)
        {
            if (移动鼠标(相对坐标))
                inputSimulator.Mouse.RightButtonDown();
        }
        public void 释放右键() => inputSimulator.Mouse.RightButtonUp();
        public void 长按中键(Point 相对坐标)
        {
            if (移动鼠标(相对坐标))
                inputSimulator.Mouse.MiddleButtonDown();
        }
        public void 释放中键() => inputSimulator.Mouse.MiddleButtonUp();

    }
}
