# Tasks

- [x] Task 1: 截图方法移到工具库 + 修复 Ollama 视觉支持
  - [x] 在 `AI配置管理器.cs` 中添加 `public static string 捕获屏幕截图()` — 全屏截图返回 base64 PNG data URI
  - [x] 重写 `调用AI分析截图()`：移除提供者类型限制，Ollama 走 `OllamaApiClient` generate + `images` 字段，OpenAI 保持原路径
  - [x] 将 `AIChatPage.xaml.cs` 的 `private static string 捕获屏幕截图()` 改为调用 `AI配置管理器.捕获屏幕截图()`
  - [x] 编译验证两个项目

- [x] Task 2: 删除全自动/半自动模式区分（数据模型 + UI）
  - [x] `AI配置模型.cs` — 删除 `AIConversation.是否全自动模式` 字段
  - [x] `AIChatPage.xaml` — 删除 ToggleButton 和提示文字
  - [x] `AIChatPage.xaml.cs` — 删除模式切换事件处理；`OnMessageSend` 移除 `if(对话.是否全自动模式)` 条件，删除自动截图代码块，工具范围统一为：对话分类脚本 + 全自动工具 + 通用工具，规则统一为：全局规则 + 对话规则 + 全自动 System Prompt；`OnWaitCompleted` 同样统一
  - [x] `AIConversationSettings.xaml/cs` — 删除「⚡ 全自动模式 AI 配置」区域
  - [x] `GlobalAISettings.xaml/cs` — 视觉 AI 配置区域始终可见
  - [x] 编译验证两个项目

- [x] Task 3: 新增 `analyze_screen` 通用MCP工具
  - [x] 在 `通用MCP工具.cs` 的 `获取通用工具列表()` 中添加 `analyze_screen` 工具定义
  - [x] `内置工具ID列表` 中添加 `"analyze_screen"`
  - [x] `执行内置工具()` switch 中添加 `"analyze_screen"` case → 调 `分析屏幕截图(参数)`
  - [x] 实现 `分析屏幕截图(参数)`：截图 → 调 `AI配置管理器.调用AI分析截图()` → 返回结果
  - [x] 编译验证

- [x] Task 4: 更新系统提示词
  - [x] `【通用MCP工具使用指南】` 添加 `analyze_screen` 说明，引导 AI 在需要查看屏幕时主动调用
  - [x] `获取全自动SystemPrompt()` 移除「视觉能力」段（不再自动提供截图分析）
  - [x] 编译验证

# Task Dependencies
- Task 2 依赖 Task 1
- Task 3 依赖 Task 1 & 2
- Task 4 独立，可与 Task 1-3 并行
