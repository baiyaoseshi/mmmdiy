using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙神奇工具库.感知库.识别;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听.获取;
using Mat = OpenCvSharp.Mat;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 点击图片 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public 节点参数<鼠标按键类型> 鼠标键 = new 节点参数<鼠标按键类型>(鼠标按键类型.左键);
        public 节点参数<Rectangle> 识别范围 = new 节点参数<Rectangle>();
        public double 识别阈值 = 0.85;
        int 取样边距 = 8;
        Mat 模板图像;

        点击图片() : base() { }

        public 点击图片(Mat 模板, 节点参数<鼠标按键类型> 鼠标键 = null, 节点参数<Rectangle> 识别范围 = null, double 识别阈值 = 0.85, int 取样边距 = 8) : base()
        {
            this.模板图像 = 模板;
            this.鼠标键 = 鼠标键 ?? new 节点参数<鼠标按键类型>(鼠标按键类型.左键);
            this.识别范围 = 识别范围 ?? new 节点参数<Rectangle>();
            this.识别阈值 = 识别阈值;
            this.取样边距 = 取样边距;
        }


        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (全局.ContainsKey("鼠标锁定") && (bool)全局["鼠标锁定"])
            {
                return false;
            }
            if (!全局.ContainsKey("取像识别器") || 全局["取像识别器"] == null)
                全局["取像识别器"] = new 取像识别器(窗口处理器.获取窗口框(hWnd).Size, 取样边距);
            取像识别器 识别器 = 全局["取像识别器"] as 取像识别器;
            Rectangle 区域 = 从全局解析(识别范围, 全局);

            Mat 模板 = 模板图像;
            var matches = 识别器.搜索图像(模板, 区域, hWnd, 识别阈值);

            Point 找到位置 = Point.Empty;
            if (matches.Count > 0)
            {
                var first = matches[0];
                找到位置 = new Point(first.X + first.Width / 2, first.Y + first.Height / 2);
            }
            if (找到位置 != Point.Empty)
            {
                var 控制器 = new 鼠标控制器(hWnd);
                switch (从全局解析(鼠标键, 全局))
                {
                    case 鼠标按键类型.左键:
                        控制器.左键单击(找到位置);
                        break;
                    case 鼠标按键类型.右键:
                        控制器.右键单击(找到位置);
                        break;
                    case 鼠标按键类型.中键:
                        控制器.中键单击(找到位置);
                        break;
                }
                return true;
            }

            return false;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@点击图片:\n" +
                   $"模板数据[{Convert.ToBase64String(模板图像.ImEncode(".png"))}],\n" +
                   $"鼠标键[{节点参数.序列化(鼠标键)}],\n" +
                   $"识别范围[{节点参数.序列化(识别范围)}],\n" +
                   $"识别阈值[{识别阈值}],\n" +
                   $"取样边距[{取样边距}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 点击图片 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@点击图片:"))
            {
                var 节点 = new 点击图片();
                Regex regex = new Regex(@"模板数据\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    var 字节 = Convert.FromBase64String(match.Groups[1].Value);
                    节点.模板图像 = OpenCvSharp.Cv2.ImDecode(字节, OpenCvSharp.ImreadModes.Color);
                }
                节点.鼠标键 = 解析参数<鼠标按键类型>(字符串, @"鼠标键\[([^\]]+)\]");
                节点.识别范围 = 解析参数<Rectangle>(字符串, @"识别范围\[([^\]]+)\]");
                
                regex = new Regex(@"识别阈值\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.识别阈值 = double.Parse(match.Groups[1].Value);
                }
                regex = new Regex(@"取样边距\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.取样边距 = int.Parse(match.Groups[1].Value);
                }
                return 节点;
            }
            return null;
        }

        public static 点击图片 创建节点(IntPtr hWnd)
        {
            通知工具.吐司通知("请框选要点击的目标图片:");
            Mat 模板 = new 获取图像(hWnd).获取图像信息();
            if (模板 == null)
                return null;

            节点参数<Rectangle> 识别范围参数;
            if (通知工具.确认弹窗("是否使用固定搜索区域？"))
            {
                通知工具.吐司通知("请框选图片搜索区域(可选):");
                var 区域框 = new 获取框(hWnd).获取框信息();
                识别范围参数 = new 节点参数<Rectangle>(new Rectangle(区域框.X, 区域框.Y, 区域框.Width, 区域框.Height));
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入搜索区域变量名:", "", "");
                识别范围参数 = new 节点参数<Rectangle>(default, 变量名);
            }

            节点参数<鼠标按键类型> 按键参数;
            if (通知工具.确认弹窗("是否使用固定按键类型？"))
            {
                var 按键选项 = new List<string> { "左键", "右键", "中键" };
                string 选择 = 通知工具.选项弹窗("请选择鼠标按键类型:", 按键选项);
                鼠标按键类型 鼠标键值 = (鼠标按键类型)Enum.Parse(typeof(鼠标按键类型), 选择);
                按键参数 = new 节点参数<鼠标按键类型>(鼠标键值);
            }
            else
            {
                string 变量名 = 通知工具.输入弹窗("请输入按键类型变量名:", "", "");
                按键参数 = new 节点参数<鼠标按键类型>(default, 变量名);
            }
            double 阈值 = double.Parse(通知工具.输入弹窗("请输入识别阈值(0~1):", "", ""));

            return new 点击图片(模板, 按键参数, 识别范围参数, 阈值);
        }

        private static 节点参数<T> 解析参数<T>(string 字符串, string pattern)
        {
            Match match = Regex.Match(字符串, pattern);
            if (match.Success)
                return 节点参数.反序列化<T>(match.Groups[1].Value);
            return new 节点参数<T>();
        }
    }
}
