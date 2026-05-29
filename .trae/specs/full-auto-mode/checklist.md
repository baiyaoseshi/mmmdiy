# Checklist

## 数据模型
- [x] `AIConversation.是否全自动模式` 字段，JSON 序列化正常
- [x] `AIChatMessage.图片列表` 字段
- [x] `AIConversation.全自动AI配置` 字段（独立 AIConfigData，多模态模型用）

## 创建脚本引擎
- [x] `创建工作脚本 → 追加节点 → 移除节点(修跳转) → 修改节点(含基类字段) → 保存脚本`
- [x] 脚本头部含进程名、窗口标题、备注
- [x] `创建节点实例(节点类型名)` 反射工厂

## 节点创建规则表
- [x] 涵盖所有节点类型，按分类组织
- [x] 鼠标操作优先级 点击图片 > 点击串联图片 > 点击文字 > 鼠标单击
- [x] 等待策略说明（操作类 100-500ms、加载类 500-2000ms）
- [x] 循环构建方式、命名要求

## 全自动 MCP 工具集
- [x] `list_node_types` 反射返回所有节点+字段（含基类通用字段）
- [x] `get_node_creation_rules` 返回结构化规则
- [x] `create_working_script` 初始化脚本
- [x] `add_node` 反射创建+设基类（名字、备注、等待时间）+ 其他字段
- [x] `remove_node` 移除 + 修跳转
- [x] `modify_node` 反射更新字段
- [x] `execute_node` 构造临时环境 → 执行 → 返回结果
- [x] `save_script` 序列化 + 保存 + 注册
- [x] `list_all_scripts` 列出所有脚本及分类
- [x] `classify_script` 将脚本移动到目标分类
- [x] `edit_category_rule` 编辑分类 AI 规则
- [x] `create_category` 创建新分类（可选父分类）
- [x] 与通用 MCP 分离，仅在 `获取全自动工具列表()` 返回
- [x] `MCP工具管理器.执行工具()` 已添加全自动工具分发

## UI
- [x] ToggleButton 切换全自动/半自动（灰🤖半自动 ↔ 绿⚡全自动）
- [x] 切换时更新对话 + 工具范围 + 规则
- [x] 全自动模式下使用独立 AI 配置（`解析AI配置(对话)`）
- [x] 全自动模式提示文字
- [x] 对话设置面板「⚡ 全自动模式 AI 配置」区域（提供者/地址/模型/密钥）

## System Prompt
- [x] `获取全自动SystemPrompt()` 含完整工作流引导
- [x] 循环检测引导
- [x] 加权质量评估体系
- [x] 命名要求 + 安全约束
- [x] 攻略搜索自主性引导

## 结构化日志
- [x] JSON → `logs/auto_mode/`
- [x] 攻略缓存 → `logs/strategies/`

## 编译
- [x] `dotnet build` 工具库通过
- [x] `dotnet build` 用户界面通过

## 跳过项（按用户要求）
- [ ] 获取交互基类智能体模式（降级路径）
- [ ] 多模态消息支持（降级路径用 image_url/OCR）
- [ ] AIChatControl 增强（图片缩略图、操作步骤样式、脚本保存通知卡片）
- [x] OpenClaw 集成 — 已彻底移除（经调研后确认不适合以脚本精灵为主的集成方式）
