using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq.Expressions;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace 淼喵妙神奇工具库.感知库.识别
{
    public class 取像识别器 : 取样感应机
    {
        public 取像识别器(System.Drawing.Size 窗口框尺寸, int 取样边距) : base(窗口框尺寸, 取样边距)
        {
            图像缓存 = new Bitmap(窗口框尺寸.Width, 窗口框尺寸.Height);
        }
        public 取像识别器(IntPtr hWnd, int 取样边距) : this(窗口处理器.获取窗口框(hWnd).Size, 取样边距) { }
        Bitmap 图像缓存;
        public Mat 取像(Rectangle 相对区域, IntPtr hWnd, int 变化阈值 = 10)
        {
            // 获取窗口的物理尺寸（实际像素尺寸）
            Rectangle 窗口框 = 窗口处理器.获取窗口框(hWnd);
            if (相对区域.Left < 0 || 相对区域.Right > 窗口框.Width || 相对区域.Top < 0 || 相对区域.Bottom > 窗口框.Height)
            {
                输出库.通知工具.信息弹窗("相对区域超出窗口范围");
                throw new ArgumentOutOfRangeException("相对区域超出窗口范围");
            }
            if (变化(相对区域, hWnd))
                using (var g = Graphics.FromImage(图像缓存))
                {
                    g.CopyFromScreen(窗口框.Left + 相对区域.X, 窗口框.Top + 相对区域.Y, 相对区域.X, 相对区域.Y, 相对区域.Size);
                }
            Thread.Sleep(100);
            return BitmapConverter.ToMat(图像缓存).SubMat(new Rect(相对区域.Left, 相对区域.Top, 相对区域.Width, 相对区域.Height));
        }
        private static Size 尺寸差(Size 大尺寸, Size 小尺寸) => new Size(大尺寸.Width - 小尺寸.Width + 1, 大尺寸.Height - 小尺寸.Height + 1);
        static Mat 图像统一(Mat 源图像)
        {
            // 图像格式统一
            if (源图像.Type() != MatType.CV_8UC3) // 检查是否为3通道8位图像
            {
                Mat 目标图像 = new Mat();
                if (源图像.Channels() == 1) // 如果是灰度图像
                {
                    Cv2.CvtColor(源图像, 目标图像, ColorConversionCodes.GRAY2BGR); // 转换为3通道图像
                }
                else if (源图像.Channels() == 4) // 如果是带Alpha通道的图像
                {
                    Cv2.CvtColor(源图像, 目标图像, ColorConversionCodes.BGRA2BGR); // 去除Alpha通道
                }
                源图像 = 目标图像; // 将源图像替换为转换后的3通道图像
            }
            return 源图像;
        }
        public static void 模板匹配(Mat 源图像, Mat 模板, Mat 结果)
        {
            源图像 = 图像统一(源图像);
            模板 = 图像统一(模板);

            Mat sigmT = new Mat(), sq = new Mat();
            using Mat fz = new Mat(); 
            Parallel.Invoke(
            () => { Cv2.MatchTemplate(new Mat(模板.Size(), 模板.Type(), 模板.Mean()), 模板, sigmT, TemplateMatchModes.SqDiff); 
                sigmT = new Mat(尺寸差(源图像.Size(), 模板.Size()), sigmT.Type(), sigmT.Mean() ); }, 
            () => Cv2.MatchTemplate(源图像, 模板, fz, TemplateMatchModes.CCoeff),
            () => Cv2.MatchTemplate(源图像, 模板, sq, TemplateMatchModes.SqDiff));
            sq = sq - sigmT + 2 * fz;
            Cv2.Multiply(sq, sigmT, sigmT);
            Cv2.Sqrt(sigmT, sigmT);
            using Mat res = fz / sigmT;
            res.CopyTo(结果);
        }
        //public static int 粗模板匹配(Mat 源图像, Mat 模板, Mat 结果)
        //{
        //    int 格宽 = Math.Min(模板.Width, 模板.Height) / Math.Max(3, 45 / Math.Max(源图像.Width / 模板.Width, 源图像.Height / 模板.Height));
        //    Mat 灰格栅源图像 = new Mat(), 灰格栅模板 = new Mat();
        //    灰格栅化(源图像, 灰格栅源图像, 格宽);
        //    灰格栅化(模板, 灰格栅模板, 格宽);
        //    Mat 粗匹配结果 = new Mat();
        //    模板匹配(灰格栅源图像, 灰格栅模板, 粗匹配结果);
        //    粗匹配结果.CopyTo(结果);
        //    return 格宽;
        //}
        //static void 灰格栅化(Mat 图像, Mat 灰格栅图像, int 格宽)
        //{
        //    Mat 图像灰度 = new Mat();
        //    Cv2.CvtColor(图像, 图像灰度, ColorConversionCodes.BGR2GRAY);
        //    Mat 格栅化图像 = new Mat(图像.Width / 格宽, 图像.Height / 格宽, MatType.CV_32FC1);
        //    Parallel.For(0, 格栅化图像.Width, i =>
        //    {
        //        for (int j = 0; j < 格栅化图像.Height; j++)
        //        {
        //            Mat 格子区域 = 图像.SubMat(new Rect(i * 格宽, j * 格宽, 格宽, 格宽));
        //            Cv2.MinMaxLoc(格子区域, out _, out double 最大灰度值);
        //            格栅化图像.At<float>(i, j) = (float)最大灰度值;
        //        }
        //    });
        //    格栅化图像.CopyTo(灰格栅图像);

        //}
        public List<Rectangle> 搜索图像(Mat 模板, Rectangle 相对区域, IntPtr hWnd, double 识别阈值) 
        {
            Mat 截图 = 取像(相对区域, hWnd);
            // 创建一个Mat来存储结果
            Mat resultMat = new Mat();


            //int 格宽 = 粗模板匹配(截图, 模板, resultMat);

            //var matches = new ConcurrentBag<Rect>();
            //using (Mat thresholded = new Mat())
            //{
            //    Cv2.Threshold(resultMat, thresholded, 识别阈值 * 0.8, 1.0, ThresholdTypes.Binary);
            //    using (Mat nonZeroMat = thresholded.FindNonZero())
            //    {
            //        if (nonZeroMat != null && !nonZeroMat.Empty())
            //            Parallel.For(0, nonZeroMat.Rows, i =>
            //            {
            //                Point p = nonZeroMat.At<Point>(i);
            //                Rect tempRect = new Rect(p.X + 格宽, p.Y + 格宽, 模板.Width + 格宽, 模板.Height + 格宽);
            //                matches.Add(tempRect);
            //            });
            //    }
            //}

            //var res = new ConcurrentBag<Rectangle>();
            //Parallel.ForEach(matches, match =>
            //{
            //    resultMat = new Mat();
            //    模板匹配(截图.SubMat(match), 模板, resultMat);
            //    using (Mat thresholded = new Mat())
            //    {
            //        Cv2.Threshold(resultMat, thresholded, 识别阈值, 1.0, ThresholdTypes.Binary);
            //        using (Mat nonZeroMat = thresholded.FindNonZero())
            //        {
            //            if (nonZeroMat != null && !nonZeroMat.Empty())
            //                for (int i = 0; i < nonZeroMat.Rows; i++)
            //                {
            //                    Point p = nonZeroMat.At<Point>(i);
            //                    Rectangle tempRect = new Rectangle(p.X + match.X, p.Y + match.Y, 模板.Width, 模板.Height);
            //                    res.Add(tempRect);
            //                }
            //        }
            //    }
            //});

            模板匹配(截图, 模板, resultMat);

            var matches = new ConcurrentBag<Rectangle>();
            using (Mat thresholded = new Mat())
            {
                Cv2.Threshold(resultMat, thresholded, 识别阈值, 1.0, ThresholdTypes.Binary);
                using (Mat nonZeroMat = thresholded.FindNonZero())
                {
                    if (nonZeroMat != null && !nonZeroMat.Empty())
                        Parallel.For(0, nonZeroMat.Rows, i =>
                        {
                            Point p = nonZeroMat.At<Point>(i);
                            Rectangle tempRect = new Rectangle(p.X + 相对区域.X, p.Y + 相对区域.Y, 模板.Width, 模板.Height);
                            matches.Add(tempRect);
                        });
                }
            }
            return matches.ToList();
        }
        public bool 识别图像(Mat 模板, Rectangle 相对区域, IntPtr hWnd, double 识别阈值)
        {
            Mat 截图 = 取像(相对区域, hWnd);
            // 创建一个Mat来存储结果
            Mat resultMat = new Mat();

            // 使用模板匹配方法
            模板匹配(截图, 模板, resultMat);
            using (Mat thresholded = new Mat())
            {
                Cv2.Threshold(resultMat, thresholded, 识别阈值, 1.0, ThresholdTypes.Binary);
                using (Mat nonZeroMat = thresholded.FindNonZero())
                {
                    if (nonZeroMat != null && !nonZeroMat.Empty())
                        return true;
                }
            }
            return false;
        }
    }
}
