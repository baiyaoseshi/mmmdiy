# 全自动模式 Spec

## Why
半自动模式下 AI 只能调用已有脚本，无法自主生成新脚本。全自动模式让 LLM 作为"总指挥"，通过以下路径自主创建脚本：

- **攻略查询**：总指挥调 `web_search` 搜索操作攻略，提炼为结构化步骤
- **脚本构建**：总指挥调用全自动 MCP 工具（`create_working_script` → `add_node` → `modify_node` → `execute_node` → `save_script`），逐节点构建并验证脚本
- **多模态感知**：全自动模式使用**独立 AI 配置**（如 GPT-4o、llava 等视觉模型），可截图分析当前界面状态，做出更精准的节点构建决策

## What Changes
- 新增 **全自动模式开关**（ToggleButton）
- 新增 **独立 AI 配置**（全自动模式专用多模态模型，与普通对话配置分离）
- 新增 **创建脚本引擎**（`创建脚本.cs`）
- 新增 **全自动专用 MCP 工具集**（`全自动MCP工具.cs`，独立文件）：
  - 节点查询：list_node_types, get_node_creation_rules
  - 脚本编辑：create_working_script, add_node, remove_node, modify_node, execute_node
  - 通用编辑：save_script
  - 脚本管理：list_all_scripts, classify_script, edit_category_rule, create_category
- 新增 **节点创建规则表**：告诉 AI 每种节点适合什么场景、选型优先级（如点击图片 > 固定坐标）、参数建议
- 新增 **加权质量评估体系**（System Prompt 引导）
- **等待机制简化**：所有节点自带 `成功后等待`/`失败后等待`，不单独设等待节点
- **循环检测**：AI 分析节点间关系，识别循环模式并设置正确跳转
- 全自动 MCP 脚本范围：全部已保存脚本，新脚本即时更新
- 结构化日志

## Impact
- Affected code:
  - `AI配置模型.cs` — `是否全自动模式`、`图片列表`、`全局全自动AI配置`（AIPersistenceData）
  - `创建脚本.cs`（新）— 节点图管理
  - `全自动MCP工具.cs`（新）— 全自动专用 MCP（13 个工具）
  - `全自动日志.cs`（新）— 结构化日志
  - `AI配置管理器.cs` — System Prompt（`获取全自动SystemPrompt()`）+ 全局全自动配置读写
  - `AIChatPage.xaml/cs` — 模式切换 ToggleButton + 全自动工具/配置加载
  - `GlobalAISettings.xaml/cs` — 全自动 AI 配置编辑区域
  - `AIChatControl.xaml/cs` — 全自动模式提示文字

## ADDED Requirements

### Requirement: LLM 自主构建脚本
系统 SHALL 让 LLM 通过全自动 MCP 工具完全自主地创建脚本。

#### Scenario: 总指挥工作流
- **WHEN** 用户全自动模式下发任务
- **THEN** 总指挥 system prompt 生效：
  1. `list_all_scripts` 检查已有脚本
  2. `web_search` 搜索操作攻略，提炼结构化步骤
  3. `get_node_creation_rules` 了解节点创建规则和优先级
  4. `create_working_script` 初始化工作脚本
  5. 逐个 `add_node` 构建脚本（设名字、备注、等待时间）
  6. `execute_node` 测试关键节点
  7. `modify_node` 修复问题
  8. `save_script` → `classify_script` 保存分类
- **AND** 安全约束：禁止高危操作；必要时拒绝并说明原因

#### Scenario: 攻略查询
- 总指挥自主调 `web_search` → 提炼结构化步骤 → 可选 `write_log` 缓存

### Requirement: 全自动独立 AI 配置
系统 SHALL 为全自动模式提供独立的全局 AI 配置，与半自动对话的全局配置分离。

#### Scenario: 使用多模态模型
- **WHEN** 用户在全局设置中填写了「全自动 AI 配置」
- **THEN** 全自动模式下使用该配置调用 AI（如 GPT-4o、llava 等视觉模型）
- **AND** 未填写时回退到全局 AI 配置
- **AND** 配置持久化到 `ai_config.json` 的 `全局全自动AI配置` 字段

#### Scenario: UI 编辑
- 全局设置面板（🌐 按钮）中显示「⚡ 全自动模式 AI 配置」区域
- 含提供者选择（Ollama/OAI）、地址、模型、API 密钥字段
- 半自动模式的对话设置面板中不显示此区域（全自动配置是全局共享的）

### Requirement: 节点创建规则表
系统 SHALL 提供规则表告诉 AI 每种节点的适用场景、选型优先级、参数建议。

#### Scenario: get_node_creation_rules
- **WHEN** AI 调用 `get_node_creation_rules`
- **THEN** 返回结构化规则表，按分类组织，含：
  - 鼠标操作优先级：点击图片 > 点击串联图片 > 点击文字 > 鼠标单击
  - 等待策略：操作类 100-500ms、加载类 500-2000ms
  - 各节点适用场景、参数说明、注意事项
  - 循环构建方式、命名要求

### Requirement: 等待机制
每个节点自带 `成功后等待` 和 `失败后等待`（毫秒），不单独设等待节点。

### Requirement: 脚本质量评估（System Prompt 引导）
| 维度 | 权重 | 检查项 |
|------|------|--------|
| **正确性** | ⭐⭐⭐⭐⭐ 最高 | 流程能否完整执行？循环正确？ |
| **稳定性** | ⭐⭐⭐⭐ 较高 | 等待时间合理？失败跳转存在？ |
| **可读性** | ⭐⭐⭐ 中等 | 节点名字清晰？ |
| **效率** | ⭐⭐⭐ 中等 | 等待时间不过度？无冗余？ |
| **名字明确度** | ⭐⭐⭐ 中等 | 名字准确反映动作（动宾结构）？ |
| **备注明确度** | ⭐⭐ 最低 | 备注补充关键上下文？ |

### Requirement: 创建脚本引擎
单例 `创建脚本.cs`，管理节点图：
- `创建工作脚本(进程名, 窗口标题)`
- `追加节点(控制节点)` — 返回索引
- `移除节点(索引)` — 自动修跳转引用
- `修改节点(索引, 参数字典)` — 反射更新字段
- `获取节点列表()` / `获取节点(索引)`
- `保存脚本(名称, 备注)` — 序列化 → 存 .script → 注册用户数据

### Requirement: 全自动专用 MCP 工具集（13 个工具）
`全自动MCP工具.cs` 独立文件，与 `通用MCP工具.cs` 分离：

| 分类 | 工具ID |
|------|--------|
| 节点查询 | list_node_types, get_node_creation_rules |
| 脚本编辑 | create_working_script, add_node, remove_node, modify_node, execute_node |
| 通用编辑 | save_script |
| 脚本管理 | list_all_scripts, classify_script, edit_category_rule, create_category |

### Requirement: 全自动 MCP 脚本范围
- 全自动：全部分类脚本 + 全自动专用工具 + 通用工具
- 半自动：按对话选中的分类筛选 + 通用工具

### Requirement: UI
- 模式切换 ToggleButton（🤖 半自动 灰色 ↔ ⚡ 全自动 绿色）
- 全自动模式下输入框下方显示提示文字
- 对话设置面板底部显示全自动 AI 配置编辑区

### Requirement: 结构化日志
`全自动日志.cs`：JSON → `logs/auto_mode/`，攻略缓存 → `logs/strategies/`
