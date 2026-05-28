using OpenCvSharp;
using OpenCvSharp.Extensions;
using PaddleOCRSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Point = OpenCvSharp.Point;

namespace 淼喵妙神奇工具库.感知库.识别
{
    public class 文字识别器 : 取像识别器
    {
        internal PaddleOCREngine engine;
        public 文字识别器(System.Drawing.Size 窗口框尺寸, int 取样边距) : base(窗口框尺寸, 取样边距)
        {
            OCRParameter parameters = new OCRParameter();
            parameters.rec_img_h = 35;

            // 建议：引擎初始化一次，全局共用，避免频繁创建报错 
            engine = new PaddleOCREngine(null, parameters);
        }
        public OCRResult 文字处理(Rectangle 相对区域, IntPtr hWnd)
        {
            var 截图 = 取像(相对区域, hWnd);
            return engine.DetectText(截图.ToBitmap());
        }
        public List<Rectangle> 搜索文本(string 目标文本, Rectangle 相对区域, IntPtr hWnd)
        {
            var result = 文字处理(相对区域, hWnd);
            var matches = new List<Rectangle>();
            foreach (var block in result.TextBlocks)
            {
                if (block.Text.Contains(目标文本) || block.Text.Replace(" ", "").Contains(目标文本))
                {
                    var pots = block.BoxPoints;
                    matches.Add(new Rectangle(pots[0].X, pots[0].Y, pots[2].X - pots[0].X, pots[2].Y - pots[0].Y));
                }
            }
            return matches;
        }
        public bool 识别文本(string 目标文本, Rectangle 相对区域, IntPtr hWnd)
        {
            var result = 文字处理(相对区域, hWnd);
            var matches = new List<Rectangle>();
            foreach (var block in result.TextBlocks)
            {
                if (block.Text.Contains(目标文本) || block.Text.Replace(" ", "").Contains(目标文本))
                    return true;
            }
            return false;
        }
    }
}
