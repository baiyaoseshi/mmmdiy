using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace 淼喵妙神奇工具库.感知库.识别
{
    public class 通用图像处理
    {
        // 定义 GetPixel 函数
        [DllImport("gdi32.dll")]
        protected static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        // 定义 GetDC 函数
        [DllImport("user32.dll")]
        protected static extern IntPtr GetDC(IntPtr hWnd);

        // 定义 ReleaseDC 函数
        [DllImport("user32.dll")]
        protected static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        protected static int 颜色差(Color? color1, Color? color2)
        {
            if (color1 == null || color2 == null)
                return int.MaxValue; // 如果任一颜色为null，返回最大差异
            int rDiff = Math.Abs(color1.Value.R - color2.Value.R);
            int gDiff = Math.Abs(color1.Value.G - color2.Value.G);
            int bDiff = Math.Abs(color1.Value.B - color2.Value.B);
            int aDiff = Math.Abs(color1.Value.A - color2.Value.A);
            int 差异 = rDiff + gDiff + bDiff + aDiff;
            return 差异;
        }
    }
}
