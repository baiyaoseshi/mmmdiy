# Checklist

## `locate_on_screen` 工具
- [x] 工具定义注册在 `获取通用工具列表()` 中
- [x] 工具 ID 在 `内置工具ID列表` 中
- [x] `执行内置工具()` switch 包含 `"locate_on_screen"` case
- [x] 描述参数必填校验
- [x] 视觉 AI 配置无效时返回友好提示
- [x] Prompt 引导视觉 AI 返回 JSON 格式
- [x] 成功时返回 `{"x": N, "y": N, "description": "..."}` 
- [x] 视觉 AI 返回 `{"error": "..."}` 时透传错误信息
- [x] JSON 解析失败时返回错误提示 + 原始 AI 输出

## `find_window` 工具
- [x] 工具定义注册在 `获取通用工具列表()` 中
- [x] 工具 ID 在 `内置工具ID列表` 中
- [x] `执行内置工具()` switch 包含 `"find_window"` case
- [x] 窗口标题参数必填校验
- [x] 找到窗口时返回 title + rect(x, y, width, height)
- [x] 未找到时返回 `{"found": false}`
- [x] 复用 `窗口处理器.查找窗口()` 和 `窗口处理器.获取窗口框()`

## 编译
- [x] `dotnet build` 工具库通过
