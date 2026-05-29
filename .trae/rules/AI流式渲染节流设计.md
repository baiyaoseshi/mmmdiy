# AI 流式渲染节流设计

## 设计目标

AI 流式输出时，AI 流线程和 UI 线程完全解耦。AI 线程通过 `BeginInvoke` 投递 UI 更新（不阻塞），靠 `Barrier(2)` 作为唯一跨线程同步点等 UI 处理完毕。

## 线程模型

```
AI流线程 (SSE read loop):                    UI线程:
  BeginInvoke(追加AI回复) ──────────────→     追加AI回复(文本)
    (不等待，立即返回)                           _内容 += 文本
                                               文本更新完成?.Invoke()
  _屏障.SignalAndWait() ←── Barrier(2) ──→   handler:
    (阻塞等UI + 渲染)                            检查旧task
                                               Task.Run(async () => {
                                                 await Background      ← 等渲染
                                                 _屏障.SignalAndWait()
                                               })
  (继续下一chunk)
```

## 核心组件

### AIChatControl — 事件触发

```csharp
public event Action 文本更新完成;
public event Action 思考更新完成;

void 追加AI回复(string 文本)
{
    _内容 += 文本;
    文本更新完成?.Invoke();   // UI 更新完成立即通知
}
```

**不碰 Barrier，不调用 SignalAndWait。**

### AIChatPage — Barrier + 事件订阅

字段：
```csharp
private readonly Barrier _文本屏障 = new Barrier(2);
private Task _文本同步Task;
private readonly Barrier _思考屏障 = new Barrier(2);
private Task _思考同步Task;
```

订阅（仅检查 + 按需创建 Task.Run）：
```csharp
ChatControl.文本更新完成 += () =>
{
    if (_文本同步Task == null || _文本同步Task.IsCompleted)
        _文本同步Task = Task.Run(async () =>
        {
            await Dispatcher.InvokeAsync(() => { }, Background);  // 等渲染完成
            _文本屏障.SignalAndWait();                              // 通知 AI 线程
        });
};
```

### AI流线程回调

```csharp
Func<string, Task> 文本回调 = chunk =>
{
    Dispatcher.BeginInvoke(new Action(() => ChatControl.追加AI回复(chunk)));
    _文本屏障.SignalAndWait();
    return Task.CompletedTask;
};
```

### AI配置管理器

所有 `await` 须加 `.ConfigureAwait(false)` 确保 SSE 循环在 ThreadPool 而非 UI 线程上运行：
- `PostAsync` / `ReadAsStreamAsync` / `ReadLineAsync`
- `await foreach` (IAsyncEnumerable)
- 思考回调须 `await`

## ⚠️ 重点

- Barrier(2) 只创建一次，文本/思考各一个独立实例
- 事件 handler 只做状态检查 + 按需创建 Task.Run，**Task.Run 内必须 await Background 等渲染后再 SignalAndWait**
- AIChatControl 不碰 Barrier
- AI 流线程回调用 `BeginInvoke`（投递不阻塞）+ `Barrier.SignalAndWait()`（阻塞等同方）
- **务必 `.ConfigureAwait(false)`** 否则 SSE 循环在 UI 线程上自我死锁
- 非必要不要修改此设计
