# AI 配置列表化 Spec

## Why

当前全局 AI 配置只有**一套**，所有对话共享同一个 AI 提供者和模型。用户无法为不同对话选择不同的模型（如一个对话用 DeepSeek，另一个用本地 Qwen），也无法在同一个对话中为文本对话和多模态（视觉）任务指定不同的模型。需要将全局单一配置改为**多配置列表**，让对话可自由选择。

## What Changes

### 数据模型
- **新增** `AINamedConfig` 类：包含 `Id`、`名称`、`AIConfigData 配置`，作为配置列表的条目
- **修改** `AIPersistenceData`：
  - **新增** `AI配置列表`（`List<AINamedConfig>`）替代单一 `全局AI配置`
  - **保留** `全局AI配置` 用于向后兼容迁移，迁移后不再使用
  - **修改** `视觉AI配置` 改为 `string 视觉AI配置Id`（引用列表中的配置）
  - **修改** `评审AI配置` 改为 `string 评审AI配置Id`（引用列表中的配置）
- **修改** `AIConversation`：
  - **新增** `文本AI配置Id` — 对话使用的文本模型配置引用
  - **新增** `多模态AI配置Id` — 对话使用的多模态（视觉）模型配置引用
  - **保留** `最大输出Token`、`温度` 作为对话级覆盖参数

### UI
- **修改** `GlobalAISettings`：将单一 AI 提供者配置区替换为**配置列表编辑器**（增/删/改名/编辑每个配置的提供者参数）
- **修改** `AIConversationSettings`：新增三个下拉框，让用户从全局配置列表中分别选择**文本模型**、**多模态模型**、**本地可调参模型**
- **修改** `AIChatPage.解析AI配置()`：根据对话选中的配置 ID 从列表中查找对应的 `AIConfigData`

### 向后兼容
- 首次加载时，将旧版 `全局AI配置` 自动迁移为配置列表的第一个条目（名称"默认配置"）
- 旧对话无 `文本AI配置Id` 时，默认使用列表第一个配置

## Impact

- Affected specs: AI系统概述、AI流式渲染节流设计
- Affected code:
  - `AI配置模型.cs` — 新增 `AINamedConfig` 类，修改 `AIConversation`、`AIPersistenceData`
  - `AI配置管理器.cs` — 新增配置列表 CRUD 方法，修改持久化逻辑，添加迁移
  - `GlobalAISettings.xaml + .cs` — 重构为配置列表编辑器
  - `AIConversationSettings.xaml + .cs` — 新增模型选择下拉框
  - `AIChatPage.xaml.cs` — 修改 `解析AI配置()` 方法

## ADDED Requirements

### Requirement: 全局 AI 配置列表管理
系统 SHALL 在持久化数据中维护一个 AI 配置列表，每项包含唯一 ID、用户自定义名称和完整的提供者连接参数（提供者类型、Ollama 地址/模型、远程 API 地址/密钥/模型、Google 搜索密钥等）。

#### Scenario: 添加新配置
- **WHEN** 用户在全局设置中点击"添加配置"
- **THEN** 系统创建一个带默认名称的配置条目，用户可编辑其名称和所有连接参数

#### Scenario: 删除配置
- **WHEN** 用户删除一个被其他对话引用的配置
- **THEN** 系统提示该配置被引用，确认后删除并将引用该配置的对话回退到列表第一个配置

#### Scenario: 旧数据迁移
- **WHEN** 系统首次加载发现持久化数据中有旧版 `全局AI配置`（有值）但 `AI配置列表` 为空
- **THEN** 自动将旧的 `全局AI配置` 封装为一条名为"默认配置"的条目加入 `AI配置列表`，并清空旧的 `全局AI配置` 字段

### Requirement: 对话级模型选择
系统 SHALL 允许每个对话从全局配置列表中分别选择文本模型、多模态模型和本地可调参模型，默认为用户上次使用的选择。

#### Scenario: 新建对话使用默认选择
- **WHEN** 用户创建新对话且之前已有使用过的模型选择
- **THEN** 新对话的文本/多模态/本地模型默认选中上次使用的配置

#### Scenario: 对话切换文本模型
- **WHEN** 用户在对话设置中从下拉框选择不同的文本模型配置
- **THEN** 该对话后续发送消息时使用新选的 AI 配置进行推理

#### Scenario: ask_vision_ai 使用多模态模型
- **WHEN** AI 调用 `ask_vision_ai` 通用 MCP 工具
- **THEN** 系统使用当前对话选中的多模态模型配置（若未设置则回退到文本模型配置）

#### Scenario: 本地可调参模型使用
- **WHEN** 用户发送消息时，系统根据对话选中的本地可调参模型配置进行推理
- **THEN** 若本地可调参模型有独立选择的配置（非"使用文本模型"），则使用该配置进行推理；否则使用文本模型配置

## MODIFIED Requirements

### Requirement: AI 配置持久化（修改）
`AIPersistenceData` 不再使用单一的 `全局AI配置` 字段作为默认配置源，改为使用 `AI配置列表`（`List<AINamedConfig>`）作为唯一的配置存储，`视觉AI配置` 和 `评审AI配置` 改为字符串引用（`视觉AI配置Id`、`评审AI配置Id`）。

### Requirement: 全局设置面板（修改）
`GlobalAISettings` 不再显示单一的 AI 提供者配置表单，改为显示可增删的配置列表，点击列表项展开编辑该配置的提供者参数（提供者类型、Ollama 地址/模型、远程 API 地址/密钥/模型、最大 Token、温度等）。

### Requirement: 对话设置面板（修改）
`AIConversationSettings` 新增"文本模型"、"多模态模型"、"本地可调参模型"三个下拉选择框，选项来源于全局配置列表。

### Requirement: AI 调用配置解析（修改）
`AIChatPage.解析AI配置()` 不再从全局配置直接复制全部字段，改为根据对话的 `文本AI配置Id`（或多模态/本地配置 ID）从配置列表中查找对应的 `AIConfigData`，再叠加对话级 `最大输出Token` 和 `温度` 覆盖。

## REMOVED Requirements

无。现有的 `全局AI配置` 字段将保留在模型中以支持向后兼容迁移，迁移完成后自动清空。
