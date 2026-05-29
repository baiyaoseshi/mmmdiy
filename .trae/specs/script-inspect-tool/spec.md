# 脚本展开查看 MCP 工具 Spec

## Why
AI 创建脚本时需要学习参考已有的脚本实现。当前 `list_all_scripts` 只列出已加载脚本列表（名称和备注），AI 无法看到脚本内部所有节点及其字段数据，导致模仿学习困难。需要提供工具让 AI 在**已加载的 MCP 工具列表范围内**展开查看任意脚本的完整节点结构，以及按关键词过滤搜索。

## 核心约束
**所有搜索与匹配操作均限制在 `通用MCP工具.执行内置工具` 方法接收的 `List<MCPToolDefinition> tools` 参数（即已加载的 MCP 工具列表）范围内，不做文件系统遍历。** `.script` 文件内容仅通过 `MCPToolDefinition.脚本路径` 按需读取。

## What Changes
- 新增 `expand_script` 通用 MCP 工具：在已加载工具列表中按名称匹配，展开返回节点结构
- 新增 `search_scripts` 通用 MCP 工具：按关键词过滤已加载工具列表，返回匹配摘要
- 在 `构建提示词()` / `构建消息列表()` 的隐藏提示词中注册这两个新工具的使用指南

## Impact
- Affected specs: 无（新功能）
- Affected code:
  - `通用MCP工具.cs` — 添加 `expand_script` 和 `search_scripts` 工具实现
  - `AI配置管理器.cs` — 更新隐藏提示词中的【通用MCP工具使用指南】
  - 不影响现有工具和流程

## ADDED Requirements

### Requirement: expand_script 工具
系统 SHALL 提供一个 `expand_script` MCP 工具，在**已加载的 `List<MCPToolDefinition>`** 中按名称匹配脚本，通过 `脚本路径` 读取 `.script` 文件，返回脚本头部信息和所有节点的完整字段数据。

#### 工具定义
- **工具ID**: `expand_script`
- **参数**:
  - `脚本标识` (必填, string): 脚本名称（匹配 `MCPToolDefinition.名称`）或完整脚本路径（匹配 `MCPToolDefinition.脚本路径`），支持模糊匹配
- **依赖数据**: 读取 `userdata.json` 中所有分类下的所有脚本路径，**不限当前对话加载的工具范围**
- **返回值**: 格式化文本，包含脚本头部信息（进程名、窗口标题、备注）和所有节点的字段数据。图片/二进制数据自动省略。

#### 返回格式
```
脚本名: 每日签到
目标进程: chrome.exe
目标窗口: Google Chrome
备注: 自动完成每日签到

--- 节点列表 ---

节点 0: [空节点]
  节点名字: 开始
  节点备注: 这是开始节点
  成功后跳转: 1
  失败后跳转: -1
  成功后等待: 1000
  失败后等待: 1000

节点 1: [单击按键]
  按键: {Key:Enter}
  节点名字: 按回车
  ...

--- 共 N 个节点 ---
```

#### 图片数据处理
- 字段名匹配 `模板数据`、`模板图像` 等已知图片字段 → 替换为 `[图片数据已省略]`
- 疑似 base64 的长字段 → 替换为 `[数据过长已省略，原长度: N 字符]`
- 其他过长文本 → 截断显示前 200 字符并标注原长度

#### Scenario: 通过脚本名精确匹配并展开
- **GIVEN** 已加载的工具列表中含 `名称`="每日签到" 的脚本
- **WHEN** AI 调用 `expand_script("每日签到")`
- **THEN** 通过 `脚本路径` 读取文件，返回该脚本的完整节点结构

#### Scenario: 模糊匹配
- **GIVEN** 已加载列表中有"每日签到"、"游戏签到"，用户输入"签到"
- **WHEN** AI 调用 `expand_script("签到")`
- **THEN** 返回第一个 Contains 匹配脚本的完整结构，在头部注明 "模糊匹配: 签到 → 每日签到"

#### Scenario: 脚本未在已加载列表中
- **GIVEN** 传入的标识在已加载列表中找不到匹配
- **WHEN** AI 调用 `expand_script("不存在的脚本")`
- **THEN** 返回 "当前对话未加载匹配的脚本: 不存在的脚本。提示：该脚本可能不在当前对话的工具库分类中。"

### Requirement: search_scripts 工具
系统 SHALL 提供一个 `search_scripts` MCP 工具，在**已加载的 `List<MCPToolDefinition>`** 中按关键词过滤，返回匹配脚本的摘要列表。

#### 工具定义
- **工具ID**: `search_scripts`
- **参数**:
  - `关键词` (必填, string): 搜索关键词
  - `搜索范围` (可选, string): "名称备注"（默认，匹配脚本文件名 + 备注）或 "完整内容"（额外读取 `.script` 文件全文匹配）
  - `最大结果数` (可选, int): 默认 5
- **依赖数据**: 读取 `userdata.json` 中所有分类下的所有脚本路径，**不限当前对话加载的工具范围**
- **返回值**: 匹配的脚本列表，每行 `- 脚本名 (分类路径): 备注摘要`

#### Scenario: 搜索名称和备注
- **GIVEN** 已加载列表中有多个签到相关脚本
- **WHEN** AI 调用 `search_scripts("签到")`
- **THEN** 返回所有名称或描述含"签到"的脚本列表

#### Scenario: 搜索完整内容
- **GIVEN** AI 想找使用了"单击按键"节点的脚本
- **WHEN** AI 调用 `search_scripts("单击按键", "完整内容")`
- **THEN** 逐一读取已加载列表中每个脚本的 `.script` 文件全文，返回所有包含"单击按键"的脚本列表

#### Scenario: 超过最大结果数
- **GIVEN** 匹配结果超过5个
- **WHEN** AI 调用 `search_scripts("签到", "名称备注", 3)`
- **THEN** 返回前3条，底部标注 "... 还有 N 条匹配结果未显示，请缩小搜索范围"

#### Scenario: 无匹配结果
- **GIVEN** 已加载列表中无匹配
- **WHEN** AI 调用 `search_scripts("xyznonexistent")`
- **THEN** 返回 "在当前对话已加载的脚本中未找到匹配结果"

### Requirement: 隐藏提示词同步
系统 SHALL 在 `AI配置管理器.cs` 的【通用MCP工具使用指南】中注册 `expand_script` 和 `search_scripts` 两个工具，说明用法和推荐工作流。

#### Scenario: AI 知晓推荐工作流
- **WHEN** AI 收到用户要求"参考已有脚本创建一个类似的"
- **THEN** AI 能先调用 `search_scripts` 在已加载工具列表中过滤相关脚本，再调用 `expand_script` 查看其节点结构，最后用全自动工具模仿创建
