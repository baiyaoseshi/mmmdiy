using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace 淼喵妙神奇工具库.输出库
{
    public static class 通知工具
    {
        private static Action<string> _信息弹窗UI;
        private static Func<string, string, string, string> _输入弹窗UI;
        private static Action<string> _吐司通知UI;
        private static Func<string, bool> _确认弹窗UI;
        private static Action<string> _错误弹窗UI;
        private static Action<string> _警告弹窗UI;
        private static Func<string, List<string>, string> _选项弹窗UI;
        private static Action<string, List<Bitmap>> _图片查看器UI;

        public static void 初始化UI(
            Action<string> 信息弹窗,
            Func<string, string, string, string> 输入弹窗,
            Action<string> 吐司通知,
            Func<string, bool> 确认弹窗,
            Action<string> 错误弹窗,
            Action<string> 警告弹窗,
            Func<string, List<string>, string> 选项弹窗,
            Action<string, List<Bitmap>> 图片查看器)
        {
            _信息弹窗UI = 信息弹窗;
            _输入弹窗UI = 输入弹窗;
            _吐司通知UI = 吐司通知;
            _确认弹窗UI = 确认弹窗;
            _错误弹窗UI = 错误弹窗;
            _警告弹窗UI = 警告弹窗;
            _选项弹窗UI = 选项弹窗;
            _图片查看器UI = 图片查看器;
        }

        public static void 信息弹窗(string 消息)
        {
            if (MCP工具管理器.是否在MCP上下文中)
            {
                MCP工具管理器.追加输出($"[通知] {消息}");
                return;
            }
            _信息弹窗UI?.Invoke(消息);
        }

        public static void 吐司通知(string 消息)
        {
            if (MCP工具管理器.是否在MCP上下文中)
            {
                MCP工具管理器.追加输出($"[提示] {消息}");
                return;
            }
            _吐司通知UI?.Invoke(消息);
        }

        public static void 错误弹窗(string 消息)
        {
            if (MCP工具管理器.是否在MCP上下文中)
            {
                MCP工具管理器.追加输出($"[错误-需关注] {消息}");
                return;
            }
            _错误弹窗UI?.Invoke(消息);
        }

        public static void 警告弹窗(string 消息)
        {
            if (MCP工具管理器.是否在MCP上下文中)
            {
                MCP工具管理器.追加输出($"[警告-请注意] {消息}");
                return;
            }
            _警告弹窗UI?.Invoke(消息);
        }

        public static void 图片查看器(string 标题, List<Bitmap> 图片列表)
        {
            if (MCP工具管理器.是否在MCP上下文中)
            {
                var count = 图片列表?.Count ?? 0;
                var sb = new StringBuilder();
                sb.AppendLine($"[图片组] {标题}: {count} 张图片");
                if (图片列表 != null)
                {
                    for (int i = 0; i < 图片列表.Count; i++)
                        sb.AppendLine($"  {i + 1}. 尺寸: {图片列表[i].Width}×{图片列表[i].Height}");
                }
                sb.Append("[注意] 这是图片组信息摘要，完整图片内容需要AI支持图片查看功能");
                MCP工具管理器.追加输出(sb.ToString());
                return;
            }
            _图片查看器UI?.Invoke(标题, 图片列表);
        }

        public static bool 确认弹窗(string 消息)
        {
            if (MCP工具管理器.是否在MCP上下文中)
            {
                MCP工具管理器.追加输出($"[交互-确认] {消息}\n(脚本自动选择了\"否\"以继续执行，如需要可选择\"是\")");
                return false;
            }
            return _确认弹窗UI?.Invoke(消息) ?? false;
        }

        public static string 输入弹窗(string 提示, string 标题, string 默认值)
        {
            if (MCP工具管理器.是否在MCP上下文中)
            {
                string 默认显示 = string.IsNullOrEmpty(默认值) ? "\"\"" : 默认值;
                MCP工具管理器.追加输出($"[交互-输入] {提示}\n(脚本自动填充了默认值: {默认显示})");
                return 默认值 ?? "";
            }
            return _输入弹窗UI?.Invoke(提示, 标题, 默认值) ?? "";
        }

        public static string 选项弹窗(string 提示, List<string> 选项列表)
        {
            if (MCP工具管理器.是否在MCP上下文中)
            {
                string 选项文本 = 选项列表 != null && 选项列表.Count > 0
                    ? string.Join(", ", 选项列表)
                    : "(空)";
                string 默认选项 = 选项列表 != null && 选项列表.Count > 0 ? 选项列表[0] : "";
                MCP工具管理器.追加输出($"[交互-选择] {提示}: [{选项文本}]\n(脚本自动选择了: {默认选项})");
                return 默认选项;
            }
            return _选项弹窗UI?.Invoke(提示, 选项列表) ?? "";
        }
    }
}
