using OpenCvSharp;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙神奇工具库.感知库.识别;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听.获取;
using Rectangle = System.Drawing.Rectangle;
using Size = OpenCvSharp.Size;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 识别方框 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 节点参数<Rectangle> 搜索区域 = new 节点参数<Rectangle>();
        public 节点参数<Size> 方框尺寸 = new 节点参数<Size>();
        public string 输出变量名 = "方框列表";
        public int 尺寸误差 = 15;

        识别方框() : base() { }

        public 识别方框(节点参数<Rectangle> 搜索区域, string 输出变量名, 节点参数<Size> 方框尺寸, int 尺寸误差) : base()
        {
            this.搜索区域 = 搜索区域;
            this.输出变量名 = 输出变量名;
            this.方框尺寸 = 方框尺寸;
            this.尺寸误差 = 尺寸误差;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (!全局.ContainsKey("取像识别器") || 全局["取像识别器"] == null)
                全局["取像识别器"] = new 取像识别器(hWnd, 5);
            取像识别器 取像器 = 全局["取像识别器"] as 取像识别器;
            Rectangle 搜索区域值 = 从全局解析(搜索区域, 全局);
            Size 方框尺寸值 = 从全局解析(方框尺寸, 全局);
            int 目标宽 = 方框尺寸值.Width;
            int 目标高 = 方框尺寸值.Height;
            int 方框短边 = Math.Min(目标宽, 目标高);

            Mat 截图 = 取像器.取像(搜索区域值, hWnd);

            Mat blurred = new Mat();
            Cv2.GaussianBlur(截图, blurred, new Size(3, 3), 0);

            Mat[] grays = 自适应分割(blurred);

            Mat sum = new Mat();
            Mat edged = new Mat();
            Cv2.Canny(grays[0], sum, 10, 80);
            Cv2.Canny(grays[1], edged, 10, 80);
            Cv2.Add(sum, edged, sum);
            Cv2.Canny(grays[2], edged, 10, 80);
            Cv2.Add(sum, edged, sum);

            Cv2.FindContours(sum, out OpenCvSharp.Point[][] contours, out _, RetrievalModes.List, ContourApproximationModes.ApproxSimple);
            var 候选列表 = new List<Rectangle>();
            foreach (OpenCvSharp.Point[] contour in contours)
            {
                Rect rect = Cv2.BoundingRect(contour);
                if (Math.Abs(rect.Width - 目标宽) + Math.Abs(rect.Height - 目标高) <= 尺寸误差)
                {
                    var 新矩形 = new Rectangle(搜索区域值.X + rect.X, 搜索区域值.Y + rect.Y, rect.Width, rect.Height);
                    if (!候选列表.Exists(r => r.IntersectsWith(新矩形)))
                        候选列表.Add(新矩形);
                }
            }

            if (!string.IsNullOrEmpty(输出变量名))
            {
                全局[输出变量名] = 候选列表;
            }

            return true;
        }

        private static Mat[] 自适应分割(Mat bgr截图)
        {
            var mean = Cv2.Mean(bgr截图);
            double a = mean.Val0 + mean.Val1 + mean.Val2;
            double aB = mean.Val0 / a;
            double aG = mean.Val1 / a;
            double aR = mean.Val2 / a;

            Mat[] mv;
            Cv2.Split(bgr截图, out mv);
            using Mat b = mv[0];
            using Mat g = mv[1];
            using Mat r = mv[2];

            Mat bW = new Mat();
            Mat gW = new Mat();
            Mat rW = new Mat();
            Cv2.Multiply(b, aB, bW);
            Cv2.Multiply(g, aG, gW);
            Cv2.Multiply(r, aR, rW);
            return [bW, gW, rW];
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@识别方框:\n" +
                   $"搜索区域[{节点参数.序列化(搜索区域)}],\n" +
                   $"方框尺寸[{节点参数.序列化(方框尺寸)}],\n" +
                   $"尺寸误差[{尺寸误差}],\n" +
                   $"输出变量名[{输出变量名}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 识别方框 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@识别方框:"))
            {
                var 节点 = new 识别方框();
                节点.搜索区域 = 解析参数<Rectangle>(字符串, @"搜索区域\[([^\]]+)\]");
                节点.方框尺寸 = 解析参数<Size>(字符串, @"方框尺寸\[([^\]]+)\]");
                节点.尺寸误差 = ParseInt(字符串, @"尺寸误差\[([^\]]+)\]");
                节点.输出变量名 = ParseString(字符串, @"输出变量名\[([^\]]+)\]");
                return 节点;
            }
            return null;
        }

        public static 识别方框 创建节点(IntPtr hWnd)
        {
            节点参数<Rectangle> 搜索区域参数;
            if (通知工具.确认弹窗("是否使用固定搜索区域？"))
            {
                通知工具.吐司通知("请框选搜索区域:");
                var 区域框 = new 获取框(hWnd).获取框信息();
                搜索区域参数 = new 节点参数<Rectangle>(new Rectangle(区域框.X, 区域框.Y, 区域框.Width, 区域框.Height));
            }
            else
            {
                string 区域名 = 通知工具.输入弹窗("请输入搜索区域变量名:", "", "");
                搜索区域参数 = new 节点参数<Rectangle>(default, 区域名);
            }

            节点参数<Size> 方框尺寸参数;
            if (通知工具.确认弹窗("是否使用固定方框尺寸？"))
            {
                通知工具.吐司通知("请框选方框模板区域:");
                var 尺寸框 = new 获取框(hWnd).获取框信息();
                方框尺寸参数 = new 节点参数<Size>(new Size(尺寸框.Width, 尺寸框.Height));
            }
            else
            {
                string 尺寸名 = 通知工具.输入弹窗("请输入方框尺寸变量名:", "", "");
                方框尺寸参数 = new 节点参数<Size>(default, 尺寸名);
            }

            string 变量名 = 通知工具.输入弹窗("请输入输出变量名:", "", "");
            int 尺寸误差值 = 15;
            if (!通知工具.确认弹窗("使用默认尺寸误差15？"))
            {
                int.TryParse(通知工具.输入弹窗("请输入尺寸误差:", "", ""), out 尺寸误差值);
            }

            return new 识别方框(搜索区域参数, 变量名, 方框尺寸参数, 尺寸误差值);
        }

        private static 节点参数<T> 解析参数<T>(string 字符串, string pattern)
        {
            Match match = Regex.Match(字符串, pattern);
            if (match.Success)
                return 节点参数.反序列化<T>(match.Groups[1].Value);
            return new 节点参数<T>();
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