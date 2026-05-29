# 统一全自动模式 + 按需截图分析 Spec

## Why
全自动模式下已有截图→视觉AI分析→注入 `[屏幕截图分析]` 的代理机制，但有两个致命缺陷：
1. **`调用AI分析截图()` 只支持 OpenAI 兼容 API**，Ollama（如 llava）被直接 return null，截图白拍
2. **截图分析仅在消息发送前自动触发一次**，AI 无法在对话中主动请求重新截图分析

修复后统一全自动/半自动模式：所有对话共享全自动 System Prompt 和全自动 MCP 工具，截图分析由 AI 通过 MCP 工具**按需主动调用**，不再自动触发。工具分类仍按对话各自筛选。

## What Changes
- 删除全自动/半自动模式切换 UI（ToggleButton、提示文字、对话设置中的全自动 AI 配置区域）
- 删除 `AIConversation.是否全自动模式` 字段
- 删除发送消息前的自动截图逻辑（不再每次自动截图，改为 AI 按需调用）
- **修复 `调用AI分析截图()`**：新增 Ollama 本地视觉模型支持
- 新增 `通用MCP工具`：`analyze_screen`，让 AI **按需主动调用**截图分析
- 将 `捕获屏幕截图()` 从 `AIChatPage` 搬到工具库
- 所有对话统一：全自动 System Prompt + 全自动 MCP 工具 + 通用工具
- 工具分类仍按对话各自筛选（`对话.工具库分类列表` 保持不变）
- 更新系统提示词，引导 AI 适时使用 `analyze_screen` 查看屏幕

## Impact
- Affected code:
  - `AI配置模型.cs` — 删除 `是否全自动模式` 字段
  - `AI配置管理器.cs` — 修复 `调用AI分析截图()` 支持 Ollama + 提取 `捕获屏幕截图()`
  - `通用MCP工具.cs` — 新增 `analyze_screen` 内置工具
  - `AIChatPage.xaml` — 删除 ToggleButton + 提示文字
  - `AIChatPage.xaml.cs` — 删除模式切换逻辑，删除自动截图代码，统一全自动 System Prompt + 全自动 MCP 工具
  - `AIConversationSettings.xaml/cs` — 删除全自动 AI 配置区域
  - `GlobalAISettings.xaml/cs` — 视觉 AI 配置区域始终可见
  - `全自动MCP工具.cs` — 始终加载
- 不改变 OpenAI API 截图分析路径
- 不改变 `全局全自动AI配置` 字段名
- **不改变** 对话各自的工具分类筛选

## ADDED Requirements

### Requirement: Ollama 本地视觉模型支持截图分析
`调用AI分析截图()` SHALL 支持 Ollama 本地，通过 `OllamaApiClient` 发送含 `images` 字段的 generate 请求。

#### Scenario: Ollama 视觉成功
- **WHEN** 视觉AI配置为 Ollama（如 llava:latest）
- **THEN** 返回文字描述

### Requirement: AI 按需截图分析（analyze_screen 工具）
系统 SHALL 提供 `analyze_screen` 通用 MCP 工具，AI **主动调用**才截图分析，不自动触发。

#### Scenario: AI 按需查看屏幕
- **WHEN** AI 调用 `analyze_screen`
- **THEN** 截图 → 视觉 AI 分析 → 返回描述

#### Scenario: 视觉 AI 未配置
- **WHEN** AI 调用但配置无效
- **THEN** 返回中文配置指引

#### Scenario: 指定关注点
- **WHEN** 传入 `关注点` 参数
- **THEN** 提示词中拼接关注点引导

### Requirement: 统一截图实现
截图功能从 UI 层提取到工具库层。

## REMOVED Requirements

### Requirement: 全自动/半自动模式切换
**Reason**: 统一为全自动 System Prompt + 全自动 MCP 工具，截图改为按需调用
**Migration**: 
- `是否全自动模式` 字段删除，JSON 反序列化兼容
- ToggleButton 和提示文字删除
- 自动截图逻辑删除
- 对话设置面板中全自动 AI 配置区域移至全局设置

### Requirement: 发送前自动截图分析
**Reason**: 截图分析改为 AI 按需调用 `analyze_screen`，而非每次自动触发
