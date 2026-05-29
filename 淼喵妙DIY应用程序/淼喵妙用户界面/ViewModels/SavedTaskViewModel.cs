using System;

namespace 淼喵妙用户界面.ViewModels
{
    /// <summary>
    /// 已保存任务视图模型 - 表示一个已保存的脚本任务
    /// </summary>
    public class SavedTaskViewModel : ViewModelBase
    {
        private string _taskName;
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

        private string _categoryColor;
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
        /// 添加到队列请求事件
        /// </summary>
        public event Action<SavedTaskViewModel> AddToQueueRequested;
        /// <summary>
        /// 编辑请求事件
        /// </summary>
        public event Action<SavedTaskViewModel> EditRequested;
        /// <summary>
        /// 删除请求事件
        /// </summary>
        public event Action<SavedTaskViewModel> DeleteRequested;

        /// <summary>
        /// 将任务添加到执行队列
        /// </summary>
        public void AddToQueue()
        {
            AddToQueueRequested?.Invoke(this);
        }

        /// <summary>
        /// 编辑任务
        /// </summary>
        public void Edit()
        {
            EditRequested?.Invoke(this);
        }

        /// <summary>
        /// 删除任务
        /// </summary>
        public void Delete()
        {
            DeleteRequested?.Invoke(this);
        }
    }
}