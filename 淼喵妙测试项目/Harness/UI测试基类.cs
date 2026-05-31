using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Xunit;

namespace 淼喵妙测试项目.Harness
{
    public abstract class UI测试基类 : 测试基类
    {
        protected static void AssertPropertyChanged<T>(T vm, string 属性名, Action 触发变更)
            where T : INotifyPropertyChanged
        {
            var 触发 = false;
            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == 属性名)
                    触发 = true;
            };

            触发变更();

            Assert.True(触发, $"属性 '{属性名}' 的 PropertyChanged 事件未被触发");
        }

        protected static void AssertPropertyChangedWithValue<T>(T vm, string 属性名, object 期望值, Action 触发变更)
            where T : INotifyPropertyChanged
        {
            object 实际值 = null;
            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == 属性名)
                {
                    var prop = typeof(T).GetProperty(属性名);
                    实际值 = prop?.GetValue(vm);
                }
            };

            触发变更();

            Assert.Equal(期望值, 实际值);
        }
    }
}
