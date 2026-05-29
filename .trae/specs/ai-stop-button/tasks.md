# Tasks

- [x] Task 1: AIChatControl 添加打断事件和按钮状态切换
  - [x] 添加 `事件 Action 打断请求` 事件
  - [x] 修改 `SendButton_Click`：根据 `_isSending` 状态决定触发 `消息发送` 还是 `打断请求`
  - [x] 修改 `设置是否正在发送` 使按钮文字和颜色根据 `IsSending` 动态切换（发送=蓝色"发送"，停止=红色"停止"）
  - [x] Enter 键也支持打断：当 `_isSending` 时按 Enter 触发打断

- [x] Task 2: AIChatPage 管理 CancellationTokenSource 并处理打断
  - [x] 添加字段 `CancellationTokenSource _aiCts`
  - [x] 在构造函数中订阅 `ChatControl.打断请求 += OnStopRequest`
  - [x] `OnStopRequest` 中调用 `_aiCts?.Cancel()`
  - [x] `OnMessageSend` 中创建新的 `_aiCts`，将 `.Token` 传入 `调用AI流式带工具`
  - [x] `OnMessageSend` 的 `catch` 中捕获 `OperationCanceledException`，追加系统消息"⏹ AI 回复已被用户中止"
  - [x] `OnWaitCompleted` 中同样传递 `CancellationToken`

- [x] Task 3: AI配置管理器 流式方法增加 CancellationToken 支持
  - [x] `调用AI流式` 和 `调用AI流式带工具` 方法签名增加 `CancellationToken cancellationToken = default` 参数
  - [x] `调用本地AI流式带工具` 增加 `CancellationToken` 参数，在 `await foreach` 循环每次迭代后调用 `cancellationToken.ThrowIfCancellationRequested()`
  - [x] `调用远程AI流式带工具` 增加 `CancellationToken` 参数，在 while 循环和 ReadLineAsync 后检查取消
  - [x] 递归调用 `调用本地AI流式带工具`（工具执行后再次调用 AI）时传递同一 token
  - [x] 修复 bare `catch` 块吞掉 `OperationCanceledException` 的问题

- [x] Task 4: 编译验证
  - [x] 编译工具库项目确认无错误
  - [x] 编译用户界面项目确认无错误

# Task Dependencies
- Task 2 depends on Task 1（打断事件需要在 AIChatPage 中订阅）
- Task 3 depends on Task 2（CancellationToken 参数需要在 AIChatPage 调用时传入）
- Task 4 depends on Task 1, Task 2, Task 3
