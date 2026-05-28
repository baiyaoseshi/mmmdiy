using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙神奇工具库.感知库.识别;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听.获取;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 识别进度条 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public string 搜索区域变量名 = "搜索区域";
        public int 进度条宽度 = 100;
        public int 进度条高度 = 20;
        public Color 进度颜色 = Color.Green;
        public Color 背景颜色 = Color.Gray;
        public int 颜色阈值 = 20;
        public string 输出变量名 = "进度百分比";

        识别进度条() : base() { }

        public 识别进度条(string 搜索区域变量名, int 进度条宽度, int 进度条高度, string 输出变量名,
            Color 进度颜色 = default, Color 背景颜色 = default, int 颜色阈值 = 20) : base()
        {
            this.搜索区域变量名 = 搜索区域变量名;
            this.进度条宽度 = 进度条宽度;
            this.进度条高度 = 进度条高度;
            this.输出变量名 = 输出变量名;
            this.进度颜色 = 进度颜色 == default ? Color.Green : 进度颜色;
            this.背景颜色 = 背景颜色 == default ? Color.Gray : 背景颜色;
            this.颜色阈值 = 颜色阈值;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (!全局.ContainsKey(搜索区域变量名))
            {
                通知工具.吐司通知($"未找到搜索区域变量: {搜索区域变量名}");
                return false;
            }

            Rectangle 搜索区域 = (Rectangle)全局[搜索区域变量名];
            
            if (!全局.ContainsKey("取像识别器") || 全局["取像识别器"] == null)
                全局["取像识别器"] = new 取像识别器(hWnd, 5);
            取像识别器 取像器 = 全局["取像识别器"] as 取像识别器;
            Mat 截图 = 取像器.取像(搜索区域, hWnd);
            Bitmap bmp = 截图.ToBitmap();

            Rectangle 进度条位置 = FindProgressBarPosition(bmp);
            
            if (进度条位置.IsEmpty)
            {
                通知工具.吐司通知("未在搜索区域内找到进度条");
                全局[输出变量名] = 0.0;
                return true;
            }

            double 百分比 = CalculateProgressPercentage(bmp, 进度条位置);
            
            if (!string.IsNullOrEmpty(输出变量名))
            {
                全局[输出变量名] = 百分比;
            }
            
            return true;
        }

        private Rectangle FindProgressBarPosition(Bitmap bmp)
        {
            for (int y = 0; y <= bmp.Height - 进度条高度; y++)
            {
                for (int x = 0; x <= bmp.Width - 进度条宽度; x++)
                {
                    if (IsProgressBarMatch(bmp, x, y))
                    {
                        return new Rectangle(x, y, 进度条宽度, 进度条高度);
                    }
                }
            }
            return Rectangle.Empty;
        }

        private bool IsProgressBarMatch(Bitmap bmp, int startX, int startY)
        {
            int 匹配进度颜色像素 = 0;
            int 匹配背景颜色像素 = 0;
            int 总像素 = 进度条宽度 * 进度条高度;

            for (int y = startY; y < startY + 进度条高度; y++)
            {
                for (int x = startX; x < startX + 进度条宽度; x++)
                {
                    Color pixelColor = bmp.GetPixel(x, y);
                    
                    if (ColorMatch(pixelColor, 进度颜色, 颜色阈值))
                    {
                        匹配进度颜色像素++;
                    }
                    else if (ColorMatch(pixelColor, 背景颜色, 颜色阈值))
                    {
                        匹配背景颜色像素++;
                    }
                }
            }

            double 有效像素比例 = (double)(匹配进度颜色像素 + 匹配背景颜色像素) / 总像素;
            return 有效像素比例 > 0.8;
        }

        private double CalculateProgressPercentage(Bitmap bmp, Rectangle 进度条区域)
        {
            int 进度像素数 = 0;
            
            for (int y = 进度条区域.Y; y < 进度条区域.Y + 进度条区域.Height; y++)
            {
                for (int x = 进度条区域.X; x < 进度条区域.X + 进度条区域.Width; x++)
                {
                    Color pixelColor = bmp.GetPixel(x, y);
                    
                    if (ColorMatch(pixelColor, 进度颜色, 颜色阈值))
                    {
                        进度像素数++;
                    }
                }
            }
            
            return (double)进度像素数 / (进度条区域.Width * 进度条区域.Height) * 100;
        }

        private bool ColorMatch(Color c1, Color c2, int threshold)
        {
            return System.Math.Abs(c1.R - c2.R) < threshold &&
                   System.Math.Abs(c1.G - c2.G) < threshold &&
                   System.Math.Abs(c1.B - c2.B) < threshold;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@识别进度条:\n" +
                   $"搜索区域变量名[{搜索区域变量名}],\n" +
                   $"进度条宽度[{进度条宽度}],\n" +
                   $"进度条高度[{进度条高度}],\n" +
                   $"进度颜色[{进度颜色.ToArgb()}],\n" +
                   $"背景颜色[{背景颜色.ToArgb()}],\n" +
                   $"颜色阈值[{颜色阈值}],\n" +
                   $"输出变量名[{输出变量名}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 识别进度条 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@识别进度条:"))
            {
                var 节点 = new 识别进度条();
                节点.搜索区域变量名 = ParseString(字符串, @"搜索区域变量名\[([^\]]+)\]");
                节点.进度条宽度 = ParseInt(字符串, @"进度条宽度\[([^\]]+)\]");
                节点.进度条高度 = ParseInt(字符串, @"进度条高度\[([^\]]+)\]");
                节点.进度颜色 = Color.FromArgb(ParseInt(字符串, @"进度颜色\[([^\]]+)\]"));
                节点.背景颜色 = Color.FromArgb(ParseInt(字符串, @"背景颜色\[([^\]]+)\]"));
                节点.颜色阈值 = ParseInt(字符串, @"颜色阈值\[([^\]]+)\]");
                节点.输出变量名 = ParseString(字符串, @"输出变量名\[([^\]]+)\]");
                return 节点;
            }
            return null;
        }

        public static 识别进度条 创建节点(IntPtr hWnd)
        {
            string 搜索区域变量名 = 通知工具.输入弹窗("请输入搜索区域变量名:", "", "");
            
            if (string.IsNullOrEmpty(搜索区域变量名))
            {
                通知工具.吐司通知("搜索区域变量名不能为空");
                return null;
            }

            通知工具.吐司通知("请框选进度条模板区域:");
            var 区域框 = new 获取框(hWnd).获取框信息();

            string 变量名 = 通知工具.输入弹窗("请输入输出变量名:", "", "");

            return new 识别进度条(搜索区域变量名, 区域框.Width, 区域框.Height, 变量名);
        }

        private static int ParseInt(string 字符串, string pattern)
        {
            Match match = Regex.Match(字符串, pattern);
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private static string ParseString(string 字符串, string pattern)
        {
            Match match = Regex.Match(字符串, pattern);
            return match.Success ? match.Groups[1].Value : "";
        }
    }
}
