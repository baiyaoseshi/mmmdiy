using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 外部脚本 : 控制节点
    {
        public string 脚本路径 = "";
        public string 解释器 = "python";
        public List<string> 输入变量名列表 = new List<string>();
        public string 输出变量名 = "";
        public int 超时秒数 = 60;

        public override bool 需要窗口句柄 => false;

        外部脚本() : base() { }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            try
            {
                string 绝对路径 = 脚本扩展工具.解析脚本路径(脚本路径);

                if (!File.Exists(绝对路径))
                {
                    通知工具.错误弹窗($"外部脚本文件不存在: {绝对路径}");
                    return false;
                }

                var 输入数据 = new Dictionary<string, object>();
                foreach (string 变量名 in 输入变量名列表)
                {
                    if (!string.IsNullOrEmpty(变量名) && 全局.TryGetValue(变量名, out object 值))
                    {
                        输入数据[变量名] = 值;
                    }
                }
                string 输入Json = JsonConvert.SerializeObject(输入数据);

                var psi = new ProcessStartInfo
                {
                    FileName = 解释器,
                    Arguments = $"\"{绝对路径}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                process.Start();

                process.StandardInput.Write(输入Json);
                process.StandardInput.Close();

                string stdout = process.StandardOutput.ReadToEnd();

                int 超时毫秒 = 超时秒数 * 1000;
                if (!process.WaitForExit(超时毫秒))
                {
                    process.Kill();
                    通知工具.错误弹窗($"外部脚本执行超时（{超时秒数}秒）: {绝对路径}");
                    return false;
                }

                if (process.ExitCode != 0)
                {
                    string stderr = process.StandardError.ReadToEnd();
                    if (!string.IsNullOrEmpty(stderr))
                    {
                        通知工具.错误弹窗($"外部脚本执行失败（退出码: {process.ExitCode}）:\n{stderr}");
                    }
                    else
                    {
                        通知工具.错误弹窗($"外部脚本执行失败（退出码: {process.ExitCode}）: {绝对路径}");
                    }
                    return false;
                }

                if (!string.IsNullOrEmpty(输出变量名))
                {
                    string 结果文本 = stdout.Trim();
                    if (!string.IsNullOrEmpty(结果文本))
                    {
                        try
                        {
                            object 解析结果 = 解析输出结果(结果文本);
                            全局[输出变量名] = 解析结果;
                        }
                        catch (Exception ex)
                        {
                            通知工具.错误弹窗($"解析外部脚本输出失败: {ex.Message}");
                            return false;
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                string 错误信息 = $"执行外部脚本时出错: {ex.Message}";
                try
                {
                    通知工具.错误弹窗(错误信息);
                }
                catch { }
                return false;
            }
        }

        private static object 解析输出结果(string 结果文本)
        {
            try
            {
                var token = JToken.Parse(结果文本);
                switch (token.Type)
                {
                    case JTokenType.Integer:
                        return token.Value<long>();
                    case JTokenType.Float:
                        return token.Value<double>();
                    case JTokenType.Boolean:
                        return token.Value<bool>();
                    case JTokenType.String:
                        return token.Value<string>();
                    default:
                        return token;
                }
            }
            catch
            {
                return 结果文本;
            }
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            string 输入变量字符串 = string.Join(";", 输入变量名列表);
            return "@外部脚本:\n" +
                   $"脚本路径[{脚本路径}],\n" +
                   $"解释器[{解释器}],\n" +
                   $"输入变量[{输入变量字符串}],\n" +
                   $"输出变量名[{输出变量名}],\n" +
                   $"超时秒数[{超时秒数}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 外部脚本 解析(string 字符串, IntPtr hWnd)
        {
            if (!字符串.StartsWith("@外部脚本:"))
                return null;

            var 节点 = new 外部脚本();

            Regex regex = new Regex(@"脚本路径\[([^\]]*)\]");
            Match match = regex.Match(字符串);
            if (match.Success)
                节点.脚本路径 = match.Groups[1].Value;

            regex = new Regex(@"解释器\[([^\]]*)\]");
            match = regex.Match(字符串);
            if (match.Success)
                节点.解释器 = match.Groups[1].Value;

            regex = new Regex(@"输入变量\[([^\]]*)\]");
            match = regex.Match(字符串);
            if (match.Success && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                节点.输入变量名列表 = match.Groups[1].Value
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            regex = new Regex(@"输出变量名\[([^\]]*)\]");
            match = regex.Match(字符串);
            if (match.Success)
                节点.输出变量名 = match.Groups[1].Value;

            regex = new Regex(@"超时秒数\[([^\]]*)\]");
            match = regex.Match(字符串);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int 超时))
                节点.超时秒数 = 超时;

            控制节点.解析基类字段(节点, 字符串, null);

            return 节点;
        }

        public static 外部脚本 创建节点(IntPtr hWnd)
        {
            通知工具.信息弹窗(脚本扩展工具.获取目录提示文本());

            string 解释器值 = 通知工具.输入弹窗("请输入解释器命令（如 python、node、pwsh）:", "解释器", "python");
            if (string.IsNullOrEmpty(解释器值))
                return null;

            bool 新建模板 = 通知工具.确认弹窗("是否新建脚本模板文件？");

            string 脚本路径值 = "";

            if (新建模板)
            {
                string 文件名 = 通知工具.输入弹窗("请输入脚本文件名（如 myscript.py）:", "新建脚本", "");
                if (string.IsNullOrEmpty(文件名))
                    return null;

                string 目录 = 脚本扩展工具.获取推荐脚本目录();
                string 完整路径 = Path.Combine(目录, 文件名);

                if (解释器值.Contains("python", StringComparison.OrdinalIgnoreCase))
                    脚本扩展工具.创建Python模板(完整路径);
                else if (解释器值.Contains("node", StringComparison.OrdinalIgnoreCase))
                    脚本扩展工具.创建NodeJs模板(完整路径);
                else
                    创建通用模板(完整路径);

                脚本扩展工具.用关联程序打开(完整路径);
                脚本路径值 = 完整路径;
            }
            else
            {
                脚本路径值 = 通知工具.输入弹窗("请输入脚本路径（支持相对路径）:", "脚本路径", "");
                if (string.IsNullOrEmpty(脚本路径值))
                    return null;
            }

            string 输入变量字符串 = 通知工具.输入弹窗("请输入输入变量名（多个用分号分隔，可为空）:", "输入变量", "");
            List<string> 输入变量列表 = string.IsNullOrEmpty(输入变量字符串)
                ? new List<string>()
                : 输入变量字符串.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();

            string 输出变量 = 通知工具.输入弹窗("请输入输出变量名（可选，脚本 stdout 输出将写入此变量）:", "输出变量", "");

            string 超时字符串 = 通知工具.输入弹窗("请输入超时秒数:", "超时", "60");
            int 超时 = 60;
            if (!string.IsNullOrEmpty(超时字符串))
                int.TryParse(超时字符串, out 超时);

            return new 外部脚本
            {
                脚本路径 = 脚本路径值,
                解释器 = 解释器值,
                输入变量名列表 = 输入变量列表,
                输出变量名 = 输出变量,
                超时秒数 = 超时
            };
        }

        private static void 创建通用模板(string 文件路径)
        {
            var 内容 = $"#!/usr/bin/env bash\n# 脚本模板 - 用于「外部脚本」节点\n# 全局变量通过 stdin 以 JSON 格式传入\n# 返回值通过 stdout 以 JSON 格式输出\n\necho 'Hello from script'\n";
            File.WriteAllText(文件路径, 内容, Encoding.UTF8);
        }
    }
}
