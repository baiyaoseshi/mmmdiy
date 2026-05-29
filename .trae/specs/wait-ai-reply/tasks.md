# Tasks

- [x] Task 1: `AI配置管理器` 新增查找方法
  - [x] 新增 `按名称查找对话(string 名称)` 静态方法
  - [x] 遍历 `_数据.对话列表`，按名称精确匹配返回 `AIConversation`，未找到返回 null
  - [x] 编译验证

- [x] Task 2: `等待管理器` 扩展
  - [x] 新增 `等待类型.AI对话完成等待` 枚举值
  - [x] `等待上下文` 新增字段：`目标对话Id`、`目标对话名`
  - [x] 新增 `创建AI对话完成等待(string 当前对话Id, string 目标对话名, int 超时分钟 = 30)` 方法
    - 查找目标对话
    - 注册等待上下文
    - 启动超时计时器
  - [x] 新增 `通知AI回复完成(string 对话Id, string 消息内容)` 静态方法
    - 遍历活跃等待，匹配 `目标对话Id`
    - 触发 `等待完成?.Invoke(发起方对话Id, "💬", 消息内容)`
    - 移除等待
  - [x] 编译验证

- [x] Task 3: `通用MCP工具` 新增工具
  - [x] 在 `获取通用工具列表()` 中注册 `wait_for_ai_reply` 工具定义
  - [x] 在 `内置工具ID列表` 中添加 `"wait_for_ai_reply"`
  - [x] 实现 `等待AI回复()` 私有方法：
    - 参数验证：`对话名` 必填
    - 调用 `AI配置管理器.按名称查找对话()` 检查目标对话存在
    - 调用 `等待管理器.创建AI对话完成等待(对话Id, 对话名, 超时分钟)`
    - 返回等待启动确认信息
  - [x] 在 `执行内置工具()` switch 中添加 `"wait_for_ai_reply"` case
  - [x] 编译验证

- [x] Task 4: `AIChatPage` 添加完成通知
  - [x] 在 `OnMessageSend` 中 `完成AI回复()` 返回非空消息后，调用 `等待管理器.通知AI回复完成(对话.Id, 最后AI消息.内容)`
  - [x] 在 `/spec` 等待回调（`OnWaitCompleted`）中 `完成AI回复()` 返回非空消息后，同样调用通知
  - [x] 编译验证

- [x] Task 5: 编译验证
  - [x] `dotnet build` 工具库通过
  - [x] `dotnet build` 用户界面通过

# Task Dependencies
- Task 2 依赖 Task 1（需要 `按名称查找对话` 验证目标存在）
- Task 3 依赖 Task 1, 2
- Task 4 依赖 Task 2
- Task 5 依赖 Task 1-4
