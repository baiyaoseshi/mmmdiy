# Checklist

## 截图方法统一
- [x] `AI配置管理器.捕获屏幕截图()` public static，返回 base64 PNG data URI
- [x] `AIChatPage.捕获屏幕截图()` 改为调用工具库方法

## Ollama 视觉模型支持（核心修复）
- [x] `调用AI分析截图()` 不再限制提供者类型为 OpenAI only
- [x] Ollama 路径：`OllamaApiClient` generate + `images` 字段
- [x] OpenAI 路径保持原有行为不变

## 全自动/半自动模式统一
- [x] `AIConversation.是否全自动模式` 字段已删除
- [x] ToggleButton 和提示文字已从 `AIChatPage.xaml` 删除
- [x] `OnMessageSend` 无 `if(对话.是否全自动模式)` 条件分支
- [x] `OnMessageSend` 中**自动截图代码块已删除**（不再自动截图）
- [x] `OnWaitCompleted` 无 `if(对话.是否全自动模式)` 条件分支
- [x] 对话设置面板「⚡ 全自动模式 AI 配置」区域已删除
- [x] 全局设置面板视觉 AI 配置区域始终可见
- [x] 所有对话统一加载：对话分类脚本 + 全自动专用工具 + 通用工具
- [x] 所有对话统一规则：全局规则 + 对话规则 + 全自动 System Prompt
- [x] 工具分类仍按对话各自筛选（`对话.工具库分类列表` 未改动）

## analyze_screen 工具
- [x] `获取通用工具列表()` 中包含 `analyze_screen`
- [x] `工具ID` = `"analyze_screen"`，`内置工具ID列表` 包含 `"analyze_screen"`
- [x] 参数 Schema：`关注点` 可选 string
- [x] `执行内置工具()` switch 有 `"analyze_screen"` case
- [x] 视觉 AI 配置有效时返回分析结果
- [x] 视觉 AI 配置无效时返回中文配置指引

## 系统提示词
- [x] `【通用MCP工具使用指南】` 包含 `analyze_screen` 使用说明
- [x] `获取全自动SystemPrompt()` 不再描述自动截图（改为引导使用 `analyze_screen` 工具）
- [x] 提示词引导 AI 适时调用 `analyze_screen`

## 编译与回归
- [x] `dotnet build` 工具库通过
- [x] `dotnet build` 用户界面通过
- [x] 全自动模式功能在统一模式下正常工作
