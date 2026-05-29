# Checklist

- [x] `AIChatMessage` 类中已添加 `是否私有` 布尔属性（默认 `false`），含 `INotifyPropertyChanged`
- [x] `是否私有` 字段 JSON 序列化/反序列化兼容（旧 `ai_config.json` 加载不报错）
- [x] `AIChatControl.设置消息()` 过滤掉 `是否私有 == true` 的消息
- [x] `AIChatControl.添加系统消息()` 对私有消息跳过 UI 添加
- [x] `AIChatControl.添加工具消息()` 对私有消息跳过 UI 添加
- [x] AIChatPage 中 `/screen` 指令注入的系统消息标记为 `是否私有 = true`
- [x] AIChatPage 中 `/web` 指令注入的系统消息标记为 `是否私有 = true`
- [x] AIChatPage 中工具进度/工具消息标记为 `是否私有 = true`
- [x] `AI配置管理器` 中 `[工具执行结果]` 系统消息标记为 `是否私有 = true`
- [x] `expand_script` 使用 `IndexOf("@")` 而非 `IndexOf("\n@")` 查找头部/节点边界
- [x] `expand_script` 换行符先统一为 `\n`（处理 `\r\n`），确保 `Split("\n@")` 正确
- [x] `expand_script` 节点 0 标签为第一个节点类型（如 `[绑定运行应用]`），不含 `@`
- [x] `expand_script` 节点数量与脚本实际节点数一致，不缺失任何节点
- [x] `search_scripts` 备注提取后截断 `@` 之后的内容
- [x] `search_scripts` 输出格式为 `- 脚本名: 备注摘要`（不含分类路径、不含节点内容）
- [x] 工具库项目编译通过
- [x] 用户界面项目编译通过
