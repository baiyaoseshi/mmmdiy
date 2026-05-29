# Tasks

- [x] Task 1: 创建 `.gitignore`
  - 在 `d:\D\淼喵妙脚本DIY应用程序\.gitignore` 创建文件
  - 排除 `bin/`、`obj/`、`.vs/`、`*.user`、`*.suo`、`*.DotSettings.user`、`TestResults/`
  - 排除 NuGet 缓存和发布输出目录
  - ✅ 验证：`git status` 不再显示 bin/obj 下的文件

- [x] Task 2: 编写核心单元测试
  - 在已有项目 `淼喵妙测试项目/` 下新增测试文件，测试方法名英文，断言调用中文 API
  - 测试 1：`AI配置管理器.获取全局配置()` 返回非 null
  - 测试 2：`MCP工具管理器.生成ASCII工具名("测试工具")` 返回非空 ASCII 标识符
  - 测试 3：`AI使用经验管理器.从回复中提取计划(string)` 解析标准计划格式
  - ✅ 验证：`dotnet test "淼喵妙测试项目\淼喵妙测试项目.csproj"` 全部通过
  - 步骤：
    - 2.1：创建测试文件 `淼喵妙测试项目\CoreTests.cs`，包含上述 3 个 `[Fact]` 测试
    - 2.2：运行 `dotnet test` 确认通过

- [x] Task 3: 创建 GitHub Actions CI 配置
  - 创建 `d:\D\淼喵妙脚本DIY应用程序\.github\workflows\dotnet.yml`
  - 触发条件：`push` 到 `main` 分支
  - 步骤：`dotnet restore` → `dotnet build` → `dotnet test`
  - ✅ 验证：配置文件存在且语法正确（推送后 GitHub 自动验证）

- [x] Task 4: 创建 `README.md`
  - 创建 `d:\D\淼喵妙脚本DIY应用程序\README.md`
  - 上半中文，下半英文
  - 包含：项目简介、CI 徽章、技术栈标签、Mermaid 架构图、核心能力列表、本地运行步骤、截图指引
  - ✅ 验证：GitHub 页面正确渲染 Markdown

- [x] Task 5: 编译验证
  - `dotnet build` 整体方案无错误
  - `dotnet test` 全部通过

# Task Dependencies
- Task 3 (CI) 依赖 Task 2 (测试)，因 CI 包含 `dotnet test` 步骤
- Task 1、Task 2、Task 4 可并行执行
- Task 5 在所有任务完成后最终验证
