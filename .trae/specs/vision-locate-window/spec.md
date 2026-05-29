# 视觉AI定位与窗口查找 MCP 工具 Spec

## Why
当前 `analyze_screen` 工具只能返回屏幕的文字描述。文字 AI 拿到描述后无法精确知道某个 UI 元素的像素坐标来进行鼠标操作，也无法快速通过窗口标题获取窗口的屏幕矩形。需要两个新 MCP 工具弥合"看到"和"操作"之间的鸿沟：

1. **视觉定位**：让视觉 AI 根据自然语言描述找到目标元素的像素坐标，返回精确的 `(x, y)` 给文字 AI 用于后续鼠标操作
2. **窗口查找**：根据窗口标题查找窗口句柄，返回窗口矩形框，让 AI 知道目标窗口在屏幕上的位置和大小

## What Changes

### 新增工具 1：`locate_on_screen`（屏幕上定位）
- 截图 → 视觉 AI 分析 → 返回像素坐标
- Prompt 要求视觉 AI 输出 JSON 格式：`{"x": 像素X, "y": 像素Y, "description": "描述"}` 或 `{"error": "未找到原因"}`
- 参数：`描述`（必填，要定位的 UI 元素描述，如「登录按钮」「右上角的X关闭按钮」「搜索输入框」）
- 参数：`关注点`（可选，辅助提示词）

### 新增工具 2：`find_window`（查找窗口）
- 按标题（部分匹配）枚举所有可见窗口，返回窗口矩形
- 参数：`窗口标题`（必填，部分匹配）
- 返回：`{"found": true, "title": "完整标题", "rect": {"left": 0, "top": 0, "right": 800, "bottom": 600}}` 或 `{"found": false}`
- 复用现有 `窗口处理器.查找窗口()` 和 `窗口处理器.获取窗口框()` 方法

### 视觉 AI 配置引用
- 两个工具的视角 AI 部分复用现有 `视觉AI配置`（原 `全局全自动AI配置`），即「视觉 AI 配置」中配置的多模态模型
- `find_window` 不需要视觉 AI，纯 Win32 API 调用

## Impact
- Affected files: `通用MCP工具.cs`（新增 2 个工具定义 + 执行逻辑）
- 不涉及其他文件修改

## ADDED Requirements

### Requirement: 视觉 AI 像素定位
系统 SHALL 提供 `locate_on_screen` 工具，通过视觉 AI 在屏幕截图上定位目标 UI 元素并返回像素坐标。

#### Scenario: 成功定位
- **WHEN** 调用 `locate_on_screen`，参数 `描述` = "登录按钮"
- **THEN** 系统截屏 → 发送给视觉 AI，使用特化 prompt 要求返回 JSON
- **AND** 视觉 AI 返回 `{"x": 450, "y": 320, "description": "蓝色登录按钮，位于窗口中央偏下"}`
- **AND** 工具将坐标和描述返回给文字 AI

#### Scenario: 未找到
- **WHEN** 视觉 AI 在屏幕上找不到目标元素
- **THEN** 返回 `{"error": "未找到登录按钮，可能不在当前屏幕中"}`

#### Scenario: 视觉 AI 未配置
- **WHEN** 视觉 AI 配置无效
- **THEN** 返回配置提示（与 `analyze_screen` 一致的错误处理）

### Requirement: 窗口标题查找
系统 SHALL 提供 `find_window` 工具，根据窗口标题（部分匹配）返回窗口的屏幕矩形。

#### Scenario: 找到窗口
- **WHEN** 调用 `find_window`，参数 `窗口标题` = "记事本"
- **THEN** 系统通过 `窗口处理器` 枚举窗口，找到标题包含"记事本"的可见窗口
- **AND** 返回 `{"found": true, "title": "无标题 - 记事本", "rect": {"x": 100, "y": 200, "width": 800, "height": 600}}`

#### Scenario: 未找到
- **WHEN** 未找到匹配的可见窗口
- **THEN** 返回 `{"found": false}`
