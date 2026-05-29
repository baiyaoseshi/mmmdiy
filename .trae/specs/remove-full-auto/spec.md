# 移除全自动概念 Spec

## Why
"全自动"（Full Auto）模式是一个独立的 AI 对话模式概念，包含专属的 MCP 工具集、独立 AI 配置、特殊 System Prompt 和 UI 开关。实际使用中"全自动"与"半自动"的界限模糊——AI 在普通对话中也能通过通用工具完成脚本创建等操作。移除这一概念可以简化代码、减少维护负担，将所有能力统一到通用工具集中。

## What Changes

### 删除（能删就删）
- **删除文件**：`全自动MCP工具.cs`、`全自动日志.cs`
- **删除模型字段**：`AIPersistenceData.全局全自动AI配置`
- **删除配置管理器方法**：`获取全局全自动AI配置()`、`更新全局全自动AI配置()`、`获取全自动SystemPrompt()`
- **删除 UI 元素**：`AIChatControl.xaml` 中的 `FullAutoHint` TextBlock
- **删除方法**：`AIChatControl.设置全自动模式()`
- **删除 XAML 控件组**：`GlobalAISettings.xaml` 中 `FullAuto*` 命名的视觉 AI 配置 UI 区域

### 合并重命名（不能删就合并）
- **全自动 MCP 工具 → 通用 MCP 工具**：12 个工具（list_node_types, get_node_creation_rules, create_working_script, add_node, remove_node, modify_node, execute_node, save_script, list_all_scripts, classify_script, edit_category_rule, create_category）合并至 `通用MCP工具.cs`，内置工具ID列表从 9 个增至 21 个
- **全自动日志 → 通用日志**：`记录()` 和 `记录攻略()` 方法合并至 `通用MCP工具.cs` 的日志功能中（写到 `logs/` 根目录）
- **全局全自动AI配置 → 视觉AI配置**：重命名为 `视觉AI配置`，去掉"全自动"前缀，全局设置面板中已有「视觉 AI 配置」标题保持不变
- **`分析屏幕截图()` 引用更新**：从 `获取全局全自动AI配置()` 改为 `获取视觉AI配置()`
- **System Prompt 合并**：全自动专属 System Prompt 中「循环检测」「加权质量评估」「命名要求」等通用指导原则合并到通用工具提示词中

### 移除（从调用方剥离）
- **AIChatPage.xaml.cs**：移除 `全自动MCP工具.获取全自动工具列表()` 调用和 `获取全自动SystemPrompt()` 拼接（两处：普通发送 + `/spec` 等待回调）

## Impact
- Affected files:
  - `全自动MCP工具.cs` — **删除**
  - `全自动日志.cs` — **删除**
  - `AI配置模型.cs` — 移除 `全局全自动AI配置`，新增 `视觉AI配置`
  - `AI配置管理器.cs` — 移除 3 个方法，新增 `获取视觉AI配置()`/`更新视觉AI配置()`
  - `通用MCP工具.cs` — 合并 12 个全自动工具 + 日志功能，更新 `分析屏幕截图()` 引用
  - `MCP工具管理器.cs` — 移除全自动工具分发路由
  - `AIChatPage.xaml.cs` — 移除全自动工具加载和 System Prompt 拼接
  - `AIChatControl.xaml` — 移除 FullAutoHint
  - `AIChatControl.xaml.cs` — 移除 `设置全自动模式()`
  - `GlobalAISettings.xaml` — 移除 FullAuto* UI 控件组
  - `GlobalAISettings.xaml.cs` — 移除 FullAuto 相关加载/保存/事件处理代码

## REMOVED Requirements

### Requirement: 全自动模式
**Reason**：全自动模式概念与普通对话模式功能重叠，AI 在普通模式下也能调用通用工具完成脚本创建等任务。
**Migration**：全自动专用 MCP 工具合并至通用 MCP 工具，所有对话均可使用；视觉 AI 配置保留为独立配置字段但移除"全自动"命名。

### Requirement: 全自动独立 System Prompt
**Reason**：总指挥 System Prompt 中的最佳实践（循环检测、质量评估、命名要求）合并入通用工具提示词。
**Migration**：N/A

## MODIFIED Requirements

### Requirement: 视觉 AI 配置
系统 SHALL 提供独立的视觉 AI 配置（用于 `analyze_screen` 工具），与普通对话 AI 配置**分离**。

- 字段命名从 `全局全自动AI配置` 改为 `视觉AI配置`
- 全局设置面板中的「视觉 AI 配置」区域保持，但 C# 变量名从 `FullAuto*` 改为 `Vision*`
- 未配置时回退到全局 AI 配置

### Requirement: 通用 MCP 工具集
`通用MCP工具.cs` 内置工具ID列表从 9 个扩展至 21 个，合并原全自动 MCP 工具的 12 个工具。

| 分类 | 工具ID（新增标 ⭐） |
|------|--------|
| 视觉分析 | analyze_screen |
| 脚本查询 | expand_script, search_scripts, list_all_scripts ⭐, list_node_types ⭐ |
| 脚本编辑 | modify_script_remark, create_working_script ⭐, add_node ⭐, remove_node ⭐, modify_node ⭐, execute_node ⭐, save_script ⭐ |
| 脚本管理 | classify_script ⭐, edit_category_rule ⭐, create_category ⭐ |
| 规则与参考 | get_node_creation_rules ⭐ |
| 等待 | wait_until_time, wait_for_event |
| 搜索 | web_search |
| 日志 | write_log, read_log |

## ADDED Requirements

### Requirement: 日志功能合并
原 `全自动日志.cs` 的 `记录()` 和 `记录攻略()` 方法合并为 `通用MCP工具.cs` 的私有辅助函数，写到 `logs/` 根目录（不再单独建 `auto_mode`/`strategies` 子目录）。
