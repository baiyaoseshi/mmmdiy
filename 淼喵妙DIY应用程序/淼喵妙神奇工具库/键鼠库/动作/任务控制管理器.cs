using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace 淼喵妙神奇工具库.键鼠库.动作
{
    public class 任务控制管理器
    {
        private static readonly Lazy<任务控制管理器> _instance = new Lazy<任务控制管理器>(() => new 任务控制管理器());
        
        public static 任务控制管理器 实例 => _instance.Value;

        private class 执行上下文
        {
            public 自动任务脚本 脚本;
            public int 起始节点索引;
        }

        private ConcurrentQueue<执行上下文> 运行队列 = new ConcurrentQueue<执行上下文>();
        private Task 当前运行任务;
        private bool _isRunning;
        private bool _isPaused;
        private bool _isCanceled;
        
        public bool IsRunning => _isRunning;
        public bool IsPaused => _isPaused;
        public bool IsCanceled => _isCanceled;

        public event Action<string> 状态更新;
        public event Action<string> 插队请求;
        public event Action<自动任务脚本> 脚本执行开始;
        public event Action<自动任务脚本, bool, string> 脚本执行完成;
        public event Action<string> 命名任务已触发;

        private 任务控制管理器() { }

        public void 添加到队列(自动任务脚本 脚本, int 起始节点索引 = 0)
        {
            运行队列.Enqueue(new 执行上下文 { 脚本 = 脚本, 起始节点索引 = 起始节点索引 });
            状态更新?.Invoke($"脚本已加入队列，队列长度: {运行队列.Count}");
            如果不在运行则启动();
        }

        public void 优先入队(自动任务脚本 脚本, int 起始节点索引 = 0)
        {
            var 新队列 = new ConcurrentQueue<执行上下文>();
            新队列.Enqueue(new 执行上下文 { 脚本 = 脚本, 起始节点索引 = 起始节点索引 });
            while (运行队列.TryDequeue(out var ctx))
                新队列.Enqueue(ctx);
            运行队列 = 新队列;
            状态更新?.Invoke($"脚本已优先加入队列，队列长度: {运行队列.Count}");
            如果不在运行则启动();
        }

        private async void 如果不在运行则启动()
        {
            if (_isRunning || 当前运行任务 != null && !当前运行任务.IsCompleted)
                return;

            _isRunning = true;
            状态更新?.Invoke("开始处理任务队列...");
            
            while (运行队列.TryDequeue(out var 上下文))
            {
                重置取消();
                脚本执行开始?.Invoke(上下文.脚本);
                try
                {
                    状态更新?.Invoke($"正在运行脚本...");
                    当前运行任务 = Task.Run(() => 上下文.脚本.执行(上下文.起始节点索引));
                    await 当前运行任务;
                    状态更新?.Invoke("脚本执行完成");
                    脚本执行完成?.Invoke(上下文.脚本, true, null);
                }
                catch (OperationCanceledException)
                {
                    状态更新?.Invoke("脚本已取消");
                    脚本执行完成?.Invoke(上下文.脚本, false, "已取消");
                }
                catch (Exception ex)
                {
                    状态更新?.Invoke($"脚本执行出错: {ex.Message}");
                    脚本执行完成?.Invoke(上下文.脚本, false, ex.Message);
                }
            }

            _isRunning = false;
            状态更新?.Invoke("任务队列已处理完毕");
        }

        public void 清空队列()
        {
            while (运行队列.TryDequeue(out _)) { }
            状态更新?.Invoke("队列已清空");
        }

        public void 暂停()
        {
            _isPaused = true;
            状态更新?.Invoke("任务已暂停");
        }

        public void 继续()
        {
            _isPaused = false;
            状态更新?.Invoke("任务已继续");
        }

        public void 取消()
        {
            _isCanceled = true;
            状态更新?.Invoke("任务已取消");
        }

        public void 重置取消()
        {
            _isCanceled = false;
        }

        public void 触发插队请求(string 脚本路径)
        {
            插队请求?.Invoke(脚本路径);
        }

        public void 触发命名任务(string 任务名)
        {
            命名任务已触发?.Invoke(任务名);
            
            try
            {
                string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "淼喵妙脚本DIY", "userdata.json");
                if (!File.Exists(appData)) return;
                
                string json = File.ReadAllText(appData);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("触发任务列表", out var tasks)) return;
                
                foreach (var task in tasks.EnumerateArray())
                {
                    if (!task.TryGetProperty("任务名", out var nameProp)) continue;
                    if (!string.Equals(nameProp.GetString()?.Trim(), 任务名?.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                    if (task.TryGetProperty("是否启用", out var enabled) && !enabled.GetBoolean()) continue;
                    if (!task.TryGetProperty("脚本路径", out var scriptPath)) continue;
                    
                    string path = scriptPath.GetString();
                    if (!File.Exists(path)) continue;
                    
                    string content = File.ReadAllText(path);
                    var 脚本 = new 自动任务脚本(IntPtr.Zero, content);
                    添加到队列(脚本);
                    return;
                }
            }
            catch { }
        }
    }
}
