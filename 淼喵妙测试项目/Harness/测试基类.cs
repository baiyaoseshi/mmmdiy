using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace 淼喵妙测试项目.Harness
{
    public abstract class 测试基类 : IAsyncLifetime
    {
        protected string 临时目录 { get; private set; }

        public virtual ValueTask InitializeAsync()
        {
            临时目录 = Path.Combine(Path.GetTempPath(), $"miaomiao_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(临时目录);
            return ValueTask.CompletedTask;
        }

        public virtual ValueTask DisposeAsync()
        {
            if (Directory.Exists(临时目录))
            {
                try { Directory.Delete(临时目录, true); } catch { }
            }
            return ValueTask.CompletedTask;
        }

        protected string 创建临时文件(string 文件名, string 内容)
        {
            var 路径 = Path.Combine(临时目录, 文件名);
            File.WriteAllText(路径, 内容);
            return 路径;
        }
    }
}
