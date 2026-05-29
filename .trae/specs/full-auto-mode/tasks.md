# Tasks

- [x] Task 1: 数据模型扩展
  - [x] `AIConversation` 添加 `是否全自动模式` (bool)
  - [x] `AIChatMessage` 添加 `图片列表` (`List<string>` base64)
  - [x] `AIConversation` 添加 `全自动AI配置` (`AIConfigData`)
  - [x] 编译验证

- [x] Task 2: 创建脚本引擎（`创建脚本.cs`）
  - [x] 单例，管理节点图
  - [x] `创建工作脚本(进程名, 窗口标题)` — 初始化会话
  - [x] `追加节点(控制节点)` — 返回索引
  - [x] `移除节点(索引)` — 自动修正其他节点跳转引用
  - [x] `修改节点(索引, 参数字典)` — 反射更新字段（含基类字段）
  - [x] `获取节点列表()` / `获取节点(索引)`
  - [x] `保存脚本(名称, 备注)` — 序列化 + 保存 + 注册
  - [x] `创建节点实例(节点类型名)` — 反射工厂
  - [x] 编译验证

- [x] Task 3: 节点创建规则表
  - [x] 在 `全自动MCP工具.cs` 中实现 `get_node_creation_rules`
  - [x] 涵盖所有节点类型，按分类组织
  - [x] 鼠标操作优先级、等待策略、循环构建、命名要求
  - [x] 编译验证

- [x] Task 4: 全自动专用 MCP 工具集（`全自动MCP工具.cs`）
  - [x] 独立文件，与 `通用MCP工具.cs` 分离
  - [x] 注册 13 个工具ID：
    - 节点查询：list_node_types, get_node_creation_rules
    - 脚本编辑：create_working_script, add_node, remove_node, modify_node, execute_node
    - 通用：save_script
    - 脚本管理：list_all_scripts, classify_script, edit_category_rule, create_category
  - [x] 各工具调 `创建脚本` 引擎完成实际操作
  - [x] add_node 反射创建 + 设基类字段（名字、备注、等待、条件）
  - [x] execute_node 构造临时执行环境 → 调节点动作()
  - [x] remove_node 移除 + 修跳转；modify_node 反射更新
  - [x] save_script 序列化 + 保存 + 注册 + 刷新工具列表
  - [x] `list_node_types` 反射列出所有节点及其字段
  - [x] 脚本管理工具（list_all_scripts, classify_script, edit_category_rule, create_category）
  - [x] 编译验证

- [ ] ~~Task 5: 获取交互基类智能体模式~~ — 已跳过（降级路径，按用户要求不实现）

- [ ] ~~Task 6: 多模态消息支持~~ — 已跳过（降级路径，按用户要求不实现）

- [x] Task 7: 总指挥 System Prompt
  - [x] `获取全自动SystemPrompt()` 方法
  - [x] 工作流程引导：list_all_scripts → web_search → get_node_creation_rules → create/add → execute → modify → save
  - [x] 循环检测引导
  - [x] 加权质量评估体系（正确性 > 稳定性 > 可读性 = 效率 = 名字 > 备注）
  - [x] 命名要求 + 安全约束
  - [x] 编译验证

- [x] Task 8: AIChatPage 模式切换 UI
  - [x] ToggleButton（自定义样式：🤖半自动灰 ↔ ⚡全自动绿）
  - [x] 切换时更新 `对话.是否全自动模式`
  - [x] 全自动发送：工具 = 全部分类脚本 + 全自动专用 + 通用；规则 = 全局 + 对话 + 总指挥 System Prompt
  - [x] 半自动：恢复分类筛选 + 移除全自动工具
  - [x] 全自动模式下使用独立 AI 配置（`解析AI配置(对话)`）
  - [x] 全自动模式提示文字
  - [x] 对话设置面板新增全自动 AI 配置编辑区域
  - [x] 编译验证

- [ ] ~~Task 9: AIChatControl 增强~~ — 已跳过（按用户要求不实现）

- [x] Task 10: 结构化日志
  - [x] `全自动日志.cs`，JSON → `logs/auto_mode/`，攻略 → `logs/strategies/`

- [x] Task 11: 集成编译验证
  - [x] `dotnet build` 两项目通过
  - [x] 半自动不含全自动工具 / 全自动含全部脚本+全自动工具
  - [x] 全自动模式下使用独立 AI 配置（已配置时）/ 回退全局配置（未配置时）

# Task Dependencies
- Task 2 依赖 Task 1
- Task 3 独立，可并行于 Task 2
- Task 4 依赖 Task 1, 2
- Task 7 独立
- Task 8 依赖 Task 1, 4, 7
- Task 10 独立
- Task 11 依赖 Task 1-10
