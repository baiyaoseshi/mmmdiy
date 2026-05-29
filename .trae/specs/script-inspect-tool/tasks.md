# Tasks

## Task 1: 实现 expand_script 工具 ✅
在 `通用MCP工具.cs` 中新增 `expand_script` 工具。

### 1.1 注册工具ID ✅
- [x] 在 `内置工具ID列表` 中添加 `"expand_script"`
- [x] 在 `执行内置工具` 的 switch 中添加 `case "expand_script"` 分发

### 1.2 实现 ExpandScript 方法 ✅
- [x] 解析参数获取 `脚本标识`（string）
- [x] 读取 `userdata.json` 遍历所有分类下所有 `.script` 文件（不限当前对话加载范围）
- [x] 先精确匹配脚本名称（不含扩展名）或完整路径
- [x] 再模糊匹配（名称 Contains）
- [x] 找到匹配后，通过 `脚本路径` → `File.ReadAllText` 读取文件
- [x] 解析 `.script` 文件：按 `\n@` 分割头部与节点列表
- [x] 格式化返回：结构化头部（脚本名/进程/窗口/备注）→ 节点列表 → 总数
- [x] 图片/二进制数据自动检测并省略
- [x] 若未找到匹配，返回友好错误提示
- [x] 若模糊匹配成功，在头部注明

### 1.3 格式化输出 ✅
```
脚本名: xxx
目标进程: xxx
目标窗口: xxx
备注: xxx

--- 节点列表 ---

节点 0: [节点类型]
  字段1: 值1
  字段2: 值2
  ...

--- 共 N 个节点 ---
```

---

## Task 2: 实现 search_scripts 工具 ✅
在 `通用MCP工具.cs` 中新增 `search_scripts` 工具。

### 2.1 注册工具ID ✅
- [x] 在 `内置工具ID列表` 中添加 `"search_scripts"`
- [x] 在 `执行内置工具` 的 switch 中添加 `case "search_scripts"` 分发

### 2.2 实现 SearchScripts 方法 ✅
- [x] 解析参数获取 `关键词`、`搜索范围`、`最大结果数`
- [x] 读取 `userdata.json` 遍历所有分类下所有脚本（不限当前对话加载范围）
- [x] "名称备注"：匹配脚本文件名 + 备注（从文件头部解析）
- [x] "完整内容"：额外读取 `.script` 文件全文匹配
- [x] 返回匹配列表，格式：`- 脚本名 (分类路径): 备注摘要`
- [x] 超过最大结果数时截断并标注剩余条数
- [x] 无匹配时返回友好提示
- [x] 返回结果中包含分类路径（通过递归查找匹配）

---

## Task 3: 更新 AI 隐藏提示词 ✅
在 `AI配置管理器.cs` 中更新【通用MCP工具使用指南】。

### 3.1 更新 `构建提示词()` (Ollama 路径) ✅
- [x] 新增 `expand_script` 工具描述
- [x] 新增 `search_scripts` 工具描述
- [x] 说明推荐工作流：`search_scripts` → `expand_script` → 全自动工具模仿创建

### 3.2 更新 `构建消息列表()` (OpenAI 路径) ✅
- [x] 新增 `expand_script` 工具描述（含参数、返回值、图片省略说明）
- [x] 新增 `search_scripts` 工具描述（含搜索范围说明）
- [x] 新增「脚本学习与模仿工作流」章节

---

## Task 4: 编译验证 ✅
- [x] 编译 `淼喵妙神奇工具库` 项目：0 错误
- [x] 编译 `淼喵妙用户界面` 项目：0 错误

# Task Dependencies
- Task 1 和 Task 2 并行完成
- Task 3 已完成（基于 Task 1、2 实现）
- Task 4 已完成（两个项目 0 错误）
