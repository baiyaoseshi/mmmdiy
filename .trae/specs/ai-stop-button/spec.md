# AI 对话打断按钮 Spec

## Why
当前 AI 流式输出一旦开始就无法中止，用户必须等待完整回复（或异常发生）。需要添加打断按钮，让用户在 AI 输出过程中能主动取消，提升交互体验。

## What Changes
- 发送按钮在 AI 输出期间自动变为"停止"按钮（红色醒目样式），点击后取消正在进行的 AI 请求
- `AI配置管理器` 的流式方法新增 `CancellationToken` 参数，在 SSE 读取循环和本地 Ollama 生成循环中检查取消
- `AIChatPage` 管理 `CancellationTokenSource`，订阅打断事件并触发取消
- `AIChatControl` 新增 `打断请求` 事件，按钮点击时根据 `IsSending` 状态发送不同事件

## Impact
- Affected specs: 无（新功能）
- Affected code:
  - `AIChatControl.xaml` — 按钮样式切换
  - `AIChatControl.xaml.cs` — 打断事件、按钮逻辑
  - `AIChatPage.xaml.cs` — CancellationTokenSource 管理、打断处理
  - `AI配置管理器.cs` — CancellationToken 贯穿流式调用链

## ADDED Requirements

### Requirement: AI 输出打断按钮
系统 SHALL 在 AI 流式输出期间将发送按钮变为停止按钮，允许用户取消正在进行的 AI 请求。

#### Scenario: 正常发送消息
- **WHEN** 用户在输入框中输入文本并点击"发送"按钮（或按 Enter）
- **AND** 当前没有 AI 正在回复
- **THEN** 系统发送消息，按钮变为红色"停止"按钮

#### Scenario: AI 输出中点击停止
- **WHEN** AI 正在流式输出回复
- **AND** 用户点击"停止"按钮
- **THEN** 系统取消正在进行的 AI 请求
- **AND** AI 的当前已输出内容保留在对话中（标记为已打断）
- **AND** 按钮恢复为蓝色"发送"按钮
- **AND** 输入框恢复可用

#### Scenario: 停止按钮视觉样式
- **WHEN** AI 正在回复（`IsSending == true`）
- **THEN** 按钮显示文字"停止"，背景色为红色（`#FFF44336`），替代原来的蓝色"发送"按钮

#### Scenario: 取消已完成的 AI 回复不应出错
- **WHEN** AI 回复已经自然完成
- **AND** 按钮已恢复为"发送"
- **THEN** 点击发送正常发送新消息（不触发打断逻辑）

### Requirement: CancellationToken 贯穿流式调用链
系统 SHALL 在 AI 配置管理器的流式调用方法中支持 `CancellationToken`，在 SSE/本地生成循环中响应取消。

#### Scenario: 取消远程 AI 流式请求
- **WHEN** 远程 AI SSE 流正在接收数据
- **AND** `CancellationToken` 被触发
- **THEN** SSE 读取循环中的 `ReadLineAsync` 抛出 `OperationCanceledException`
- **AND** 方法向上传播 `OperationCanceledException`

#### Scenario: 取消本地 Ollama 流式请求
- **WHEN** 本地 Ollama 正在流式生成
- **AND** `CancellationToken` 被触发
- **THEN** `await foreach` 循环中的取消检查生效
- **AND** 方法抛出 `OperationCanceledException`

#### Scenario: 工具调用中取消
- **WHEN** AI 正在执行工具调用循环（多轮）
- **AND** `CancellationToken` 被触发
- **THEN** 工具调用循环终止，方法抛出 `OperationCanceledException`
