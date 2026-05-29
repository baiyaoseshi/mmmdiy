# GitHub 项目竞争力提升 Spec

## Why

项目已放到 GitHub 并写入简历，面试官/HR 打开仓库时，当前状态（无 README、无测试文件、无 CI、无 .gitignore）无法有效展示项目价值，需要最低成本改善第一印象。

## 英文/中文边界原则

* **保持中文不变**：所有现有源码的类名、方法名、变量名、注释保持中文

* **新增英文**：README（中英双语，上半中文下半英文）、.gitignore、CI 配置、测试方法名（xUnit 惯例）、测试类名

* **测试内部调用中文 API**：测试断言调用的是现有中文命名的公开方法，不改动源码

## What Changes

* 新增 `README.md`（中英双语架构、Mermaid 架构图、技术栈、本地运行说明）

* 新增 `.gitignore`（排除 bin/obj/.vs/TestResults 等，英文内容）

* 新增到已有测试项目 `淼喵妙测试项目/` 的测试 .cs 文件（测试方法名英文，调用中文 API）

* 新增 `.github/workflows/dotnet.yml` GitHub Actions CI（编译 + 测试，README 顶部有 CI 徽章）

* 可选：为 README 录制一个 15-30 秒的屏幕演示 GIF

## Impact

* Affected specs: 无（不修改现有功能）

* Affected code: 仅新增文件，不改动现有源码

* 风险：零，所有改动均为纯增量

## ADDED Requirements

### Requirement: README 中英双语

项目根目录 SHALL 包含 `README.md`，使用中英双语（上半中文、下半英文），包含以下内容：

* 项目一句话简介（多模态 AI Agent + RPA 桌面工具）

* CI 徽章（编译状态）

* 技术栈标签（C# / WPF / .NET 10 / Ollama / OpenAI / ONNX / OpenCV / PaddleOCR）

* Mermaid 架构图（四层：UI/MVVM → AI引擎 → 工具协议 → 操作系统层）

* 核心能力列表（4-5 项，每项简要说明）

* 本地运行步骤（前置条件、克隆、编译、运行）

* 截图占位指引（引导后续添加）

#### Scenario: 面试官打开 GitHub 仓库

* **WHEN** 面试官访问项目 GitHub 页面

* **THEN** 页面展示完整 README，含截图、架构图、技术栈标签

* **AND** README 顶部显示 CI 徽章

### Requirement: .gitignore 排除构建产物

项目根目录 SHALL 包含 `.gitignore`（英文内容），排除：

* `bin/`、`obj/`、`.vs/`、`*.user`、`*.suo`、`*.DotSettings.user`

* `TestResults/`、发布输出目录

* NuGet 缓存

#### Scenario: 代码提交时不包含构建产物

* **WHEN** 执行 `git add .`

* **THEN** `bin/` 和 `obj/` 目录下的文件不会被追踪

### Requirement: 核心单元测试

已有测试项目 `淼喵妙测试项目/`（xUnit + StaFact）SHALL 新增至少 3 个单元测试 `.cs` 文件，测试方法名使用英文（xUnit 惯例），断言调用中文命名的公开静态方法：

* `AI配置管理器.获取全局配置()` 返回非空 `AIConfigData`

* `MCP工具管理器.生成ASCII工具名(string)` 对纯中文名称返回 ASCII 标识符

* `AI使用经验管理器.从回复中提取计划(string)` 正确解析 `[计划]...[/计划]` 格式

#### Scenario: CI 自动运行测试

* **WHEN** 代码推送到 GitHub

* **THEN** `dotnet test` 全部通过，CI 徽章显示绿色

### Requirement: GitHub Actions CI

项目 SHALL 配置 `.github/workflows/dotnet.yml`（英文内容），在 `push` 到 `main` 时：

* `dotnet restore`

* `dotnet build --no-restore`

* `dotnet test --no-build`

#### Scenario: 提交代码后自动构建

* **WHEN** 向 `main` 推送代码

* **THEN** GitHub Actions 完成编译 + 测试，结果显示在仓库 Actions 页面

### Requirement: 项目演示录制（可选）

可选录制 15-30 秒演示 GIF，展示 AI 对话 → 工具调用 → 脚本创建流程。

#### Scenario: README 包含演示

* **WHEN** 面试官浏览 README

* **THEN** 可看到动态演示 GIF

