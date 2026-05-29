# Checklist

- [x] `.gitignore` 存在于 `d:\D\淼喵妙脚本DIY应用程序\.gitignore`，排除 `bin/`、`obj/`、`.vs/`、`*.user`、`*.suo`、`TestResults/`
- [x] 测试文件存在：`淼喵妙测试项目\CoreTests.cs`（或其他命名），包含至少 3 个 `[Fact]` 方法
- [x] 测试方法名使用英文（xUnit 惯例），断言调用中文命名的公开静态方法
- [x] `dotnet test "淼喵妙测试项目\淼喵妙测试项目.csproj"` 全部通过
- [x] `.github\workflows\dotnet.yml` 存在，包含 restore → build → test 三步
- [x] `README.md` 存在于 `d:\D\淼喵妙脚本DIY应用程序\README.md`
- [x] README 包含 CI 徽章（指向 GitHub Actions）
- [x] README 包含 Mermaid 架构图且语法正确
- [x] README 包含技术栈、核心能力列表、本地运行步骤
- [x] README 中英双语（上半中文、下半英文）
- [x] 完整 `dotnet build` 无编译错误
- [x] 所有新增文件不改动现有源码
