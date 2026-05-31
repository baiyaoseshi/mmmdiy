# Tasks

- [x] Task 1: 添加 Moq 依赖到测试项目
  - [x] 1.1 在 `淼喵妙测试项目.csproj` 中添加 Moq NuGet 引用（最新稳定版）
  - [x] 1.2 `dotnet restore` 确认依赖解析成功

- [x] Task 2: 创建测试基础设施目录和基类
  - [x] 2.1 创建 `Harness/` 目录结构
  - [x] 2.2 创建 `测试基类.cs`：实现 `IAsyncLifetime`，提供 `InitializeAsync` / `DisposeAsync`，管理临时目录
  - [x] 2.3 创建 `通知工具Mock辅助.cs`：提供静态方法快速 mock 通知工具的所有委托
  - [x] 2.4 编译验证

- [x] Task 3: 创建集成测试 Fixture
  - [x] 3.1 创建 `集成测试Fixture.cs`：实现 `IAsyncLifetime`，管理 ChromaDB 客户端等共享资源
  - [x] 3.2 创建 `集成测试集合.cs`：定义 `[CollectionDefinition("集成测试")]`
  - [x] 3.3 编译验证

- [x] Task 4: 创建 WPF UI 测试支架
  - [x] 4.1 创建 `UI测试基类.cs`：继承 `测试基类`，封装 STA 线程相关辅助方法
  - [x] 4.2 添加 `AssertPropertyChanged` / `AssertPropertyChangedWithValue` 辅助方法
  - [x] 4.3 编译验证

- [x] Task 5: 迁移现有测试到新基础设施
  - [x] 5.1 `CoreTests.cs` 改为继承 `测试基类`
  - [x] 5.2 无需拆分
  - [x] 5.3 运行 `dotnet test` 确认所有 5 个现有测试通过

- [x] Task 6: 编写示例测试验证 Harness 可用性
  - [x] 6.1 添加 3 个使用 通知工具Mock辅助 的单元测试（信息弹窗/确认弹窗/错误弹窗）
  - [x] 6.2 添加 1 个 Moq 验证 INotifyPropertyChanged 的测试
  - [x] 6.3 添加 1 个 测试基类 临时文件测试
  - [x] 6.4 添加 1 个 `[StaFact]` WPF ViewModel 属性变更测试
  - [x] 6.5 运行 `dotnet test` 确认全部 11 个测试通过

# Task Dependencies
- Task 2 依赖 Task 1
- Task 3 依赖 Task 2
- Task 4 依赖 Task 2
- Task 5 依赖 Task 2
- Task 6 依赖 Task 1、Task 2、Task 4（可独立验证）
