# web_search 改用 Google Custom Search API Spec

## Why
当前 `web_search` 使用手写 HttpClient 代码调用 DuckDuckGo Instant Answer API，只返回百科摘要而非真正的网页搜索结果，信息量有限。改用 Google Custom Search API 官方 SDK 可以获得更丰富、更准确的网页搜索结果，同时用第三方库替代手写代码。

## What Changes
- 添加 `Google.Apis.CustomSearchAPI.v1` NuGet 包（Google 官方 SDK）
- 在 `AIConfigData` 中新增 Google API 密钥和搜索引擎 ID (CX) 字段
- 重写 `通用MCP工具.联网搜索()` 方法，用 Google SDK 替换手写 HttpClient + JSON 解析
- 更新 `AI配置管理器.cs` 中两处 web_search 的 AI 提示词描述
- 在 `GlobalAISettings.xaml` 全局设置面板中添加 Google API Key 和 CX 的配置入口
- API Key 使用 DPAPI 加密存储（与现有远程 API 密钥相同机制）

## Impact
- Affected specs: 无（纯替换实现，不改变外部接口）
- Affected code:
  - `通用MCP工具.cs` — `联网搜索()` 方法重写
  - `AI配置模型.cs` — `AIConfigData` 新增 2 个字段
  - `AI配置管理器.cs` — 两处提示词更新
  - `GlobalAISettings.xaml` — 新增配置 UI
  - `GlobalAISettings.xaml.cs` — 新增配置逻辑 + 加密/解密
  - `淼喵妙神奇工具库.csproj` — 新增 NuGet 引用

## ADDED Requirements

### Requirement: Google Custom Search API 集成
系统 SHALL 使用 Google.Apis.CustomSearchAPI.v1 官方 SDK 执行联网搜索，替代当前手写的 DuckDuckGo HTTP 调用。

#### Scenario: 正常搜索
- **WHEN** AI 调用 web_search 工具，传入有效的查询关键词
- **THEN** 系统使用 Google Custom Search API 执行搜索，返回标题 + 摘要 + 链接的结构化结果

#### Scenario: API Key 或 CX 未配置
- **WHEN** AI 调用 web_search 但 Google API Key 或 CX 为空
- **THEN** 返回明确的错误提示「联网搜索未配置，请在全局设置中填写 Google API Key 和搜索引擎 ID(CX)」

#### Scenario: API 调用失败
- **WHEN** Google API 返回错误或网络超时
- **THEN** 返回友好的错误信息，包含失败原因

#### Scenario: 无搜索结果
- **WHEN** Google API 返回空结果
- **THEN** 返回「未找到相关搜索结果，建议尝试其他关键词」

### Requirement: Google API 配置
系统 SHALL 在 `AIConfigData` 中提供 Google API Key 和搜索引擎 ID (CX) 的全局配置存储。

#### Scenario: 加密存储
- **WHEN** 用户保存 API Key
- **THEN** API Key 使用 DPAPI 加密后存储于 `ai_config.json`，与现有远程 API 密钥使用相同加密/解密方法

#### Scenario: 全局设置面板
- **WHEN** 用户打开全局 AI 设置面板（GlobalAISettings）
- **THEN** 面板底部（保存按钮上方）显示 Google API Key (PasswordBox) 和搜索引擎 ID (TextBox) 输入框，含获取链接指引

#### Scenario: 配置持久化
- **WHEN** 用户保存全局设置
- **THEN** Google API Key 和 CX 一同保存到 `ai_config.json` 持久化数据中

### Requirement: AI 提示词更新
系统 SHALL 更新 AI 系统提示中 web_search 工具的描述，反映新的搜索引擎特性。

#### Scenario: 提示词准确性
- **WHEN** 系统向 LLM 注入 web_search 使用指南
- **THEN** 描述应从「DuckDuckGo 引擎，无需 API 密钥」改为「Google 搜索引擎，需在全局设置中配置」，并附搜索结果特点说明

## REMOVED Requirements

### Requirement: DuckDuckGo Instant Answer API 手写调用
**Reason**: 被 Google Custom Search API 官方 SDK 替代
**Migration**: `联网搜索()` 方法中的 HttpClient + JsonDocument.Parse 手动解析代码全部移除，`System.Net.Http` using 若仅在此方法使用也一并移除
