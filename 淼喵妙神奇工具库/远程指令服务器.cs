using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace 淼喵妙神奇工具库
{
    public enum 远程指令类型
    {
        AI指令,
        任务指令,
        计划指令
    }

    public class 远程指令消息
    {
        public 远程指令类型 类型 { get; set; }
        public string 原始消息 { get; set; }
        public string 对话标签 { get; set; }
        public string 消息内容 { get; set; }
        public string 任务名 { get; set; }
    }

    public static partial class 远程指令服务器
    {
        private static HttpListener _httpListener;
        private static Thread _监听线程;
        private static volatile bool _是否运行;
        private static string _认证令牌;
        private static readonly ConcurrentQueue<string> _计划队列 = new ConcurrentQueue<string>();

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static event Action<远程指令消息> 收到AI指令;
        public static event Action<远程指令消息> 收到任务指令;

        public static bool 是否运行中 => _是否运行;
        public static int 计划队列长度 => _计划队列.Count;

        private static readonly Regex 指令模式 = 指令模式Regex();

        public static bool 启动(int 端口, string 令牌)
        {
            if (_是否运行) return true;

            _认证令牌 = 令牌 ?? "";

            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://127.0.0.1:{端口}/");
                _httpListener.Prefixes.Add($"http://localhost:{端口}/");
                foreach (var ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        _httpListener.Prefixes.Add($"http://{ip}:{端口}/");
                    }
                }
                _httpListener.Start();
            }
            catch
            {
                return false;
            }

            _是否运行 = true;
            _监听线程 = new Thread(监听循环) { IsBackground = true };
            _监听线程.Start();
            return true;
        }

        public static void 停止()
        {
            if (!_是否运行) return;
            _是否运行 = false;

            try { _httpListener?.Stop(); } catch { }
            try { _httpListener?.Close(); } catch { }
            _httpListener = null;
        }

        private static void 监听循环()
        {
            while (_是否运行)
            {
                try
                {
                    var context = _httpListener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => 处理请求(context));
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch
                {
                    break;
                }
            }
        }

        private static void 处理请求(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                response.ContentType = "application/json; charset=utf-8";

                var path = request.Url?.AbsolutePath?.TrimEnd('/') ?? "/";

                if (path == "" || path == "/")
                {
                    响应JSON(response, 200, new { status = "running", queueSize = _计划队列.Count });
                    return;
                }

                if (path == "/cmd")
                {
                    处理指令请求(request, response);
                    return;
                }

                响应JSON(response, 404, new { error = "not found" });
            }
            catch
            {
                try { context.Response.StatusCode = 500; context.Response.Close(); } catch { }
            }
        }

        private static void 处理指令请求(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (!string.IsNullOrEmpty(_认证令牌))
            {
                var auth = request.Headers["Authorization"];
                if (auth == null || !auth.StartsWith("Bearer ") || auth.Substring(7) != _认证令牌)
                {
                    响应JSON(response, 401, new { error = "unauthorized" });
                    return;
                }
            }

            string 消息 = null;

            if (request.HttpMethod == "GET")
            {
                消息 = request.QueryString["msg"];
            }
            else if (request.HttpMethod == "POST")
            {
                using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                {
                    var body = reader.ReadToEnd();
                    try
                    {
                        var json = JsonSerializer.Deserialize<JsonElement>(body);
                        if (json.TryGetProperty("msg", out var msgProp))
                            消息 = msgProp.GetString();
                    }
                    catch { }
                }
            }

            if (string.IsNullOrEmpty(消息))
            {
                响应JSON(response, 400, new { error = "missing msg" });
                return;
            }

            var 解析结果 = 解析指令(消息);
            路由指令(解析结果);
            响应JSON(response, 200, new { ok = true, type = 解析结果.类型.ToString() });
        }

        private static 远程指令消息 解析指令(string 文本)
        {
            文本 = 文本.Trim();
            var 结果 = new 远程指令消息 { 原始消息 = 文本 };

            if (文本.StartsWith("/2AI ") || 文本.StartsWith("/2ai "))
            {
                结果.类型 = 远程指令类型.AI指令;
                var 剩余 = 文本.Substring(5).TrimStart();
                var 空格位置 = 剩余.IndexOf(' ');
                if (空格位置 > 0)
                {
                    结果.对话标签 = 剩余.Substring(0, 空格位置).Trim();
                    结果.消息内容 = 剩余.Substring(空格位置 + 1).Trim();
                }
                else
                {
                    结果.对话标签 = 剩余;
                    结果.消息内容 = "";
                }
            }
            else if (文本.StartsWith("/task ") || 文本.StartsWith("/Task "))
            {
                结果.类型 = 远程指令类型.任务指令;
                结果.任务名 = 文本.Substring(6).Trim();
            }
            else if (文本.StartsWith("/plan ") || 文本.StartsWith("/Plan "))
            {
                结果.类型 = 远程指令类型.计划指令;
                结果.消息内容 = 文本.Substring(6).Trim();
            }
            else
            {
                结果.类型 = 远程指令类型.计划指令;
                结果.消息内容 = 文本;
            }

            return 结果;
        }

        private static void 路由指令(远程指令消息 指令)
        {
            switch (指令.类型)
            {
                case 远程指令类型.AI指令:
                    收到AI指令?.Invoke(指令);
                    break;
                case 远程指令类型.任务指令:
                    收到任务指令?.Invoke(指令);
                    break;
                case 远程指令类型.计划指令:
                    _计划队列.Enqueue(指令.消息内容 ?? 指令.原始消息);
                    break;
            }
        }

        public static bool 取计划指令(int 超时毫秒, out string 指令)
        {
            指令 = null;
            var 截止时间 = DateTime.Now.AddMilliseconds(超时毫秒);

            while (_是否运行 && DateTime.Now < 截止时间)
            {
                if (_计划队列.TryDequeue(out 指令))
                    return true;
                Thread.Sleep(100);
            }

            while (_是否运行 && _计划队列.TryDequeue(out 指令))
                return true;

            return false;
        }

        private static void 响应JSON(HttpListenerResponse response, int statusCode, object data)
        {
            response.StatusCode = statusCode;
            var json = JsonSerializer.Serialize(data, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            response.OutputStream.Write(bytes, 0, bytes.Length);
            response.Close();
        }

        [System.Text.RegularExpressions.GeneratedRegex(@"^/\S+")]
        private static partial Regex 指令模式Regex();
    }
}
