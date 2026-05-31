# 测试基础设施 (Test Harness) Spec

## Why
当前测试项目（淼喵妙测试项目）仅有 1 个 `CoreTests.cs` 文件、5 个 `[Fact]` 测试用例，严重缺乏系统性的测试基础设施。随着项目规模持续增长（76+ 源文件、~14k 行代码），补全测试 harneess 是保障代码质量和回归防护的当务之急。

## What Changes
- 引入 **Moq** mock 框架，隔离外部依赖
- 添加 **测试基类**，统一 setup/teardown 与共享工具方法
- 添加 **Collection Fixture**，用于管理重量级共享资源（如 ChromaDB 连接、Ollama 进程等）
- 添加 **集成测试 fixture**，支持需要真实外部依赖的场景
- 添加 **WPF UI 测试支架**，利用已有的 `Xunit.StaFact` 编写 UI 层测试
- 为现有测试迁移到新基础设施

## Impact
- Affected specs: 无（新增基础设施，不修改现有功能）
- Affected code: `淼喵妙测试项目/` 目录
  - 新增 `Harness/` 子目录（基类、fixture、工具类）
  - 修改 `淼喵妙测试项目.csproj`（添加 Moq 依赖）
  - 修改 `CoreTests.cs`（可选迁移至基类）

## ADDED Requirements

### Requirement: Mock 依赖隔离
系统 SHALL 通过 Moq 框架支持对核心库接口的 mock，使单元测试能隔离外部依赖。

#### Scenario: Mock AI 配置管理器
- **WHEN** 测试需要模拟 AI 配置返回特定值
- **THEN** 可使用 Moq 创建 `Mock<IAIConfigProvider>` 并注入，不触发真实 HTTP 调用

#### Scenario: Mock 通知工具
- **WHEN** 测试调用可能触发弹窗的代码
- **THEN** 可通过 mock 委托替代 `通知工具.*` 静态方法，避免测试中弹窗阻塞

### Requirement: 测试基类
系统 SHALL 提供 `测试基类` 作为所有测试的公共父类，封装通用 setup/teardown 逻辑。

#### Scenario: 自动初始化测试环境
- **WHEN** 测试类继承 `测试基类` 并执行任意测试
- **THEN** 自动完成基础初始化（如工作目录设置、临时文件清理等）
- **AND** 测试结束后自动清理临时资源

### Requirement: 集成测试 Fixture
系统 SHALL 提供 `集成测试集合` fixture，供需要真实外部服务（ChromaDB、Ollama）的测试共享资源。

#### Scenario: 共享 ChromaDB 连接
- **WHEN** 多个测试类标记为 `[Collection("集成测试")]`
- **THEN** 它们共享同一个 fixture 实例，避免重复启动 ChromaDB 客户端

### Requirement: WPF UI 测试支架
系统 SHALL 提供 `UI测试基类` 和辅助方法，支持基于 `StaFact` 的 WPF 组件测试。

#### Scenario: 测试 ViewModel 属性变更
- **WHEN** 在 STA 线程中设置 ViewModel 属性
- **THEN** `PropertyChanged` 事件按预期触发

### Requirement: 数据驱动测试支持
系统 SHALL 在基类中提供便捷的 `[Theory]` 数据源辅助方法。

#### Scenario: 批量验证节点序列化
- **WHEN** 使用 `[Theory]` + `[MemberData]` 提供多组节点数据
- **THEN** 每个数据组合作为独立测试用例执行，单组失败不影响其他组
