# AI 消息隐私分层 & 脚本解析修复 Spec

## Why
当前 AI 对话系统中存在两类问题：
1. **消息泄露**：AI 的隐藏提示词（如屏幕分析结果、节点创建规则表等）及工具执行中间结果被持久化到对话历史，在 UI 上显示给用户，干扰阅读且暴露内部机制。
2. **脚本解析缺陷**：`.script` 文件保存格式为 `进程名[...]窗口标题[...]备注@节点1:\n...\n@节点2:\n...`——头部与第一个 `@` 之间**没有** `\n`。`expand_script` 使用 `IndexOf("\n@")` 定位头部/节点边界，由于第一个 `@` 前无 `\n`，会错误地跳过头部→节点1的分界，转而命中节点1→节点2之间的 `\n@`，导致节点1被吞入头部、节点2成为"节点 0"。同样地 `search_scripts` 中 `ReadLines().FirstOrDefault()` 读取第一行时也会把 `@绑定运行应用:...` 一并读入备注。

## What Changes
- `AIChatMessage` 新增 `是否私有` 布尔字段（默认 `false`），标记消息仅对 AI 可见、不对用户 UI 展示
- `AIChatControl` 在渲染消息列表时过滤掉 `是否私有 == true` 的消息
- `AI配置管理器` 的 `构建消息列表()` / `构建提示词()` 包含私有消息（正常传给 LLM）
- 以下系统消息改为私有（仅 AI 可见）：`/screen`、`/web` 的强制提示、工具执行结果、工具进度消息
- `search_scripts` 输出修正：仅显示脚本名和备注（截断 `@` 之后内容）
- `expand_script` 节点解析修复：`IndexOf("\n@")` → `IndexOf("@")` 正确找到头部与第一个节点的边界

## Impact
- Affected specs: `script-inspect-tool`（expand_script/search_scripts 输出格式修复）
- Affected code:
  - `AI配置模型.cs` — `AIChatMessage` 新增 `是否私有` 字段
  - `AIChatControl.xaml.cs` — `设置消息()` 等过滤私有消息
  - `AIChatPage.xaml.cs` — 标记特定系统消息为私有
  - `AI配置管理器.cs` — 工具执行结果标记为私有
  - `通用MCP工具.cs` — 修复 `expand_script` 节点边界查找 + `search_scripts` 备注截断

## ADDED Requirements

### Requirement: 消息隐私标记
系统 SHALL 在 `AIChatMessage` 中提供 `是否私有` 布尔字段（默认 `false`），标记消息仅对 AI 可见。

#### Scenario: 默认公开
- **WHEN** 新建 `AIChatMessage` 实例
- **THEN** `是否私有` 为 `false`

#### Scenario: 私有消息不显示
- **WHEN** UI 渲染消息列表
- **THEN** `是否私有 == true` 的消息不参与 UI 渲染

#### Scenario: 私有消息参与 LLM 请求
- **WHEN** 构建 LLM 请求（`构建消息列表()` / `构建提示词()`）
- **THEN** 私有消息与公开消息同样被包含

#### Scenario: 私有消息持久化
- **WHEN** 对话数据保存到 `ai_config.json`
- **THEN** `是否私有` 字段被序列化保存，下次加载恢复

### Requirement: 系统消息私有化
以下系统消息 SHALL 标记为私有（`是否私有 = true`）。

#### Scenario: /screen 强制提示私有化
- **WHEN** 用户发送 `/screen` 指令
- **THEN** 注入的 `【强制要求】...调用 ask_vision_ai...` 标记为私有

#### Scenario: /web 强制提示私有化
- **WHEN** 用户发送 `/web` 指令
- **THEN** 注入的 `【强制要求】...调用 web_search...` 标记为私有

#### Scenario: 工具执行结果私有化
- **WHEN** AI 调用工具后结果被追加到对话历史
- **THEN** 该条系统消息标记为私有

#### Scenario: 工具进度消息私有化
- **WHEN** `AIChatPage` 中 `开始工具进度()` 或 `添加工具消息()` 产出系统消息
- **THEN** 该条消息标记为私有

### Requirement: expand_script 头部/节点边界修复
`expand_script` SHALL 使用 `IndexOf("@")` 查找第一个 `@` 来定位头部与第一个节点的边界，而非 `IndexOf("\n@")`。

#### 根因
`.script` 保存格式为 `进程名[...]窗口标题[...]备注@节点1:\n...\n@节点2:\n...`，头部与第一个 `@` 之间无 `\n`。`IndexOf("\n@")` 会跳过它，命中节点1→节点2之间的 `\n@`，吞掉节点1。

#### Scenario: 正确分离头部与第一个节点
- **GIVEN** 脚本文件 `进程名[CraveSaga]窗口标题[...]经过主页到达团体战救援页面@绑定运行应用:\n...\n@点击图片:\n...`
- **WHEN** `expand_script` 展开该脚本
- **THEN** 输出包含 `节点 0: [绑定运行应用]` 和 `节点 1: [点击图片]`，共 2 个节点

### Requirement: search_scripts 备注截断修复
`search_scripts` 在解析头部备注时 SHALL 截断第一个 `@` 之后的内容。

#### 根因
`ReadLines().FirstOrDefault()` 返回头部行：`进程名[...]备注@绑定运行应用:...`，正则 `(.*)$` 捕获到 `备注@绑定运行应用:...`。

#### Scenario: 正确提取备注
- **GIVEN** 脚本头部 `进程名[CraveSaga]窗口标题[...]一段备注@绑定运行应用:...`
- **WHEN** `search_scripts` 解析该脚本
- **THEN** 返回 `- 脚本名: 一段备注`（不含 `@绑定运行应用:...`）

## MODIFIED Requirements

### Requirement: AIChatMessage 数据模型（修改）
`AIChatMessage` 新增 `是否私有` 布尔字段（默认 `false`），通过 `INotifyPropertyChanged` 支持绑定。
