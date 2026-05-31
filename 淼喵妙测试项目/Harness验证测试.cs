using System.ComponentModel;
using System.IO;
using Moq;
using Xunit;
using 淼喵妙测试项目.Harness;
using 淼喵妙用户界面.ViewModels;

namespace 淼喵妙测试项目
{
    public class Harness验证测试 : 测试基类
    {
        [Fact]
        public void Mock通知工具_信息弹窗_记录被追加()
        {
            通知工具Mock辅助.安装Mock通知();

            淼喵妙神奇工具库.输出库.通知工具.信息弹窗("测试消息");

            Assert.Single(通知工具Mock辅助.信息弹窗记录);
            Assert.Equal("测试消息", 通知工具Mock辅助.信息弹窗记录[0]);
        }

        [Fact]
        public void Mock通知工具_确认弹窗_可自定义返回值()
        {
            通知工具Mock辅助.确认弹窗行为 = _ => true;
            通知工具Mock辅助.安装Mock通知();

            var 结果 = 淼喵妙神奇工具库.输出库.通知工具.确认弹窗("确认?");

            Assert.True(结果);
            Assert.Single(通知工具Mock辅助.确认弹窗记录);
        }

        [Fact]
        public void Mock通知工具_错误弹窗_记录被追加()
        {
            通知工具Mock辅助.安装Mock通知();

            淼喵妙神奇工具库.输出库.通知工具.错误弹窗("错误!");

            Assert.Single(通知工具Mock辅助.错误弹窗记录);
            Assert.Equal("错误!", 通知工具Mock辅助.错误弹窗记录[0]);
        }

        [Fact]
        public void Moq_模拟INotifyPropertyChanged_事件触发验证()
        {
            var mock = new Mock<INotifyPropertyChanged>();
            var 触发 = false;
            mock.Object.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == "测试属性")
                    触发 = true;
            };

            mock.Raise(m => m.PropertyChanged += null, new PropertyChangedEventArgs("测试属性"));

            Assert.True(触发);
        }

        [Fact]
        public void 测试基类_创建临时文件_文件存在且有内容()
        {
            var 内容 = "测试内容123";
            var 路径 = 创建临时文件("test.txt", 内容);

            Assert.True(File.Exists(路径));
            Assert.Equal(内容, File.ReadAllText(路径));
        }

        [StaFact]
        public void StaFact_ViewModel属性变更_事件触发()
        {
            var vm = new 测试ViewModel();
            var 触发 = false;
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.名称))
                    触发 = true;
            };

            vm.名称 = "新名称";

            Assert.True(触发);
            Assert.Equal("新名称", vm.名称);
        }

        private class 测试ViewModel : ViewModelBase
        {
            private string _名称 = "";
            public string 名称
            {
                get => _名称;
                set
                {
                    _名称 = value;
                    OnPropertyChanged(nameof(名称));
                }
            }
        }
    }
}
