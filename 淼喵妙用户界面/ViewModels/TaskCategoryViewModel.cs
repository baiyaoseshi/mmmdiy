using System.Collections.ObjectModel;

namespace 淼喵妙用户界面.ViewModels
{
    /// <summary>
    /// 任务分类视图模型 - 表示一个任务分类
    /// </summary>
    public class TaskCategoryViewModel : ViewModelBase
    {
        private string _categoryName;
        /// <summary>
        /// 分类名称
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
        /// 分类颜色
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
        /// 该分类下的任务路径列表
        /// </summary>
        public ObservableCollection<string> TaskPaths { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 子分类集合
        /// </summary>
        public ObservableCollection<TaskCategoryViewModel> SubCategories { get; } = new ObservableCollection<TaskCategoryViewModel>();

        private bool _isExpanded;
        /// <summary>
        /// 展开状态
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        private string _ai规则;
        /// <summary>
        /// 该分类的AI使用规则
        /// </summary>
        public string AI规则
        {
            get => _ai规则;
            set
            {
                _ai规则 = value;
                OnPropertyChanged(nameof(AI规则));
            }
        }

        private TaskCategoryViewModel _parentCategory;
        /// <summary>
        /// 父分类引用
        /// </summary>
        public TaskCategoryViewModel ParentCategory
        {
            get => _parentCategory;
            set
            {
                _parentCategory = value;
                OnPropertyChanged(nameof(ParentCategory));
            }
        }
    }
}