using Newtonsoft.Json;
using OpenCvSharp;
using System;
using System.Drawing;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.键鼠库.动作.流程节点;

namespace 淼喵妙神奇工具库.感知库.串联
{
    [Serializable]
    public class 串联图片数据
    {
        public string 模板数据 { get; set; } = "";
        public double 识别阈值 { get; set; } = 0.8;
        public string 目标文本 { get; set; } = "";
        public bool 是文字识别 { get; set; } = false;
        public Rectangle 校准参数 { get; set; } = Rectangle.Empty;
        public string 初始校准参数字符串 { get; set; } = "";
        public 串联图片数据? 附加 { get; set; } = null;
        public System.Drawing.Size 窗口框尺寸 { get; set; } = System.Drawing.Size.Empty;
        public 串联图片数据() { }

        public static 串联图片数据 从串联图创建(串联图片 串联图)
        {
            if (串联图 == null) return null;
            
            var 数据 = new 串联图片数据();
            if (!串联图.是文字识别 && 串联图.模板 != null)
                数据.模板数据 = Convert.ToBase64String(串联图.模板.ImEncode(".png"));
            数据.识别阈值 = 串联图.识别阈值;
            数据.目标文本 = 串联图.目标文本;
            数据.是文字识别 = 串联图.是文字识别;
            数据.校准参数 = 串联图.校准参数;
            if (串联图.初始校准参数 != null)
            {
                数据.初始校准参数字符串 = 节点参数.序列化(串联图.初始校准参数);
            }
            if (串联图.附加 != null)
            {
                数据.附加 = 从串联图创建((串联图片)串联图.附加);
            }
            
            return 数据;
        }

        public 串联图片 还原为串联图()
        {
            串联图片 串联图;
            
            if (是文字识别 && !string.IsNullOrEmpty(目标文本))
            {
                串联图 = new 串联图片(目标文本);
            }
            else if (!string.IsNullOrEmpty(模板数据))
            {
                串联图 = new 串联图片(Cv2.ImDecode(Convert.FromBase64String(模板数据), OpenCvSharp.ImreadModes.Color), 识别阈值);
            }
            else
            {
                return null;
            }
            
            串联图.校准参数 = 校准参数;
            if (!string.IsNullOrEmpty(初始校准参数字符串))
            {
                串联图.初始校准参数 = 节点参数.反序列化<Rectangle>(初始校准参数字符串);
            }

            if (附加 != null)
            {
                串联图.附加 = 附加.还原为串联图();
            }
            
            return 串联图;
        }

        public string 序列化()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static 串联图片数据 反序列化(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<串联图片数据>(json);
        }
    }
}