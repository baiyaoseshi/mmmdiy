using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class HTTP请求 : 控制节点
    {
        public string URL = "";
        public string HTTP方法 = "GET";
        public string 请求头 = "";
        public string 请求体 = "";
        public int 超时秒数 = 30;
        public string 输出变量名 = "";

        public override bool 需要窗口句柄 => false;

        HTTP请求() : base() { }

        public HTTP请求(string url, string http方法, string 请求头, string 请求体, int 超时秒数, string 输出变量名) : base()
        {
            this.URL = url;
            this.HTTP方法 = http方法;
            this.请求头 = 请求头;
            this.请求体 = 请求体;
            this.超时秒数 = 超时秒数;
            this.输出变量名 = 输出变量名;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(超时秒数);

                var httpMethod = new HttpMethod(HTTP方法.ToUpper());
                var request = new HttpRequestMessage(httpMethod, URL);

                if (!string.IsNullOrEmpty(请求头))
                {
                    var headerLines = 请求头.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in headerLines)
                    {
                        var trimmedLine = line.Trim();
                        if (string.IsNullOrEmpty(trimmedLine))
                            continue;
                        var colonIndex = trimmedLine.IndexOf(':');
                        if (colonIndex > 0)
                        {
                            var key = trimmedLine.Substring(0, colonIndex).Trim();
                            var value = trimmedLine.Substring(colonIndex + 1).Trim();
                            request.Headers.TryAddWithoutValidation(key, value);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(请求体)
                    && httpMethod != HttpMethod.Get
                    && httpMethod != HttpMethod.Head)
                {
                    request.Content = new StringContent(请求体, Encoding.UTF8, "application/json");
                }

                using var response = httpClient.SendAsync(request).GetAwaiter().GetResult();
                var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (response.IsSuccessStatusCode)
                {
                    if (!string.IsNullOrEmpty(输出变量名))
                    {
                        全局[输出变量名] = responseBody;
                    }
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            var safeHeaders = 请求头.Replace("\r\n", "\\n").Replace("\n", "\\n");
            var safeBody = 请求体.Replace("\r\n", "\\n").Replace("\n", "\\n");
            return "@HTTP请求:\n" +
                   $"URL[{URL}],\n" +
                   $"方法[{HTTP方法}],\n" +
                   $"请求头[{safeHeaders}],\n" +
                   $"请求体[{safeBody}],\n" +
                   $"超时秒数[{超时秒数}],\n" +
                   $"输出变量名[{输出变量名}],\n" +
                   base.保存为字符串(脚本);
        }

        public static HTTP请求 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@HTTP请求:"))
            {
                var 节点 = new HTTP请求();

                var regex = new Regex(@"URL\[([\s\S]*?)\],\n方法\[");
                var match = regex.Match(字符串);
                if (match.Success)
                    节点.URL = match.Groups[1].Value;

                regex = new Regex(@"方法\[([^\]]*)\]");
                match = regex.Match(字符串);
                if (match.Success)
                    节点.HTTP方法 = match.Groups[1].Value;

                regex = new Regex(@"请求头\[([\s\S]*?)\],\n请求体\[");
                match = regex.Match(字符串);
                if (match.Success)
                    节点.请求头 = match.Groups[1].Value.Replace("\\n", "\n");

                regex = new Regex(@"请求体\[([\s\S]*?)\],\n超时秒数\[");
                match = regex.Match(字符串);
                if (match.Success)
                    节点.请求体 = match.Groups[1].Value.Replace("\\n", "\n");

                regex = new Regex(@"超时秒数\[(\d+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                    节点.超时秒数 = int.Parse(match.Groups[1].Value);

                regex = new Regex(@"输出变量名\[([^\]]*)\]");
                match = regex.Match(字符串);
                if (match.Success)
                    节点.输出变量名 = match.Groups[1].Value;

                return 节点;
            }
            return null;
        }

        public static HTTP请求 创建节点(IntPtr hWnd)
        {
            string url = 通知工具.输入弹窗("请输入URL:", "HTTP请求", "");
            string 方法 = 通知工具.输入弹窗("请输入HTTP方法 (GET/POST/PUT/DELETE):", "HTTP请求", "GET");
            string 请求头 = 通知工具.输入弹窗("请输入请求头(每行一个 Key: Value):", "HTTP请求", "");
            string 请求体 = 通知工具.输入弹窗("请输入请求体:", "HTTP请求", "");
            string 超时 = 通知工具.输入弹窗("请输入超时秒数:", "HTTP请求", "30");
            int.TryParse(超时, out int 超时秒数);
            string 输出变量名 = 通知工具.输入弹窗("请输入输出变量名(可选):", "HTTP请求", "");
            return new HTTP请求(url, 方法, 请求头, 请求体, 超时秒数, 输出变量名);
        }
    }
}
