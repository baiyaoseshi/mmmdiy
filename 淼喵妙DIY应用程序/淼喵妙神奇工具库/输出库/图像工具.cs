using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙神奇工具库.感知库.识别;

namespace 淼喵妙神奇工具库.输出库
{
    public class 图像工具 
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        public static Mat 截图保存(Rectangle 目标框, IntPtr hWnd)
        {
            var 窗口框 = 窗口处理器.获取窗口框(hWnd);
            using (Bitmap bitmap = new Bitmap(目标框.Width, 目标框.Height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(窗口框.X + 目标框.X, 窗口框.Y + 目标框.Y, 0, 0, 目标框.Size);
                return bitmap.ToMat();
            }
        }

        public static void 绘制方框(Rectangle 方框)
        {
            IntPtr hWndDesktop = GetDesktopWindow();

            using (Graphics graphics = Graphics.FromHdc(hWndDesktop))
            using (Pen pen = new Pen(Color.Red, 2))
            {
                graphics.DrawRectangle(pen, 方框);
            }
        }
    }
}
