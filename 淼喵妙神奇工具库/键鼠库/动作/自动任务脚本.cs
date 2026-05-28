using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading;
using 淼喵妙神奇工具库.键鼠库.动作.流程节点;
using 淼喵妙神奇工具库.感知库.识别;
using 淼喵妙神奇工具库.感知库;

namespace 淼喵妙神奇工具库.键鼠库.动作
{
    public class 自动任务脚本
    {
        public List<控制节点> 节点列表;
        public IntPtr HWnd => hWnd;
        IntPtr hWnd;
        public string 脚本备注 = "";
        public string 绑定进程名 = "";
        public string 绑定窗口标题 = "";
        public bool 是否为支线脚本 = false;
        public 自动任务脚本(IntPtr hWnd)
        {
            节点列表 = new List<控制节点>();
            this.hWnd = hWnd;
        }
        public 控制节点 解析(string 字符串)
        {
            Regex regex = new Regex(@"@([^:]+):");
            Match match = regex.Match(字符串);
            if (match.Success)
                {
                    switch (match.Groups[1].Value)
                    {
                        case "停止任务":
                            return 停止任务.解析(字符串, hWnd);
                        case "插队":
                            return 插队.解析(字符串, hWnd);
                        case "执行任务":
                            return 执行任务.解析(字符串, hWnd);
                        case "选定执行":
                            return 选定执行.解析(字符串, hWnd);
                        case "外部脚本":
                            return 外部脚本.解析(字符串, hWnd);
                        case "智能规划任务":
                            return 智能执行任务.解析(字符串, hWnd);
                        case "单击按键":
                            return 单击按键.解析(字符串, hWnd);
                        case "长按按键":
                            return 长按按键.解析(字符串, hWnd);
                        case "输入文本":
                            return 输入文本.解析(字符串, hWnd);
                        case "快捷键":
                            return 快捷键.解析(字符串, hWnd);
                        case "鼠标悬停":
                            return 鼠标悬停.解析(字符串, hWnd);
                        case "鼠标单击":
                            return 鼠标单击.解析(字符串, hWnd);
                        case "鼠标双击":
                            return 鼠标双击.解析(字符串, hWnd);
                        case "鼠标长按":
                            return 鼠标长按.解析(字符串, hWnd);
                        case "鼠标拖拽":
                            return 鼠标拖拽.解析(字符串, hWnd);
                        case "鼠标滚轮":
                            return 鼠标滚轮.解析(字符串, hWnd);
                        case "区域截图":
                            return 区域截图.解析(字符串, hWnd);
                        case "图片位置":
                            return 图片位置.解析(字符串, hWnd);
                        case "点击联想图片":
                            return 点击串联图片.解析(字符串, hWnd);
                        case "感应屏幕":
                            return 感应屏幕.解析(字符串, hWnd);
                        case "识别文字":
                            return 识别文字.解析(字符串, hWnd);
                        case "文字位置":
                            return 文字位置.解析(字符串, hWnd);
                        case "点击文字":
                            return 点击文字.解析(字符串, hWnd);
                        case "识别方框":
                            return 识别方框.解析(字符串, hWnd);
                        case "识别进度条":
                            return 识别进度条.解析(字符串, hWnd);
                        case "智能识别目标":
                            return 智能识别目标.解析(字符串, hWnd);
                        case "计算函数":
                            return 计算函数.解析(字符串, hWnd);
                        case "PowerShell指令":
                            return PowerShell指令.解析(字符串, hWnd);
                        case "打开应用":
                            return 打开应用.解析(字符串, hWnd);
                        case "绑定运行应用":
                            return 绑定运行应用.解析(字符串);
                        case "调整窗口尺寸":
                            return 调整窗口尺寸.解析(字符串, hWnd);
                        case "点击图片":
                            return 点击图片.解析(字符串, hWnd);
                        case "空节点":
                            return 空节点.解析(字符串, hWnd);
                        case "远程指令":
                            return 远程指令.解析(字符串, hWnd);
                        case "显示变量":
                            return 显示变量.解析(字符串, hWnd);
                        case "提示弹窗":
                            return 提示弹窗.解析(字符串, hWnd);
                        case "从列表中按序读取":
                            return 从列表中按序读取.解析(字符串, hWnd);
                        case "读取Excel到全局":
                            return 读取Excel到全局.解析(字符串, hWnd);
                        case "读取标题":
                            return 读取标题.解析(字符串, hWnd);
                        case "HTTP请求":
                            return HTTP请求.解析(字符串, hWnd);
                        case "CSharp脚本":
                            return CSharp脚本.解析(字符串, hWnd);
                        default:
                            return null;
                    }
                }
            return null;
        }
        public 自动任务脚本(IntPtr hWnd, string 字符串) : this(hWnd)
        {
            string[] 部分数组 = 字符串.Split('@');

            string 头部信息 = 部分数组[0].Trim();

            是否为支线脚本 = 头部信息.StartsWith("支线脚本[True]");
            if (是否为支线脚本)
                头部信息 = 头部信息.Substring("支线脚本[True]".Length);

            var 头部匹配 = Regex.Match(头部信息, @"进程名\[([^\]]*)\]窗口标题\[([^\]]*)\]");
            if (头部匹配.Success)
            {
                绑定进程名 = 头部匹配.Groups[1].Value;
                绑定窗口标题 = 头部匹配.Groups[2].Value;
                脚本备注 = 头部信息.Substring(头部匹配.Length).Trim();
            }
            else
            {
                脚本备注 = 头部信息;
            }

            var 节点数据列表 = 部分数组.Skip(1).Select(节点数据 => "@" + 节点数据.Trim()).ToList();
            var 解析后的节点数组 = 节点数据列表.AsParallel().Select(节点数据 => 解析(节点数据) ?? throw new Exception($"无法解析节点数据：{节点数据}")).ToArray();
            节点列表.AddRange(解析后的节点数组);

            部分数组.Skip(1).ToList().AsParallel().ForAll(节点数据 =>
            {
                int 索引 = Array.IndexOf(部分数组, 节点数据) - 1;
                if (索引 >= 0 && 索引 < 节点列表.Count)
                {
                    string 完整节点数据 = "@" + 节点数据.Trim();
                    控制节点.解析基类字段(节点列表[索引], 完整节点数据, this);
                }
            });
        }
        public bool 执行(int 起始节点索引 = 0, Action<string> 进度回调 = null)
        {
            if (节点列表.Count == 0) return false;
            var 计时器 = System.Diagnostics.Stopwatch.StartNew();
            int 总数 = 节点列表.Count;
            if (起始节点索引 < 0 || 起始节点索引 >= 节点列表.Count)
                throw new ArgumentOutOfRangeException(nameof(起始节点索引), "起始节点索引超出范围");
            节点列表.ForEach(node => node.初始化());


            控制节点 当前节点 = 节点列表[起始节点索引];
            Dictionary<string, object> 全局 = new Dictionary<string, object>();
            if (hWnd != IntPtr.Zero && 窗口处理器.IsWindow(hWnd))
                全局["文字识别器"] = 全局["取像识别器"] = new 文字识别器(窗口处理器.获取窗口框(hWnd).Size, 8);
            while (true)
            {
                if (当前节点.需要窗口句柄 && hWnd == IntPtr.Zero)
                {
                    throw new Exception($"节点 \"{当前节点.节点名字}\" 需要窗口句柄，但未绑定窗口！");
                }
                
                int 索引 = 进度回调 != null ? 节点列表.IndexOf(当前节点) : -1;
                进度回调?.Invoke($"{当前节点.节点名字}已执行{Math.Round(计时器.Elapsed.TotalSeconds, 1)}s  {索引 + 1}/{总数}");

                var 下一节点 = 当前节点.执行(hWnd, 全局);
                if (当前节点 is 绑定运行应用 绑定)
                    hWnd = 绑定.hWnd;
                if (当前节点.成功后等待 > 0 || 当前节点.失败后等待 > 0)
                {
                    进度回调?.Invoke($"{当前节点.节点名字}已等待{Math.Round(计时器.Elapsed.TotalSeconds, 1)}s  {索引 + 1}/{总数}");
                }
                switch (下一节点)
                {
                    case null:
                        int 当前节点索引 = 节点列表.IndexOf(当前节点);
                        if (当前节点索引 == -1)
                            throw new Exception("当前节点不在节点列表中");
                        if (当前节点索引 == 节点列表.Count - 1)
                            return true;
                        下一节点 = 节点列表[当前节点索引 + 1];
                        break;
                    case 停止任务 _:                        
                        return false;
                }
                当前节点 = 下一节点;
            }
        }
        public string 保存为字符串()
        {
            StringBuilder sb = new StringBuilder();
            if (是否为支线脚本)
                sb.Append($"支线脚本[True]");
            sb.Append($"进程名[{绑定进程名}]窗口标题[{绑定窗口标题}]{脚本备注}");
            foreach (var 节点 in 节点列表)
                sb.Append(节点.保存为字符串(this));
            return sb.ToString();
        }
    }
}
