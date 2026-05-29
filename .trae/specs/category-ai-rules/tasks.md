# Tasks

- [ ] Task 1: 数据模型层 — 为分类添加 AI规则 字段
  - [ ] 在 `UserData.cs` 的 `TaskCategoryData` 类中添加 `public string AI规则 { get; set; } = "";`
  - [ ] 在 `TaskCategoryViewModel.cs` 中添加 `public string AI规则 { get; set; } = "";` 属性
  - [ ] 在 `MCP工具管理器.cs` 的 `对象型TaskCategory` 私有类中添加 `public string AI规则 { get; set; }`

- [ ] Task 2: MCP 工具管理器 — 加载工具时附带多级分类规则
  - [ ] 在 `MCPToolDefinition` 类中添加 `public string 分类AI规则 { get; set; } = "";` 和 `public List<string> 祖先AI规则列表 { get; set; } = new List<string>();`
  - [ ] 修改 `递归收集分类脚本()`：增加 `List<string> 祖先规则` 参数（从根到当前分类的父级），递归时追加当前分类的 AI 规则后传给子分类；创建 MCPToolDefinition 时正确填充两个规则字段
  - [ ] 修改 `加载工具列表()`：调用 `递归收集分类脚本` 时传入空 `祖先规则` 列表

- [ ] Task 3: AI配置管理器 — 注入分类规则到系统提示词（区分当前规则与多级祖先参考规则）
  - [ ] 在 `AI配置管理器.cs` 中添加辅助方法 `收集分类规则提示词(List<MCPToolDefinition>)`：从工具列表按分类去重收集非空规则，生成格式化的提示词文本：
    - 当前分类规则标注"必须遵守"
    - 祖先规则列表标注"仅供参考，请提取关键要点理解"，以 `- 分类名: 规则` 列表呈现
  - [ ] 修改 `调用本地AI流式带工具()`：在工具描述后面调用 `收集分类规则提示词()` 追加注入
  - [ ] 修改 `调用远程AI流式带工具()`：在消息列表中插入一条额外的 system 消息包含分类规则

- [ ] Task 4: 主界面 UI — 分类右键菜单添加"编辑AI规则"
  - [ ] 在 `MainPageControl.xaml.cs` 的 `CategoryListBox_ContextMenuOpening` 方法中追加"编辑AI规则"菜单项（与现有"添加子分类""解散分类"等并列）
  - [ ] 在 `MainWindowViewModel.cs` 中添加 `EditCategoryAIRule(TaskCategoryViewModel)` 方法：通过通知工具.输入弹窗编辑规则，标记 `_needsSave = true`
  - [ ] 在 `MainWindowViewModel.cs` 的加载/保存分类方法中递归同步 `AI规则` 字段

- [ ] Task 5: 对话设置面板 — 显示分类AI规则摘要
  - [ ] 在 `AIConversationSettings.xaml` 的分类勾选列表下方添加只读 `TextBlock` 规则摘要区域，含标题"分类AI规则摘要"
  - [ ] 在 `AIConversationSettings.xaml.cs` 中：修改 `加载()` 方法接收分类 AI 规则数据（`Dictionary<string, string>`，key为分类完整路径）
  - [ ] 在 `AIChatPage.xaml.cs` 中：调用 `SettingsPanel.加载()` 时传入分类 AI 规则数据
  - [ ] 添加刷新规则摘要的方法；无规则时隐藏摘要区域；勾选变化时实时刷新

- [ ] Task 6: 编译验证
  - [ ] 编译工具库项目确认无错误
  - [ ] 编译用户界面项目确认无错误

# Task Dependencies
- Task 2 依赖 Task 1（需要 `对象型TaskCategory.AI规则` 字段）
- Task 3 依赖 Task 2（需要 `MCPToolDefinition` 中的 `分类AI规则` 和 `祖先AI规则列表` 字段）
- Task 4 依赖 Task 1（需要 ViewModel 中的 `AI规则` 属性）
- Task 5 依赖 Task 1（需要分类 AI 规则数据）
- Task 6 依赖所有前序任务
- Task 1 / Task 4 / Task 5 可部分并行；Task 2 → Task 3 需顺序执行
