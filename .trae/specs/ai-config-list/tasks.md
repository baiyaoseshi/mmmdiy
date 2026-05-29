# Tasks

- [x] Task 1: 修改数据模型（AI配置模型.cs）
  - [x] 新增 `AINamedConfig` 类：包含 `string Id`、`string 名称`、`AIConfigData 配置`
  - [x] 修改 `AIConversation`：新增 `string 文本AI配置Id`、`string 多模态AI配置Id`，移除或标记废弃 `AIConfigData AI配置` 字段（保持向后兼容）
  - [x] 修改 `AIPersistenceData`：新增 `List<AINamedConfig> AI配置列表`，新增 `string 上次文本配置Id`、`string 上次多模态配置Id`（用于新对话默认值），将 `视觉AI配置` 改为 `string 视觉AI配置Id`，将 `评审AI配置` 改为 `string 评审AI配置Id`，保留旧 `全局AI配置` 字段用于迁移

- [x] Task 2: 修改 AI 配置管理器（AI配置管理器.cs）
  - [x] 添加数据迁移逻辑：`加载数据()` 后检测旧 `全局AI配置` 有值但 `AI配置列表` 为空时，自动迁移为列表第一个条目
  - [x] 新增配置列表 CRUD 方法：`获取配置列表()`、`添加配置(AINamedConfig)`、`删除配置(string id)`、`更新配置(AINamedConfig)`
  - [x] 新增 `根据Id获取配置(string id)` 辅助方法（未找到时返回列表第一个配置）
  - [x] 修改 `获取全局配置()` 返回列表第一个配置（向后兼容）
  - [x] 修改 `获取视觉AI配置()` 和 `获取评审AI配置()` 通过 ID 引用从列表查找
  - [x] 新增 `更新上次使用的配置Id(string 文本Id, string 多模态Id)` 方法
  - [x] 确保所有 `await` 使用 `.ConfigureAwait(false)`

- [x] Task 3: 重构全局设置面板（GlobalAISettings.xaml + .cs）
  - [x] 将原有的单一 AI 提供者配置区域替换为可滚动的配置列表（ListBox/ItemsControl），每项显示名称和提供者类型摘要
  - [x] 添加"添加配置"按钮（新增一条默认配置）和每项的"删除"按钮
  - [x] 点击列表项展开编辑面板（或弹出编辑区域），编辑该配置的：名称、提供者类型、Ollama 地址/模型、远程 API 地址/密钥/模型、Google 搜索密钥、最大 Token、温度
  - [x] 保留视觉 AI 配置区域，但改为从列表中选择多模态模型
  - [x] 保留评审 AI 配置区域（如之前有UI），同样改为从列表中选择
  - [x] 保存时将整个配置列表 + 视觉/评审引用写入管理器

- [x] Task 4: 修改对话设置面板（AIConversationSettings.xaml + .cs）
  - [x] 新增"文本模型"下拉框（ComboBox），选项来源于全局配置列表（显示配置名称）
  - [x] 新增"多模态模型"下拉框，选项同上，额外添加"使用文本模型"选项
  - [x] 新增"本地可调参模型"下拉框，选项同上，额外添加"使用文本模型"选项
  - [x] 加载时若对话未设置配置 ID（null/空），默认选中上次使用的配置（从 `AIPersistenceData.上次文本配置Id` / `上次多模态配置Id` 获取），若仍为空则选中列表第一个
  - [x] 保存时将选中的配置 ID 写回 `AIConversation`，并更新全局"上次使用"记录

- [x] Task 5: 修改 AIChatPage 配置解析（AIChatPage.xaml.cs）
  - [x] 修改 `解析AI配置()` 方法：根据对话的 `文本AI配置Id` 从配置列表查找 `AIConfigData` 作为基础配置，再叠加对话级 `最大输出Token` 和 `温度` 覆盖
  - [x] 确保 `ask_vision_ai` 工具调用处使用对话的 `多模态AI配置Id`（若未设置则回退到 `文本AI配置Id`）
  - [x] 确保现有代码中其他引用 `获取全局配置()` 的地方仍能正常工作（方法返回列表第一个配置）

- [x] Task 6: 编译验证与手动测试
  - [x] 编译两个项目确保无错误
  - [x] 运行应用验证：旧数据自动迁移、新配置添加/删除、对话选择不同模型发送消息

# Task Dependencies

- Task 2 依赖 Task 1（数据模型）
- Task 3 和 Task 4 可并行，均依赖 Task 2
- Task 5 依赖 Task 2、3、4
- Task 6 依赖 Task 5
