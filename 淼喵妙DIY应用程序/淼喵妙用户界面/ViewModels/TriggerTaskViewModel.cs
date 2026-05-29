using System;
using System.Windows;
using 淼喵妙用户界面.Services;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙用户界面.ViewModels
{
    /// <summary>
    /// 触发任务视图模型 - 表示一个条件触发执行的任务
    /// </summary>
    public class TriggerTaskViewModel : ViewModelBase
    {
        /// <summary>
        /// 任务唯一标识
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();
        
        private string _taskName = "触发任务";
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName
        {
            get => _taskName;
            set
            {
                _taskName = value;
                OnPropertyChanged(nameof(TaskName));
            }
        }
        
        private string _triggerType = TaskConstants.TriggerType.指令;
        /// <summary>
        /// 触发类型
        /// </summary>
        public string TriggerType
        {
            get => _triggerType;
            set
            {
                _triggerType = value;
                OnPropertyChanged(nameof(TriggerType));
            }
        }
        
        private int _delaySeconds = 5;
        /// <summary>
        /// 触发后延迟执行的秒数
        /// </summary>
        public int DelaySeconds
        {
            get => _delaySeconds;
            set
            {
                _delaySeconds = value;
                OnPropertyChanged(nameof(DelaySeconds));
            }
        }
        
        private string _scriptPath;
        /// <summary>
        /// 脚本文件路径
        /// </summary>
        public string ScriptPath
        {
            get => _scriptPath;
            set
            {
                _scriptPath = value;
                OnPropertyChanged(nameof(ScriptPath));
            }
        }
        
        private bool _isEnabled = true;
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }
        
        private string _categoryName;
        /// <summary>
        /// 所属分类名称
        /// </summary>
        public string CategoryName
        {
            get => _categoryName;
            set
            {
                _categoryName = value;
                OnPropertyChanged(nameof(CategoryName));
            }
        }
        
        private string _categoryColor = TaskConstants.默认分类颜色;
        /// <summary>
        /// 所属分类颜色
        /// </summary>
        public string CategoryColor
        {
            get => _categoryColor;
            set
            {
                _categoryColor = value;
                OnPropertyChanged(nameof(CategoryColor));
            }
        }

        /// <summary>
        /// 构造函数 - 创建一个新的触发任务
        /// </summary>
        public TriggerTaskViewModel()
        {
        }

        /// <summary>
        /// 打开文件对话框选择脚本文件
        /// </summary>
        public void SelectScript()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "脚本文件 (*.script)|*.script|所有文件 (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                ScriptPath = dialog.FileName;
            }
        }

        /// <summary>
        /// 保存触发任务配置
        /// </summary>
        /// <returns>保存成功返回true，验证失败返回false</returns>
        public bool Save()
        {
            if (string.IsNullOrEmpty(ScriptPath))
            {
               通知工具.警告弹窗("请选择脚本文件");
                return false;
            }

            SaveRequested?.Invoke(this);
            通知工具.信息弹窗("触发任务已设置");
            return true;
        }

        /// <summary>
        /// 删除此触发任务
        /// </summary>
        public void Delete()
        {
            DeleteRequested?.Invoke(this);
        }

        /// <summary>
        /// 保存请求事件
        /// </summary>
        public event Action<TriggerTaskViewModel> SaveRequested;
        /// <summary>
        /// 删除请求事件
        /// </summary>
        public event Action<TriggerTaskViewModel> DeleteRequested;
    }
}