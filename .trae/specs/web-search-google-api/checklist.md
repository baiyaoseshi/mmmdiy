# Checklist

- [x] `Google.Apis.CustomSearchAPI.v1` 包已添加到工具库 .csproj
- [x] `AIConfigData` 包含 `加密GoogleAPI密钥` 和 `Google搜索引擎ID` 字段
- [x] `联网搜索()` 方法使用 Google SDK 执行搜索，不再使用手写 HttpClient
- [x] `联网搜索()` 参数接口不变（查询关键词、最大结果数）
- [x] 未配置 API Key 或 CX 时返回友好错误提示
- [x] API 调用失败时有异常处理和错误信息
- [x] 搜索结果格式化包含标题、摘要、链接
- [x] 结果仍保留 6000 字符截断
- [x] AI 提示词中 web_search 描述已更新为 Google 引擎
- [x] GlobalAISettings 面板显示 Google API Key (PasswordBox) 和 CX 输入框
- [x] API Key 通过 DPAPI 加密存储/解密读取
- [x] 全局设置可正常保存和加载 Google 搜索配置
- [x] 工具库项目编译通过
- [x] 用户界面项目编译通过
