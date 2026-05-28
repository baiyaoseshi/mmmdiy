using System;
using System.Windows;
using System.Windows.Controls;
using 淼喵妙用户界面.ViewModels;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙用户界面.Controls
{
    /// <summary>
    /// 主页控件 - 显示任务管理界面
    /// </summary>
    public partial class MainPageControl : UserControl
    {
        public MainPageControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 分类项单击/双击 - 单击切换展开，双击设置/取消筛选
        /// </summary>
        private void CategoryItem_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            var category = element?.DataContext as TaskCategoryViewModel;
            if (category == null) return;

            if (e.ClickCount == 2)
            {
                var vm = GetMainViewModel();
                if (vm != null)
                {
                    if (vm.FilteredCategoryName == category.CategoryName)
                        vm.SetFilterCategory(null);
                    else
                        vm.SetFilterCategory(category);
                }
                e.Handled = true;
            }
            else if (e.ClickCount == 1)
            {
                GetMainViewModel()?.ToggleCategoryExpanded(category);
                e.Handled = true;
            }
        }

        /// <summary>
        /// 分类项右键菜单 - 根据分类类型显示不同菜单
        /// </summary>
        private void CategoryItem_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            var category = element?.DataContext as TaskCategoryViewModel;
            if (category == null || category.CategoryName == TaskConstants.默认分类名称)
            {
                e.Handled = true;
                return;
            }

            var vm = GetMainViewModel();
            if (vm == null) return;

            var contextMenu = new ContextMenu();
            bool isSubCategory = category.ParentCategory != null;

            if (!isSubCategory)
            {
                var addSubItem = new MenuItem { Header = "添加子分类" };
                addSubItem.Click += (s, args) => vm.AddSubCategory(category);
                contextMenu.Items.Add(addSubItem);
            }

            if (category.CategoryName != TaskConstants.默认分类名称)
            {
                var moveMenuItem = new MenuItem { Header = "移动为子分类" };
                foreach (var potentialParent in vm.TaskCategories)
                {
                    if (potentialParent == category) continue;
                    if (potentialParent.CategoryName == TaskConstants.默认分类名称) continue;
                    if (IsAncestorOf(category, potentialParent)) continue;
                    var subItem = new MenuItem
                    {
                        Header = potentialParent.CategoryName,
                        Icon = new System.Windows.Shapes.Ellipse
                        {
                            Width = 10,
                            Height = 10,
                            Fill = new System.Windows.Media.SolidColorBrush(
                                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(potentialParent.CategoryColor))
                        }
                    };
                    var capturedParent = potentialParent;
                    subItem.Click += (s, args) => vm.MoveCategoryToParent(category, capturedParent);
                    moveMenuItem.Items.Add(subItem);
                }
                if (moveMenuItem.Items.Count > 0)
                    contextMenu.Items.Add(moveMenuItem);

                var renameItem = new MenuItem { Header = "改名" };
                renameItem.Click += (s, args) => vm.RenameCategory(category);
                contextMenu.Items.Add(renameItem);

                var editAIRuleItem = new MenuItem { Header = "编辑AI规则" };
                editAIRuleItem.Click += (s, args) =>
                {
                    GetMainViewModel()?.EditCategoryAIRule(category);
                };
                contextMenu.Items.Add(editAIRuleItem);
            }

            if (isSubCategory)
            {
                var removeSubItem = new MenuItem { Header = "删除子分类" };
                removeSubItem.Click += (s, args) => vm.RemoveSubCategory(category);
                contextMenu.Items.Add(removeSubItem);

                var promoteItem = new MenuItem { Header = "提升为分类" };
                promoteItem.Click += (s, args) => vm.PromoteSubCategory(category);
                contextMenu.Items.Add(promoteItem);
            }
            else
            {
                var deleteItem = new MenuItem { Header = "解散分类" };
                deleteItem.Click += (s, args) => vm.RemoveCategory(category);
                contextMenu.Items.Add(deleteItem);
            }

            element.ContextMenu = contextMenu;
            element.ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private bool IsAncestorOf(TaskCategoryViewModel category, TaskCategoryViewModel potentialChild)
        {
            var current = potentialChild;
            while (current != null)
            {
                if (current == category) return true;
                current = current.ParentCategory;
            }
            return false;
        }

        /// <summary>
        /// 获取主窗口视图模型
        /// </summary>
        private MainWindowViewModel GetMainViewModel() => DataContext as MainWindowViewModel;

        /// <summary>
        /// 切换到任务管理模式按钮点击事件
        /// </summary>
        private void SwitchToTaskManagementButton_Click(object sender, RoutedEventArgs e)
        {
            GetMainViewModel()?.SwitchToTaskManagement();
        }

        /// <summary>
        /// 切换到运行状态模式按钮点击事件
        /// </summary>
        private void SwitchToRunningStatusButton_Click(object sender, RoutedEventArgs e)
        {
            GetMainViewModel()?.SwitchToRunningStatus();
        }

        /// <summary>
        /// 添加分类按钮点击事件
        /// </summary>
        private void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            GetMainViewModel()?.AddCategory();
        }

        /// <summary>
        /// 添加任务到队列按钮点击事件
        /// </summary>
        private void AddTaskToQueueButton_Click(object sender, RoutedEventArgs e)
        {
            GetMainViewModel()?.AddTaskToQueueDirectly();
        }

        /// <summary>
        /// 添加定时任务按钮点击事件
        /// </summary>
        private void AddScheduledTaskButton_Click(object sender, RoutedEventArgs e)
        {
            GetMainViewModel()?.ShowScheduledTaskDialog();
        }

        /// <summary>
        /// 添加触发任务按钮点击事件
        /// </summary>
        private void AddTriggerTaskButton_Click(object sender, RoutedEventArgs e)
        {
            GetMainViewModel()?.ShowTriggerTaskDialog();
        }

        /// <summary>
        /// 任务项删除按钮点击事件
        /// </summary>
        private void TaskItemDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var taskItem = button?.DataContext as TaskItemViewModel;
            taskItem?.Delete();
        }

        /// <summary>
        /// 定时任务编辑按钮点击事件
        /// </summary>
        private void ScheduledTaskEditButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.DataContext as ScheduledTaskViewModel;
            if (task != null)
            {
                GetMainViewModel()?.ShowScheduledTaskEditDialog(task);
            }
        }

        /// <summary>
        /// 定时任务删除按钮点击事件
        /// </summary>
        private void ScheduledTaskDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.DataContext as ScheduledTaskViewModel;
            task?.Delete();
        }

        /// <summary>
        /// 触发任务删除按钮点击事件
        /// </summary>
        private void TriggerTaskDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.DataContext as TriggerTaskViewModel;
            task?.Delete();
        }

        /// <summary>
        /// 已保存任务打开按钮点击事件
        /// </summary>
        private void SavedTaskOpenButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.DataContext as SavedTaskViewModel;
            task?.Edit();
        }

        /// <summary>
        /// 已保存任务添加到队列按钮点击事件
        /// </summary>
        private void SavedTaskAddToQueueButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.DataContext as SavedTaskViewModel;
            task?.AddToQueue();
        }

        /// <summary>
        /// 已保存任务删除按钮点击事件
        /// </summary>
        private void SavedTaskDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.DataContext as SavedTaskViewModel;
            task?.Delete();
        }

        /// <summary>
        /// 已保存任务移动到分类按钮点击事件
        /// </summary>
        private void SavedTaskMoveToCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var task = button?.DataContext as SavedTaskViewModel;
            var vm = GetMainViewModel();
            if (task == null || vm == null) return;

            var contextMenu = new ContextMenu();
            foreach (var category in vm.TaskCategories)
            {
                添加移动菜单项(contextMenu, vm, task, category, 0);
            }

            if (contextMenu.Items.Count == 0)
            {
                通知工具.信息弹窗("暂无其他分类，请先添加分类");
                return;
            }

            contextMenu.PlacementTarget = button;
            contextMenu.IsOpen = true;
        }

        private void 添加移动菜单项(ContextMenu contextMenu, MainWindowViewModel vm, SavedTaskViewModel task, TaskCategoryViewModel category, int depth)
        {
            if (category.CategoryName != task.CategoryName)
            {
                var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                if (depth > 0)
                    headerPanel.Children.Add(new TextBlock { Text = new string(' ', depth * 2), Width = depth * 12 });
                headerPanel.Children.Add(new System.Windows.Shapes.Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(category.CategoryColor)),
                    VerticalAlignment = VerticalAlignment.Center
                });
                headerPanel.Children.Add(new TextBlock
                {
                    Text = category.CategoryName,
                    Margin = new Thickness(4, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                });

                var menuItem = new MenuItem { Header = headerPanel };
                var capturedCategory = category;
                menuItem.Click += (s, args) =>
                {
                    vm.MoveSavedTaskToCategory(task, capturedCategory.CategoryName);
                };
                contextMenu.Items.Add(menuItem);
            }

            if (category.IsExpanded)
            {
                foreach (var sub in category.SubCategories)
                {
                    添加移动菜单项(contextMenu, vm, task, sub, depth + 1);
                }
            }
        }

        private void AIButton_Click(object sender, RoutedEventArgs e)
        {
            GetMainViewModel()?.SwitchToAIPage();
        }

        private void 远程指令开关_Changed(object sender, RoutedEventArgs e) { }

        private void 应用远程指令设置_Click(object sender, RoutedEventArgs e)
        {
            GetMainViewModel()?.应用远程指令配置();
        }
    }
}