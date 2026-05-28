using System.ComponentModel;

namespace 淼喵妙用户界面.ViewModels
{
    /// <summary>
    /// 视图模型基类 - 封装 INotifyPropertyChanged 接口的通用实现
    /// 所有需要属性变化通知的 ViewModel 都应继承此类
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// 属性变化事件
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 触发属性变化事件
        /// </summary>
        /// <param name="propertyName">属性名称</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}