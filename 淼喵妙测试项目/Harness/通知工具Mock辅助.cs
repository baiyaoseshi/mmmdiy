using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙测试项目.Harness
{
    public static class 通知工具Mock辅助
    {
        public static List<string> 信息弹窗记录 { get; } = new();
        public static List<string> 吐司通知记录 { get; } = new();
        public static List<string> 错误弹窗记录 { get; } = new();
        public static List<string> 警告弹窗记录 { get; } = new();
        public static List<(string 提示, string 标题, string 默认值)> 输入弹窗记录 { get; } = new();
        public static List<string> 确认弹窗记录 { get; } = new();
        public static List<(string 提示, List<string> 选项列表)> 选项弹窗记录 { get; } = new();
        public static List<(string 标题, List<Bitmap> 图片列表)> 图片查看器记录 { get; } = new();

        public static Func<string, bool> 确认弹窗行为 { get; set; } = _ => false;
        public static Func<string, string, string, string> 输入弹窗行为 { get; set; } = (_, _, 默认值) => 默认值 ?? "";
        public static Func<string, List<string>, string> 选项弹窗行为 { get; set; } = (_, 选项列表) => 选项列表?.FirstOrDefault() ?? "";

        public static void 安装Mock通知()
        {
            清空记录();

            通知工具.初始化UI(
                信息弹窗: msg => 信息弹窗记录.Add(msg),
                输入弹窗: (提示, 标题, 默认值) =>
                {
                    输入弹窗记录.Add((提示, 标题, 默认值));
                    return 输入弹窗行为(提示, 标题, 默认值);
                },
                吐司通知: msg => 吐司通知记录.Add(msg),
                确认弹窗: msg =>
                {
                    确认弹窗记录.Add(msg);
                    return 确认弹窗行为(msg);
                },
                错误弹窗: msg => 错误弹窗记录.Add(msg),
                警告弹窗: msg => 警告弹窗记录.Add(msg),
                选项弹窗: (提示, 选项列表) =>
                {
                    选项弹窗记录.Add((提示, 选项列表));
                    return 选项弹窗行为(提示, 选项列表);
                },
                图片查看器: (标题, 图片列表) => 图片查看器记录.Add((标题, 图片列表))
            );
        }

        public static void 清空记录()
        {
            信息弹窗记录.Clear();
            吐司通知记录.Clear();
            错误弹窗记录.Clear();
            警告弹窗记录.Clear();
            输入弹窗记录.Clear();
            确认弹窗记录.Clear();
            选项弹窗记录.Clear();
            图片查看器记录.Clear();
        }
    }
}
