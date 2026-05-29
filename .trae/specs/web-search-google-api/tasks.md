# Tasks

- [ ] Task 1: 添加 NuGet 包引用 — 在 `淼喵妙神奇工具库.csproj` 中添加 `Google.Apis.CustomSearchAPI.v1` 包
- [ ] Task 2: 扩展 `AIConfigData` 模型 — 在 `AI配置模型.cs` 中添加 `加密GoogleAPI密钥` 和 `Google搜索引擎ID` 两个字段
- [ ] Task 3: 重写 `联网搜索()` 方法 — 在 `通用MCP工具.cs` 中用 `Google.Apis.CustomSearchAPI.v1` SDK 替换手写 HttpClient 代码（参数不变：查询关键词、最大结果数）
- [ ] Task 4: 更新 AI 提示词 — 在 `AI配置管理器.cs` 中修改两处 web_search 的描述文案（构建消息列表 和 构建提示词），反映 Google 引擎和配置要求
- [ ] Task 5: 扩展 `GlobalAISettings` UI — 在 XAML 中添加 Google API Key (PasswordBox) + 搜索引擎 ID (TextBox) 输入框，在 .cs 中实现加载/保存/加密解密逻辑
- [ ] Task 6: 编译验证 — 确保工具库和用户界面两个项目均编译通过

# Task Dependencies
- Task 2、Task 3、Task 4 互相独立，可并行
- Task 5 依赖 Task 2（需要模型字段）
- Task 6 依赖所有前序任务
