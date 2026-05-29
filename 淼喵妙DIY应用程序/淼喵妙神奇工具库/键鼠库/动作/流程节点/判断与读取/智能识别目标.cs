using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Linq;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙神奇工具库.感知库.识别;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.监听.获取;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 智能识别目标 : 控制节点
    {
        public override bool 需要窗口句柄 => true;

        public List<string> 训练图片路径列表 = new List<string>();
        public string 预训练模型URL = "https://example.com/models/yolov8n.onnx";
        public string 模型保存路径 = "models/yolov8n.onnx";
        public string 搜索区域变量名 = "搜索区域";
        public float 置信度阈值 = 0.5f;
        public float NMS阈值 = 0.45f;
        public string 输出变量名 = "目标列表";

        private InferenceSession 推理会话;
        private List<Bitmap> 训练样本 = new List<Bitmap>();
        private bool 模型已加载 = false;

        智能识别目标() : base() { }

        public 智能识别目标(List<string> 训练图片路径列表, string 预训练模型URL, string 搜索区域变量名, 
            string 输出变量名, float 置信度阈值 = 0.5f, float NMS阈值 = 0.45f) : base()
        {
            this.训练图片路径列表 = 训练图片路径列表 ?? new List<string>();
            this.预训练模型URL = 预训练模型URL;
            this.搜索区域变量名 = 搜索区域变量名;
            this.输出变量名 = 输出变量名;
            this.置信度阈值 = 置信度阈值;
            this.NMS阈值 = NMS阈值;
            this.模型保存路径 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "models", Path.GetFileName(预训练模型URL));
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (!全局.ContainsKey(搜索区域变量名))
            {
                通知工具.吐司通知($"未找到搜索区域变量: {搜索区域变量名}");
                return false;
            }

            Rectangle 搜索区域 = (Rectangle)全局[搜索区域变量名];

            if (!模型已加载)
            {
                下载并加载模型();
                加载训练样本();
            }

            if (!全局.ContainsKey("取像识别器") || 全局["取像识别器"] == null)
                全局["取像识别器"] = new 取像识别器(hWnd, 5);
            取像识别器 取像器 = 全局["取像识别器"] as 取像识别器;
            Mat 截图 = 取像器.取像(搜索区域, hWnd);
            Bitmap sourceBmp = 截图.ToBitmap();

            List<Rectangle> 目标列表 = 推理识别(sourceBmp, 搜索区域);

            if (!string.IsNullOrEmpty(输出变量名))
            {
                全局[输出变量名] = 目标列表;
            }

            return true;
        }

        private void 下载并加载模型()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(模型保存路径));

                if (!File.Exists(模型保存路径))
                {
                    通知工具.吐司通知("正在下载预训练模型...");
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(预训练模型URL, 模型保存路径);
                    }
                    通知工具.吐司通知("模型下载完成");
                }

                推理会话 = new InferenceSession(模型保存路径);
                模型已加载 = true;
                通知工具.吐司通知("模型加载成功");
            }
            catch (Exception ex)
            {
                通知工具.吐司通知($"模型加载失败: {ex.Message}");
                throw;
            }
        }

        private void 加载训练样本()
        {
            训练样本.Clear();
            foreach (string 路径 in 训练图片路径列表)
            {
                if (File.Exists(路径))
                {
                    try
                    {
                        using (Bitmap bmp = new Bitmap(路径))
                        {
                            训练样本.Add(new Bitmap(bmp));
                        }
                    }
                    catch { }
                }
            }
        }

        private List<Rectangle> 推理识别(Bitmap 输入图片, Rectangle 搜索区域)
        {
            List<Rectangle> 结果 = new List<Rectangle>();

            if (推理会话 == null || 训练样本.Count == 0)
                return 结果;

            try
            {
                int 输入宽度 = 640;
                int 输入高度 = 640;

                Bitmap 缩放图片 = ResizeAndPad(输入图片, 输入宽度, 输入高度);
                Tensor<float> 输入张量 = BitmapToTensor(缩放图片);

                var 输入名称 = 推理会话.InputMetadata.Keys.First();
                var 输出名称 = 推理会话.OutputMetadata.Keys.First();

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(输入名称, 输入张量)
                };

                using (var outputs = 推理会话.Run(inputs))
                {
                    var 输出张量 = outputs.First().AsTensor<float>();
                    结果 = 解析输出(输出张量, 输入图片.Width, 输入图片.Height, 搜索区域);
                }
            }
            catch (Exception ex)
            {
                通知工具.吐司通知($"识别失败: {ex.Message}");
            }

            return 结果;
        }

        private Bitmap ResizeAndPad(Bitmap bmp, int targetWidth, int targetHeight)
        {
            float scale = Math.Min((float)targetWidth / bmp.Width, (float)targetHeight / bmp.Height);
            int newWidth = (int)(bmp.Width * scale);
            int newHeight = (int)(bmp.Height * scale);

            Bitmap resized = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmp, 0, 0, newWidth, newHeight);
            }

            Bitmap padded = new Bitmap(targetWidth, targetHeight);
            using (Graphics g = Graphics.FromImage(padded))
            {
                g.Clear(Color.Black);
                int x = (targetWidth - newWidth) / 2;
                int y = (targetHeight - newHeight) / 2;
                g.DrawImage(resized, x, y);
            }

            return padded;
        }

        private Tensor<float> BitmapToTensor(Bitmap bmp)
        {
            Tensor<float> tensor = new DenseTensor<float>(new[] { 1, 3, bmp.Height, bmp.Width });

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    tensor[0, 0, y, x] = pixel.R / 255f;
                    tensor[0, 1, y, x] = pixel.G / 255f;
                    tensor[0, 2, y, x] = pixel.B / 255f;
                }
            }

            return tensor;
        }

        private List<Rectangle> 解析输出(Tensor<float> output, int 原图宽度, int 原图高度, Rectangle 搜索区域)
        {
            List<Rectangle> 结果 = new List<Rectangle>();

            try
            {
                int 步长 = output.Dimensions[1];
                int 数量 = output.Dimensions[2];

                List<float[]> 检测结果 = new List<float[]>();

                for (int i = 0; i < 数量; i++)
                {
                    float[] 检测 = new float[步长];
                    for (int j = 0; j < 步长; j++)
                    {
                        检测[j] = output[0, j, i];
                    }

                    float 置信度 = 检测[4];
                    if (置信度 >= 置信度阈值)
                    {
                        检测结果.Add(检测);
                    }
                }

                List<int> 保留索引 = NMS(检测结果, NMS阈值);

                foreach (int idx in 保留索引)
                {
                    float[] 检测 = 检测结果[idx];
                    float x = 检测[0];
                    float y = 检测[1];
                    float w = 检测[2];
                    float h = 检测[3];

                    float 比例 = Math.Min((float)640 / 原图宽度, (float)640 / 原图高度);
                    float padX = (640 - 原图宽度 * 比例) / 2;
                    float padY = (640 - 原图高度 * 比例) / 2;

                    int 实际X = (int)((x - padX) / 比例);
                    int 实际Y = (int)((y - padY) / 比例);
                    int 实际宽度 = (int)(w / 比例);
                    int 实际高度 = (int)(h / 比例);

                    结果.Add(new Rectangle(搜索区域.X + 实际X, 搜索区域.Y + 实际Y, 实际宽度, 实际高度));
                }
            }
            catch { }

            return 结果;
        }

        private List<int> NMS(List<float[]> 检测结果, float 阈值)
        {
            List<int> 保留索引 = new List<int>();

            if (检测结果.Count == 0)
                return 保留索引;

            float[] 置信度 = 检测结果.Select(d => d[4]).ToArray();
            int[] 索引 = Enumerable.Range(0, 检测结果.Count).ToArray();

            Array.Sort(置信度, 索引);
            Array.Reverse(索引);

            while (索引.Length > 0)
            {
                int 最佳索引 = 索引[0];
                保留索引.Add(最佳索引);

                List<int> 剩余索引 = new List<int>();
                float[] 最佳检测 = 检测结果[最佳索引];

                for (int i = 1; i < 索引.Length; i++)
                {
                    float[] 当前检测 = 检测结果[索引[i]];
                    float iou = CalculateIOU(最佳检测, 当前检测);
                    if (iou < 阈值)
                    {
                        剩余索引.Add(索引[i]);
                    }
                }

                索引 = 剩余索引.ToArray();
            }

            return 保留索引;
        }

        private float CalculateIOU(float[] box1, float[] box2)
        {
            float x1 = Math.Max(box1[0] - box1[2] / 2, box2[0] - box2[2] / 2);
            float y1 = Math.Max(box1[1] - box1[3] / 2, box2[1] - box2[3] / 2);
            float x2 = Math.Min(box1[0] + box1[2] / 2, box2[0] + box2[2] / 2);
            float y2 = Math.Min(box1[1] + box1[3] / 2, box2[1] + box2[3] / 2);

            float 交集 = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
            float 并集 = box1[2] * box1[3] + box2[2] * box2[3] - 交集;

            return 交集 / 并集;
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            string 图片路径 = string.Join("|", 训练图片路径列表);
            return "@智能识别目标:\n" +
                   $"训练图片路径列表[{图片路径}],\n" +
                   $"预训练模型URL[{预训练模型URL}],\n" +
                   $"搜索区域变量名[{搜索区域变量名}],\n" +
                   $"置信度阈值[{置信度阈值}],\n" +
                   $"NMS阈值[{NMS阈值}],\n" +
                   $"输出变量名[{输出变量名}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 智能识别目标 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@智能识别目标:"))
            {
                var 节点 = new 智能识别目标();
                string 图片路径 = ParseString(字符串, @"训练图片路径列表\[([^\]]+)\]");
                节点.训练图片路径列表 = string.IsNullOrEmpty(图片路径) ? new List<string>() :
                    new List<string>(图片路径.Split('|'));
                节点.预训练模型URL = ParseString(字符串, @"预训练模型URL\[([^\]]+)\]");
                节点.搜索区域变量名 = ParseString(字符串, @"搜索区域变量名\[([^\]]+)\]");
                节点.置信度阈值 = ParseFloat(字符串, @"置信度阈值\[([^\]]+)\]");
                节点.NMS阈值 = ParseFloat(字符串, @"NMS阈值\[([^\]]+)\]");
                节点.输出变量名 = ParseString(字符串, @"输出变量名\[([^\]]+)\]");
                return 节点;
            }
            return null;
        }

        public static 智能识别目标 创建节点(IntPtr hWnd)
        {
            string 模型URL = 通知工具.输入弹窗("请输入预训练模型URL:", "", "");
            if (string.IsNullOrEmpty(模型URL))
            {
                通知工具.吐司通知("模型URL不能为空");
                return null;
            }

            通知工具.吐司通知("请输入训练图片路径(多个用|分隔):");
            string 图片路径输入 = 通知工具.输入弹窗("请输入训练图片路径(多个用|分隔):", "", "");

            List<string> 图片路径列表 = new List<string>();
            if (!string.IsNullOrEmpty(图片路径输入))
            {
                foreach (string 路径 in 图片路径输入.Split('|'))
                {
                    string 清理路径 = 路径.Trim();
                    if (File.Exists(清理路径))
                    {
                        图片路径列表.Add(清理路径);
                    }
                }
            }

            if (图片路径列表.Count == 0)
            {
                通知工具.吐司通知("未找到有效图片文件");
                return null;
            }

            string 搜索区域变量名 = 通知工具.输入弹窗("请输入搜索区域变量名:", "", "");
            if (string.IsNullOrEmpty(搜索区域变量名))
            {
                通知工具.吐司通知("搜索区域变量名不能为空");
                return null;
            }

            string 变量名 = 通知工具.输入弹窗("请输入输出变量名:", "", "");

            string 置信度输入 = 通知工具.输入弹窗("请输入置信度阈值(0-1):", "", "");
            float 置信度 = float.TryParse(置信度输入, out float result) ? result : 0.5f;

            return new 智能识别目标(图片路径列表, 模型URL, 搜索区域变量名, 变量名, 置信度);
        }

        private static int ParseInt(string 字符串, string pattern)
        {
            Match match = Regex.Match(字符串, pattern);
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        private static float ParseFloat(string 字符串, string pattern)
        {
            Match match = Regex.Match(字符串, pattern);
            return match.Success ? float.Parse(match.Groups[1].Value) : 0.5f;
        }

        private static string ParseString(string 字符串, string pattern)
        {
            Match match = Regex.Match(字符串, pattern);
            return match.Success ? match.Groups[1].Value : "";
        }
    }
}
