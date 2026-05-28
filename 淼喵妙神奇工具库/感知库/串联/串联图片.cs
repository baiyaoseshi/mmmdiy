using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using 淼喵妙神奇工具库.感知库.识别;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.动作.流程节点;
using 淼喵妙神奇工具库.键鼠库.监听.获取;
using Size = System.Drawing.Size;

namespace 淼喵妙神奇工具库.感知库.串联
{
    public class 串联图片 : 串联抽象类
    {
        Func<Rectangle, IntPtr, 取像识别器, List<Rectangle>> 目标列表;
        public Rectangle 校准参数;
        public 串联图片 附加;
        public 节点参数<Rectangle> 初始校准参数 = null;

        public Mat 模板;
        public double 识别阈值 { get; private set; } = 0.8;
        public string 目标文本 { get; private set; } = "";
        public bool 是文字识别 { get; private set; } = false;

        public 串联图片(Mat 模板, double 识别阈值)
        {
            this.模板 = 模板;
            this.识别阈值 = 识别阈值;
            目标列表 += (r, h, 识别器) => 识别器.搜索图像(模板, r, h, 识别阈值);
        }
        public static 串联图片 创建(IntPtr hWnd, out Rectangle 目标框, (Rectangle, 串联图片)? 锚 = null)
        {
            目标框 = Rectangle.Empty;
            if (通知工具.确认弹窗("添加识图条件?"))
            {
                var 窗口框 = 窗口处理器.获取窗口框(hWnd);
                通知工具.信息弹窗("选择识别范围:"); 
                var 识别范围 = new 获取框(hWnd).获取框信息();
                通知工具.信息弹窗("框选目标:");
                目标框 = new 获取框(hWnd).获取框信息();
                var 条件 = new 串联图片(图像工具.截图保存(目标框, hWnd),
                    double.Parse(通知工具.输入弹窗("请输入相似度阈值:", "", "")));
                创建(hWnd, out _, (目标框, 条件));
                if (锚 != null)
                {
                    锚.Value.Item2.附加 = 条件;
                    锚.Value.Item2.校准参数 = new Rectangle(识别范围.X - (锚.Value.Item1.X + 锚.Value.Item1.Width / 2),
                        识别范围.Y - (锚.Value.Item1.Y + 锚.Value.Item1.Height / 2), 识别范围.Width, 识别范围.Height);
                }
                else
                {
                    if (通知工具.确认弹窗("是否使用识别范围作为固定搜索区域？"))
                    {
                        条件.初始校准参数 = new 节点参数<Rectangle>(识别范围);
                    }
                    else
                    {
                        string 变量名 = 通知工具.输入弹窗("请输入搜索区域变量名:", "", "");
                        条件.初始校准参数 = new 节点参数<Rectangle>(default, 变量名);
                    }
                    return 条件;
                }
            }
            else if (通知工具.确认弹窗("添加文字条件?"))
            {
                var 窗口框 = 窗口处理器.获取窗口框(hWnd);
                通知工具.信息弹窗("选择识别范围:");
                var 识别范围 = new 获取框(hWnd).获取框信息();
                var 条件 = new 串联图片(通知工具.输入弹窗("输入目标文本:", "", ""));
                创建(hWnd, out _, (目标框, 条件));
                if (锚 != null)
                {
                    锚.Value.Item2.附加 = 条件;
                    锚.Value.Item2.校准参数 = new Rectangle(识别范围.X - (锚.Value.Item1.X + 锚.Value.Item1.Width / 2),
                        识别范围.Y - (锚.Value.Item1.Y + 锚.Value.Item1.Height / 2), 识别范围.Width, 识别范围.Height);
                }
                else
                {
                    if (通知工具.确认弹窗("是否使用识别范围作为固定搜索区域？"))
                    {
                        条件.初始校准参数 = new 节点参数<Rectangle>(识别范围);
                    }
                    else
                    {
                        string 变量名 = 通知工具.输入弹窗("请输入搜索区域变量名:", "", "");
                        条件.初始校准参数 = new 节点参数<Rectangle>(default, 变量名);
                    }
                    return 条件;
                }
            }
            return null;
        }
        public 串联图片(string 目标文本)
        {
            this.目标文本 = 目标文本;
            this.是文字识别 = true;
            目标列表 += (r, h, 识别器) => (识别器 as 文字识别器).搜索文本(目标文本, r, h);
        }
        public Rectangle 校准(Rectangle 原始结果, IntPtr hWnd, Rectangle 校准)
        {
            return new Rectangle(原始结果.Left + 原始结果.Width / 2 + 校准.Left, 原始结果.Top + 原始结果.Height / 2 + 校准.Top,
                    校准.Width, 校准.Height);
        }

        public List<Rectangle> 搜索(取像识别器 识别器, Rectangle 限制区域, IntPtr hWnd)
        {
            List<Rectangle> search = 目标列表(限制区域, hWnd, 识别器), res = new List<Rectangle>();

            foreach (var rect in search)
            {
                if (附加 == null)
                {
                    res.Add(rect);
                }
                else
                {
                    var 校准结果 = 校准(rect, hWnd, 校准参数);
                    List<Rectangle> 附加搜索 = 附加.搜索(识别器, 校准结果, hWnd);
                    if (附加搜索.Count > 0)
                        res.Add(rect);
                }
            }
            return res;
        }
        public List<Rectangle> 搜索(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (!全局.ContainsKey("文字识别器") || 全局["文字识别器"] == null)
                全局["文字识别器"] = new 文字识别器(窗口处理器.获取窗口框(hWnd).Size, 5);
            取像识别器 识别器 = 全局["文字识别器"] as 取像识别器;
            Rectangle 限制区域 = 初始校准参数?.解析值(全局) ?? 窗口处理器.获取窗口框(hWnd);
            return 搜索(识别器, 限制区域, hWnd);
        }
        public override bool 判断(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            return 搜索(hWnd, 全局).Count > 0;
        }
    }
}
