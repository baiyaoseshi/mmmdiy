# Tasks

- [x] Task 1: 实现 `ask_vision_ai` 通用视觉委托工具
  - [x] 在 `通用MCP工具.cs` 中添加 `ask_vision_ai` 工具定义（替换 `analyze_screen` 和 `locate_on_screen`），参数：`提示词`（必填，string，自定义视觉分析Prompt）
  - [x] 实现 `询问视觉AI(Dictionary<string, object> 参数)` 方法——截图 + 将自定义Prompt传给视觉AI + 返回视觉AI原始回复
  - [x] 在 `执行内置工具()` switch 中添加 `"ask_vision_ai"` case
  - [x] 从 `获取通用工具列表()` 中移除 `analyze_screen` 和 `locate_on_screen` 工具定义
  - [x] 更新 `内置工具ID列表`：移除 `analyze_screen` 和 `locate_on_screen`，添加 `ask_vision_ai`
  - [x] 删除旧的 `分析屏幕截图()` 和 `定位屏幕元素()` 方法

- [x] Task 2: 更新系统Prompt中的工具描述
  - [x] 在 `AI配置管理器.cs` 的 `构建消息列表()` 方法中，更新远程API的系统Prompt：
    - 将 `analyze_screen` 和 `locate_on_screen` 的描述替换为 `ask_vision_ai` 的描述
    - 移除 `web_search` 在脚本编辑工作流中的推荐地位（不删除工具本身描述）
  - [x] 在 `AI配置管理器.cs` 的 `构建提示词()` 方法中，更新Ollama的系统Prompt：
    - 将 `analyze_screen` 和 `locate_on_screen` 的描述替换为 `ask_vision_ai` 的描述
    - 移除 `web_search` 在脚本编辑工作流中的推荐地位

- [x] Task 3: 实现 `/web` 和 `/screen` 内置指令
  - [x] 在 `AIChatPage.xaml.cs` 的 `OnMessageSend()` 中，在 `/spec` 检查之后添加 `/web` 和 `/screen` 指令检测
  - [x] `/web` 检测：在用户消息前追加「先搜索再回答」系统指令，然后走正常流式带工具对话
  - [x] `/screen` 检测：在用户消息前追加「先看屏幕再回答」系统指令，然后走正常流式带工具对话
  - [x] 在 `AIChatControl.xaml.cs` 的 `InputTextBox_TextChanged()` 中，将 `/web` 和 `/screen` 作为内置指令加入匹配列表（始终出现在弹出菜单中，无需用户手动创建快捷指令）
  - [x] 选中 `/web` 或 `/screen` 后填入对应前缀（如 `/web `、`/screen `），光标定位到末尾让用户继续输入

- [x] Task 4: 编译验证
  - [x] 编译工具库项目，修复所有编译错误
  - [x] 编译用户界面项目，修复所有编译错误
  - [x] 确认无遗漏的 `analyze_screen` / `locate_on_screen` 引用

# Task Dependencies
- Task 2 depends on Task 1（需要先确定新工具名称和参数才能更新Prompt）
- Task 3 独立于 Task 1、2（可并行开发）
- Task 4 depends on Task 1、2、3
