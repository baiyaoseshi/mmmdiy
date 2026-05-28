using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using 淼喵妙神奇工具库.感知库;

namespace 淼喵妙神奇工具库.感知库.识别
{
    public class 取样感应机 : 通用图像处理
    {
        protected int 取样边距 = 8;
        (int, int, Color?)[,] 取点坐标表;
        protected Size 窗口框尺寸;
        public 取样感应机(Size 窗口框尺寸, int 取样边距)
        {
            this.取样边距 = 取样边距;
            this.窗口框尺寸 = 窗口框尺寸;
            取点坐标表 = new (int, int, Color?)[窗口框尺寸.Width / 取样边距, 窗口框尺寸.Height / 取样边距];
            Parallel.For(0, 取点坐标表.GetLength(0), x =>
            {
                for (int y = 0; y < 取点坐标表.GetLength(1); y++)
                    取点坐标表[x, y] = (x * 取样边距 + Random.Shared.Next(0, 取样边距 - 1), y * 取样边距 + Random.Shared.Next(0, 取样边距 - 1), null);
            });
        }
        public bool 变化(Rectangle 相对区域, IntPtr hWnd)
        {
            Rectangle 窗口框 = 窗口处理器.获取窗口框(hWnd);
            if (窗口框 == Rectangle.Empty)
            {
                输出库.通知工具.信息弹窗("未找到窗口");
                throw new Exception("未找到窗口");
            }
            // 截图前确保目标窗口处于前台，防止截图到被遮挡的内容
            窗口处理器.确保窗口前台(hWnd);
            int x0 = (int)Math.Ceiling(相对区域.X / (double)取样边距), x1 = (相对区域.X + 相对区域.Width) / 取样边距;
            int y0 = (int)Math.Ceiling(相对区域.Y / (double)取样边距), y1 = (相对区域.Y + 相对区域.Height) / 取样边距;
            if (x0 >= x1 || y0 >= y1)
                throw new ArgumentException("相对区域过小");
            bool 结果 = false;
            List<Task> tasks = new List<Task>();
            // 获取屏幕的设备上下文
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            for (int i = 0; i < x1 - x0; i++) 
                for (int j = 0; j < y1 - y0; j++)
                    tasks.Add(Task.Run(() =>
                    {
                        var (dx, dy, oldColor) = 取点坐标表[x0 + i, y0 + j];
                        uint pixelColor = GetPixel(hdcScreen, 窗口框.X + dx, 窗口框.Y + dy);
                        Color 颜色 = Color.FromArgb((int)pixelColor);
                        取点坐标表[x0 + i, y0 + j] = (dx, dy, 颜色);
                        if (颜色差(颜色, oldColor) > 0)
                            结果 = true;
                    }));
            Task.WaitAll(tasks);
            // 释放屏幕的设备上下文
            ReleaseDC(IntPtr.Zero, hdcScreen);
            return 结果;
        }
    }
}
