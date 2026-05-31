using Xunit;
using 淼喵妙神奇工具库;
using 淼喵妙测试项目.Harness;

namespace 淼喵妙测试项目
{
    public class CoreTests : 测试基类
    {
        [Fact]
        public void GetGlobalConfig_ReturnsNonNullConfig()
        {
            var config = AI配置管理器.获取全局配置();
            Assert.NotNull(config);
        }

        [Fact]
        public void GenerateAsciiToolName_PureChineseName_ReturnsToolPrefixHash()
        {
            var result = MCP工具管理器.生成ASCII工具名("测试工具");

            Assert.StartsWith("tool_", result);
            Assert.Equal(17, result.Length);
        }

        [Fact]
        public void GenerateAsciiToolName_AsciiName_ReturnsLowercaseAscii()
        {
            var result = MCP工具管理器.生成ASCII工具名("MyTool_123");

            Assert.Equal("mytool_123", result);
        }

        [Fact]
        public void ExtractPlan_ValidFormat_ReturnsPlanWithRequirementAndSteps()
        {
            var input = "[计划]需求=测试任务\n步骤=执行操作|tool_test[/计划]";

            var result = AI使用经验管理器.从回复中提取计划(input);

            Assert.NotNull(result);
            Assert.Equal("测试任务", result.需求);
            Assert.Single(result.子步骤);
            Assert.Equal("执行操作", result.子步骤[0].需求);
            Assert.Equal("tool_test", result.子步骤[0].工具ID);
        }

        [Fact]
        public void ExtractPlan_NoPlanBlock_ReturnsNull()
        {
            var input = "这是一条普通消息，没有计划块";

            var result = AI使用经验管理器.从回复中提取计划(input);

            Assert.Null(result);
        }
    }
}
