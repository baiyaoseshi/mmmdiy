using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace 淼喵妙神奇工具库
{
    public static class 等待管理器
    {
        private enum 等待类型
        {
            时间等待,
            事件等待,
            AI对话完成等待
        }

        private class 等待上下文
        {
            public string 对话Id;
            public CancellationTokenSource Cts;
            public 等待类型 类型;
            public DateTime? 目标时间;
            public string 事件名称;
            public string 目标对话Id;
            public string 目标对话名;
        }

        private static readonly ConcurrentDictionary<string, 等待上下文> _活跃等待 = new ConcurrentDictionary<string, 等待上下文>();
        private static bool _已订阅任务触发;

        public static event Action<string, string, string> 等待完成;

        static 等待管理器()
        {
            EnsureTaskManagerSubscription();
        }

        private static void EnsureTaskManagerSubscription()
        {
            if (_已订阅任务触发) return;
            _已订阅任务触发 = true;
            键鼠库.动作.任务控制管理器.实例.命名任务已触发 += (任务名) =>
            {
                触发事件等待(任务名);
            };
        }

        public static void 创建时间等待(string 对话Id, DateTime 目标时间)
        {
            if (_活跃等待.TryGetValue(对话Id, out _))
                取消等待(对话Id);

            var cts = new CancellationTokenSource();
            var ctx = new 等待上下文
            {
                对话Id = 对话Id,
                Cts = cts,
                类型 = 等待类型.时间等待,
                目标时间 = 目标时间
            };
            _活跃等待[对话Id] = ctx;

            Task.Run(async () =>
            {
                var 延迟 = (目标时间 - DateTime.Now).TotalMilliseconds;
                if (延迟 <= 0)
                {
                    等待完成?.Invoke(对话Id, "⏰", $"已到达目标时间 {目标时间:yyyy-MM-dd HH:mm:ss}");
                    _活跃等待.TryRemove(对话Id, out _);
                    return;
                }

                try
                {
                    await Task.Delay((int)延迟, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (!cts.IsCancellationRequested)
                {
                    等待完成?.Invoke(对话Id, "⏰", $"时间等待完成，目标时间: {目标时间:yyyy-MM-dd HH:mm:ss}");
                    _活跃等待.TryRemove(对话Id, out _);
                }
            });
        }

        public static void 创建事件等待(string 对话Id, string 事件名称, int 超时分钟 = 30)
        {
            if (_活跃等待.TryGetValue(对话Id, out _))
                取消等待(对话Id);

            var cts = new CancellationTokenSource();
            var ctx = new 等待上下文
            {
                对话Id = 对话Id,
                Cts = cts,
                类型 = 等待类型.事件等待,
                事件名称 = 事件名称
            };
            _活跃等待[对话Id] = ctx;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(超时分钟 * 60 * 1000, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (!cts.IsCancellationRequested)
                {
                    等待完成?.Invoke(对话Id, "🔔超时", $"等待事件 \"{事件名称}\" 超时（{超时分钟}分钟）");
                    _活跃等待.TryRemove(对话Id, out _);
                }
            });
        }

        public static bool 取消等待(string 对话Id)
        {
            if (_活跃等待.TryRemove(对话Id, out var ctx))
            {
                ctx.Cts.Cancel();
                ctx.Cts.Dispose();
                return true;
            }
            return false;
        }

        public static void 创建AI对话完成等待(string 当前对话Id, string 目标对话名, int 超时分钟 = 30)
        {
            if (_活跃等待.TryGetValue(当前对话Id, out _))
                取消等待(当前对话Id);

            var 目标对话 = AI配置管理器.按名称查找对话(目标对话名);
            if (目标对话 == null) return;

            var cts = new CancellationTokenSource();
            var ctx = new 等待上下文
            {
                对话Id = 当前对话Id,
                Cts = cts,
                类型 = 等待类型.AI对话完成等待,
                目标对话Id = 目标对话.Id,
                目标对话名 = 目标对话名
            };
            _活跃等待[当前对话Id] = ctx;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(超时分钟 * 60 * 1000, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }

                if (!cts.IsCancellationRequested)
                {
                    等待完成?.Invoke(当前对话Id, "🔔超时", $"等待对话 \"{目标对话名}\" 的AI回复超时（{超时分钟}分钟）");
                    _活跃等待.TryRemove(当前对话Id, out _);
                }
            });
        }

        public static void 通知AI回复完成(string 对话Id, string 消息内容)
        {
            foreach (var kvp in _活跃等待)
            {
                if (kvp.Value.类型 == 等待类型.AI对话完成等待 && kvp.Value.目标对话Id == 对话Id)
                {
                    kvp.Value.Cts.Cancel();
                    等待完成?.Invoke(kvp.Key, "💬", 消息内容);
                    _活跃等待.TryRemove(kvp.Key, out _);
                    return;
                }
            }
        }

        public static void 触发事件等待(string 事件名称)
        {
            foreach (var kvp in _活跃等待)
            {
                if (kvp.Value.类型 == 等待类型.事件等待 && kvp.Value.事件名称 == 事件名称)
                {
                    kvp.Value.Cts.Cancel();
                    等待完成?.Invoke(kvp.Key, "🔔", $"事件 \"{事件名称}\" 已触发");
                    _活跃等待.TryRemove(kvp.Key, out _);
                    return;
                }
            }
        }
    }
}
