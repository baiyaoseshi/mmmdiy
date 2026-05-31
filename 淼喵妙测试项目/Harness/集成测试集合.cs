using Xunit;

namespace 淼喵妙测试项目.Harness
{
    [CollectionDefinition("集成测试")]
    public class 集成测试集合 : ICollectionFixture<集成测试Fixture>
    {
    }
}
