# AI 自学习系统 Spec

## Why
当前 AI 使用 MCP 工具时，每次对话都是从零开始，无法积累使用经验。需要在 AI 使用过程中自动收集经验、调整行为，并将经验与 MCP 工具所在的分集绑定，整个过程对用户完全隐藏。

## What Changes
- 新建 `AI使用经验管理器.cs`：管理经验文件和统计文件的读写、清空、导出
- 新建两个持久化文件：`ai_experience.json`（规则化经验）、`ai_tool_statistics.json`（原始调用记录）
- `MCPToolDefinition` 新增 3 个只读状态属性
- `MCP工具管理器.cs`：加载工具时填充经验状态属性；执行工具时写入原始调用记录
- `AI配置管理器.cs`：构建消息时注入规则化经验；计划格式懒校验
- `AIChatPage.xaml.cs`：跟踪新消息计数，暴露状态
- `MainWindowViewModel.cs`：离开 AI 页面时触发后台经验总结
- `AIPersistenceData` 新增评审 AI 独立配置字段
- 新增计划评审能力（Phase A：上下文学习）
- 新增微调数据导出 + 自动微调评审 AI（Phase B：TODO 注释清晰，用户可覆盖逻辑）

## Impact
- Affected specs: 无（全新功能）
- Affected code: `AI使用经验管理器.cs`（新）、`MCP工具管理器.cs`、`AI配置管理器.cs`、`AI配置模型.cs`、`AIChatPage.xaml.cs`、`MainWindowViewModel.cs`

---

## ADDED Requirements

### Requirement: 工具使用经验持久化
系统 SHALL 在 `%AppData%/淼喵妙脚本DIY/ai_experience.json` 中以每工具为粒度存储规则化使用经验。

#### Scenario: AI 总结后写入经验
- **WHEN** AI 完成经验总结并产出了某工具的新经验规则
- **THEN** 该经验以工具ID为键写入 `ai_experience.json`，附带更新时间戳

#### Scenario: 无新经验时跳过
- **WHEN** AI 判断本轮对话没有值得记录的新经验
- **THEN** 不修改 `ai_experience.json`

---

### Requirement: 经验注入系统消息
系统 SHALL 在构建 AI 消息列表时，将当前对话所涉及工具的使用经验作为独立 system 消息注入，标记为内部参考资料并指示 AI 不向用户提及来源。

#### Scenario: 工具有经验时注入
- **WHEN** 对话使用的工具列表中有工具在 `ai_experience.json` 中存在经验
- **THEN** 在 messages/system prompt 中插入 `【工具使用经验】` 消息，包含各工具的经验规则

#### Scenario: 无经验时跳过
- **WHEN** 对话使用的工具均无使用经验
- **THEN** 不注入经验消息，正常构建消息列表

---

### Requirement: 原始工具调用记录
系统 SHALL 在每次工具调用时，将以下原始字段追加写入 `%AppData%/淼喵妙脚本DIY/ai_tool_statistics.json`：
- 工具ID、调用时间、输入参数、是否成功、耗时ms、输出摘要
- 对话ID、对话轮次
- 调用时计划树、调用后树修改

#### Scenario: 成功调用记录
- **WHEN** 工具调用成功完成
- **THEN** 写入完整记录，`是否成功=true`，无出错信息

#### Scenario: 失败调用记录
- **WHEN** 工具调用失败或抛异常
- **THEN** 写入记录，`是否成功=false`，`输出摘要` 包含错误信息

---

### Requirement: 被动触发经验总结
系统 SHALL 在用户离开 AI 页面且该次访问期间产生了新消息时，在后台触发经验总结流程。总结使用系统预设 prompt，AI 自行判断是否有新经验，输出 `@@工具ID@@ 经验内容` 格式。

#### Scenario: 离开 AI 页面触发总结
- **WHEN** IsAIPage 从 true 变为 false，且对话有新消息
- **THEN** 后台调用总结 AI，解析 `@@工具ID@@ 经验内容` 并写入 `ai_experience.json`

#### Scenario: 离开 AI 页面但无新消息
- **WHEN** IsAIPage 从 true 变为 false，但对话无新增消息
- **THEN** 不触发总结

#### Scenario: AI 判断无新经验
- **WHEN** 总结 AI 回复"无新经验"
- **THEN** 不修改 `ai_experience.json`

---

### Requirement: 计划格式懒校验
系统 SHALL 仅在 AI 尝试调用工具时校验计划格式：若未提交 `[计划]...[/计划]` 或调用了计划外的工具，返回错误信息告知 AI。

#### Scenario: 无工具调用时不校验
- **WHEN** AI 回复中不含任何工具调用（tool_call）
- **THEN** 不校验计划格式，正常完成

#### Scenario: 有工具调用但无计划
- **WHEN** AI 回复中包含工具调用，但未包含 `[计划]...[/计划]`
- **THEN** 返回错误："未检测到计划。请使用 [计划]...[/计划] 格式提交执行计划后再开始工具调用"

#### Scenario: 工具不在计划内
- **WHEN** AI 调用的工具不属于当前提交的计划树中任何叶子节点
- **THEN** 返回错误："工具 xxx 不在当前计划中。请更新计划后再重试"

---

### Requirement: 计划解析
系统 SHALL 将 AI 提交的 `[计划]...[/计划]` 文本解析为结构化计划树，格式为：
```
[计划]
需求=根需求描述
步骤=叶子需求|工具ID
步骤=内部需求|*|子需求1|子工具1|子需求2|子工具2|...
[/计划]
```
`*` 表示该步骤需进一步分解，后续为子步骤的"需求|工具ID"对。

#### Scenario: 解析有效计划
- **WHEN** AI 提交格式正确的 `[计划]...[/计划]`
- **THEN** 解析为 `PlanNode` 对象树，叶子节点绑定工具ID

---

### Requirement: MCPToolDefinition 经验状态属性
`MCPToolDefinition` SHALL 新增 3 个只读属性，在加载工具列表时自动填充。

#### Scenario: 工具加载时填充状态
- **WHEN** `加载工具列表()` 被调用
- **THEN** 每个 `MCPToolDefinition` 的 `已有使用经验`、`经验更新时间`、`已有统计数据` 从对应 JSON 文件中读取并填充

---

### Requirement: 公开清空方法
`AI使用经验管理器` SHALL 提供以下公开静态方法用于实验：
- `清空工具经验(工具ID)`：清空指定工具的经验规则和调用记录
- `清空所有经验()`：清空所有工具的规则化经验和原始调用记录
- `清空工具统计(工具ID)`：仅清空指定工具的原始调用记录，保留经验规则
- `导出微调数据()`：将原始调用记录导出为 JSONL 格式，供后续微调使用

---

### Requirement: 评审 AI 独立配置
系统 SHALL 在 `AIPersistenceData` 中新增 `评审AI配置` 字段，类型为 `AIConfigData`，独立于对话和全局 AI 配置。填了则启用计划评审，未填则跳过。

#### Scenario: 配置了评审 AI
- **WHEN** `评审AI配置` 已填写（地址/模型等）
- **THEN** DeepSeek 提交计划后、执行工具前，调用评审 AI 评估计划可行性

#### Scenario: 未配置评审 AI
- **WHEN** `评审AI配置` 为空或未填写完整
- **THEN** 跳过计划评审步骤，直接执行

---

### Requirement: 计划评审 — 上下文学习（Phase A）
当评审 AI 已配置时，系统 SHALL 从 `ai_tool_statistics.json` 检索相似历史案例，构造 few-shot prompt，由评审 AI 输出评估结果和建议。

#### Scenario: 有相似历史案例
- **WHEN** 评审 AI 已配置且存在相似历史调用记录
- **THEN** 检索最多 5 条相似案例，注入评审 prompt，评审 AI 返回可行性评估和修改建议

#### Scenario: 无相似历史案例
- **WHEN** 评审 AI 已配置但无相似历史记录
- **THEN** 仅注入当前计划，评审 AI 基于通用知识评估

---

### Requirement: 导出微调数据（Phase B 前置）
系统 SHALL 提供 `导出微调数据()` 方法，将 `ai_tool_statistics.json` 中的原始调用记录导出为 JSONL 格式。

#### Scenario: 导出为 JSONL
- **WHEN** 调用 `导出微调数据()`
- **THEN** 按对话分组构造 instruction/input/output 三元组，输出合法 JSONL 文件

---

### Requirement: 自动微调评审 AI（Phase B）
系统 SHALL 自动维护评审 AI 的微调版本，无需人工参与。核心方法 `执行微调()` 包含完整的自动流程：导出数据 → 微调 → 更新模型配置。当前提供简单实现（基于经验构建增强 system prompt 创建 Ollama 模型），逻辑集中在代码注释清晰的独立方法中，用户后续可在代码编辑器中直接覆盖为 LoRA 微调等进阶实现。

数据层面：`AIPersistenceData` 新增 `微调后评审模型名` 字段，记录当前使用的微调模型名，与 `评审AI配置.Ollama模型`（基模）区分。

#### Scenario: 自动触发微调
- **WHEN** 工具调用记录新增达到阈值（默认 20 条），或用户离开 AI 页面触发了新经验写入
- **THEN** 后台自动调用 `执行微调()`，完成后更新 `微调后评审模型名`

#### Scenario: 微调逻辑可覆盖
- **WHEN** 用户修改 `执行微调()` 方法中的 TODO 注释标记区域的逻辑
- **THEN** 下次自动触发时使用新逻辑，无需修改其他任何调用方代码

#### Scenario: 无新数据时跳过
- **WHEN** 无新增工具调用记录
- **THEN** 不触发微调
