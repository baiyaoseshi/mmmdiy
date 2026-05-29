using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Newtonsoft.Json;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 读取Excel到全局 : 控制节点
    {
        public override bool 需要窗口句柄 => false;

        public string 文件路径 = "";
        public bool 读取为单个字典 = false;
        public string 字典变量名 = "excelData";

        读取Excel到全局() : base() { }

        public 读取Excel到全局(string 文件路径) : base()
        {
            this.文件路径 = 文件路径;
        }

        private static object 安全反序列化(string 原始值)
        {
            if (string.IsNullOrEmpty(原始值))
                return "";

            try
            {
                return JsonConvert.DeserializeObject(原始值);
            }
            catch
            {
                return 原始值;
            }
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (string.IsNullOrEmpty(文件路径) || !File.Exists(文件路径))
            {
                return false;
            }

            try
            {
                using var workbook = new XLWorkbook(文件路径);
                var worksheet = workbook.Worksheet(1);

                var rows = worksheet.RowsUsed();
                int 已读行数 = 0;
                if (读取为单个字典)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var row in rows)
                    {
                        string key = row.Cell(1).GetString();
                        if (string.IsNullOrEmpty(key))
                            continue;

                        string rawValue = row.Cell(2).GetString();
                        dict[key] = 安全反序列化(rawValue);
                        已读行数++;
                    }
                    全局[字典变量名] = dict;
                }
                else
                {
                    foreach (var row in rows)
                    {
                        string key = row.Cell(1).GetString();
                        if (string.IsNullOrEmpty(key))
                            continue;

                        string rawValue = row.Cell(2).GetString();
                        全局[key] = 安全反序列化(rawValue);
                        已读行数++;
                    }
                }

                if (已读行数 == 0)
                {
                    通知工具.吐司通知($"警告: 从Excel读取到0条数据，请检查文件内容");
                    return false;
                }

                通知工具.吐司通知($"从Excel读取到 {已读行数} 条数据");
                return true;
            }
            catch (Exception ex)
            {
                通知工具.吐司通知($"读取Excel失败: {ex.Message}");
                return false;
            }
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@读取Excel到全局:\n" +
                   $"文件路径[{文件路径}],\n" +
                   $"读取为单个字典[{读取为单个字典}],\n" +
                   $"字典变量名[{字典变量名}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 读取Excel到全局 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@读取Excel到全局:"))
            {
                var 节点 = new 读取Excel到全局();
                Match m;
                m = Regex.Match(字符串, @"文件路径\[([^\]]+)\]");
                if (m.Success) 节点.文件路径 = m.Groups[1].Value;
                m = Regex.Match(字符串, @"读取为单个字典\[([^\]]+)\]");
                if (m.Success) bool.TryParse(m.Groups[1].Value, out 节点.读取为单个字典);
                m = Regex.Match(字符串, @"字典变量名\[([^\]]+)\]");
                if (m.Success) 节点.字典变量名 = m.Groups[1].Value;
                return 节点;
            }
            return null;
        }

        public static 读取Excel到全局 创建节点(IntPtr hWnd)
        {
            string 文件路径 = 通知工具.输入弹窗("请输入Excel文件(.xlsx)的完整路径:", "", "");
            var 节点 = new 读取Excel到全局(文件路径);
            string 模式选择 = 通知工具.选项弹窗("读取模式", new List<string> { "逐行存入全局", "读取为单个字典" });
            if (模式选择 == "读取为单个字典")
            {
                节点.读取为单个字典 = true;
                string 变量名 = 通知工具.输入弹窗("请输入存储字典的全局变量名(默认excelData):", "", "");
                if (!string.IsNullOrEmpty(变量名))
                    节点.字典变量名 = 变量名;
            }
            return 节点;
        }
    }
}
