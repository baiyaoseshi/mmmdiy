# Tasks

- [x] Task 1: 新增 `locate_on_screen` 工具
  - [x] 在 `获取通用工具列表()` 中注册 `locate_on_screen` 工具定义
  - [x] 在 `内置工具ID列表` 中添加 `"locate_on_screen"`
  - [x] 实现 `定位屏幕元素()` 私有方法：
    - 获取视觉 AI 配置（`AI配置管理器.获取视觉AI配置()`）
    - 参数验证：`描述` 必填
    - 构造特化 prompt：要求视觉 AI 返回 JSON `{"x":<int>, "y":<int>, "description":"..."}` 或 `{"error":"..."}`
    - 截屏 → 调用 `AI配置管理器.调用AI分析截图()`
    - 解析返回的 JSON，提取坐标和描述
    - 未找到或解析失败时返回友好错误信息
  - [x] 在 `执行内置工具()` switch 中添加 `"locate_on_screen"` case

- [x] Task 2: 新增 `find_window` 工具
  - [x] 在 `获取通用工具列表()` 中注册 `find_window` 工具定义
  - [x] 在 `内置工具ID列表` 中添加 `"find_window"`
  - [x] 实现 `查找窗口框()` 私有方法：
    - 参数：`窗口标题`（必填）
    - 调用 `窗口处理器.查找窗口(窗口标题)` 获取句柄
    - 调用 `窗口处理器.获取窗口框(hWnd)` 获取矩形
    - 返回 JSON：`{"found":true, "title":"...", "rect":{"x":..., "y":..., "width":..., "height":...}}` 或 `{"found":false}`
  - [x] 在 `执行内置工具()` switch 中添加 `"find_window"` case
  - [x] 确保 `using` 引用 `窗口处理器` 命名空间

- [x] Task 3: 编译验证
  - [x] `dotnet build` 工具库通过

# Task Dependencies
- Task 1 和 Task 2 独立，可并行
- Task 3 依赖 Task 1, 2
