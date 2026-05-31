# Test Harness 验证清单

- [x] Moq 已添加到 `淼喵妙测试项目.csproj` 且 `dotnet restore` 成功
- [x] `Harness/测试基类.cs` 存在且实现 `IAsyncLifetime`
- [x] `Harness/通知工具Mock辅助.cs` 存在且覆盖所有 8 个委托
- [x] `Harness/集成测试Fixture.cs` 存在且实现 `IAsyncLifetime`
- [x] `Harness/集成测试集合.cs` 定义了 `[CollectionDefinition("集成测试")]`
- [x] `Harness/UI测试基类.cs` 存在且继承 `测试基类`
- [x] `dotnet build 淼喵妙测试项目.csproj` 编译通过
- [x] `dotnet test 淼喵妙测试项目.csproj` 全部测试通过（11/11：现有 5 个 + 新增 6 个）
- [x] 至少 1 个测试使用 Moq mock 外部依赖（`Moq_模拟INotifyPropertyChanged_事件触发验证`）
- [x] 至少 1 个测试使用 `[StaFact]` 验证 WPF ViewModel 行为（`StaFact_ViewModel属性变更_事件触发`）
- [x] 现有 `CoreTests` 测试迁移至继承 `测试基类` 后仍通过
