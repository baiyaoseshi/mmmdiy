# 灵活视觉委托 + 内置指令 + 编辑工作流精简 Spec

## Why
当前通用MCP工具中有两个固定的多模态AI工具（`analyze_screen` 和 `locate_on_screen`），文本AI只能以固定方式使用它们，无法灵活构造视觉分析Prompt。同时缺少内置指令（`/web`、`/screen`）来快速约束AI行为，且脚本编辑系统Prompt中推荐了"网上搜索攻略"步骤，不必要地增加了AI调用web_search的频率。

## What Changes
- 用单一通用工具 `ask_vision_ai` 替代 `analyze_screen` 和 `locate_on_screen`，文本AI可自定义任意视觉分析Prompt
- 添加两个内置指令 `/web` 和 `/screen`，输入 `/` 时自动弹出菜单中可见
- 从脚本编辑工作流的系统Prompt中移除 `web_search` 推荐步骤
- 同步更新 `构建提示词()` 和 `构建消息列表()` 两个方法中的隐藏系统Prompt

## Impact
- Affected specs: 通用MCP工具、AI聊天指令、AI配置管理器
- Affected code:
  - `通用MCP工具.cs` — 替换两个多模态工具为 `ask_vision_ai`，更新 `内置工具ID列表`
  - `AI配置管理器.cs` — 更新 `构建提示词()` 和 `构建消息列表()` 中的系统Prompt
  - `AIChatPage.xaml.cs` — 添加 `/web` 和 `/screen` 指令处理
  - `AIChatControl.xaml.cs` — 添加内置指令到快捷指令匹配逻辑

## ADDED Requirements

### Requirement: 通用视觉AI委托工具
系统 SHALL 提供一个单一的通用视觉AI工具 `ask_vision_ai`，替代现有的 `analyze_screen` 和 `locate_on_screen`。

#### Scenario: 文本AI灵活调用视觉AI分析屏幕
- **WHEN** 文本AI需要获取屏幕视觉信息
- **THEN** 文本AI调用 `ask_vision_ai` 工具，传入自定义Prompt（如"屏幕上有哪些按钮？"、"登录按钮在什么位置？返回JSON坐标"等）
- **THEN** 系统截图后调用视觉AI，将视觉AI的回复直接返回给文本AI

#### Scenario: 未配置视觉AI
- **WHEN** 文本AI调用 `ask_vision_ai` 但视觉AI未配置
- **THEN** 返回配置指引信息，提示用户先配置视觉AI

#### Scenario: 视觉AI调用失败
- **WHEN** 视觉AI返回空结果或调用异常
- **THEN** 返回友好的错误提示，包含可能原因和排查建议

### Requirement: /web 内置指令
系统 SHALL 支持 `/web` 内置指令，强制AI在回答前先搜索互联网。

#### Scenario: 用户输入 /web 指令
- **WHEN** 用户输入 `/web <查询内容>` 并发送
- **THEN** 系统在用户消息前追加系统指令："【强制要求】在回答之前，你必须**首先**调用 web_search 工具搜索互联网上的相关信息。结合搜索结果来回答用户的问题。如果 web_search 未配置（返回配置指引），则直接告知用户需要先配置 Google API Key 和 CX。"
- **THEN** 走正常流式带工具对话流程（AI可调用所有工具）

#### Scenario: /web 出现在快捷指令弹出菜单中
- **WHEN** 用户输入 `/` 开头文本
- **THEN** `/web` 作为内置指令出现在快捷指令弹出菜单中
- **THEN** 选中后填入 `/web ` 前缀，用户继续输入查询内容后发送

### Requirement: /screen 内置指令
系统 SHALL 支持 `/screen` 内置指令，强制AI在回答前先查看屏幕。

#### Scenario: 用户输入 /screen 指令
- **WHEN** 用户输入 `/screen <问题>` 并发送
- **THEN** 系统在用户消息前追加系统指令："【强制要求】在回答之前，你必须**首先**调用 ask_vision_ai 工具查看当前屏幕。结合屏幕内容来回答用户的问题。如果视觉AI未配置（返回配置指引），则直接告知用户需要先配置视觉AI。"
- **THEN** 走正常流式带工具对话流程

#### Scenario: /screen 出现在快捷指令弹出菜单中
- **WHEN** 用户输入 `/` 开头文本
- **THEN** `/screen` 作为内置指令出现在快捷指令弹出菜单中
- **THEN** 选中后填入 `/screen ` 前缀，用户继续输入问题后发送

## MODIFIED Requirements

### Requirement: 脚本学习与模仿工作流Prompt
系统 SHALL 在脚本编辑相关的系统Prompt中**移除**对 `web_search` 的推荐步骤。

#### Scenario: 脚本编辑工作流不再推荐联网搜索
- **WHEN** AI收到脚本编辑类系统Prompt
- **THEN** Prompt中不包含任何引导AI调用 `web_search` 的步骤
- **THEN** `web_search` 工具定义仍然存在且可用（对话级开关控制），但AI不会因Prompt而主动调用

## REMOVED Requirements

### Requirement: analyze_screen 工具
**Reason**: 被 `ask_vision_ai` 替代，功能合并到单一灵活工具中。
**Migration**: 所有对 `analyze_screen` 的引用替换为 `ask_vision_ai`，系统Prompt中相应更新。

### Requirement: locate_on_screen 工具
**Reason**: 被 `ask_vision_ai` 替代。文本AI可通过自定义Prompt（如 "请定位登录按钮，返回格式 `{"x": 整数, "y": 整数, "description": "..."}` 或 `{"error": "..."}`"）实现同样功能。
**Migration**: 所有对 `locate_on_screen` 的引用替换为 `ask_vision_ai`，系统Prompt中相应更新。
