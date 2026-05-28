using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using 淼喵妙用户界面.ViewModels;

namespace 淼喵妙用户界面.Controls
{
    public partial class TabHeaderControl : UserControl
    {
        public event EventHandler? TabClicked;

        public TabHeaderControl()
        {
            InitializeComponent();
        }

        private void Grid_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            TabClicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 运行按钮点击事件 - 运行脚本
        /// </summary>
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            viewModel?.RunScript();
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            if (viewModel != null)
            {
                viewModel.SaveScript();
            }
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            if (viewModel != null)
            {
                var mainViewModel = Window.GetWindow(this)?.DataContext as MainWindowViewModel;
                if (mainViewModel != null)
                {
                    mainViewModel.ClosePage(viewModel);
                }
            }
        }

        private void RenameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            viewModel?.RenameScript();
        }

        private void EditRemarkMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            viewModel?.EditRemark();
        }

        /// <summary>
        /// 清空绑定窗口菜单项点击事件 - 清空脚本绑定的窗口数据
        /// </summary>
        private void ClearBindingMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            viewModel?.ClearWindowBinding();
        }

        /// <summary>
        /// 一键停止菜单项点击事件 - 停止当前标签页关联的所有任务
        /// </summary>
        private void StopAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            viewModel?.StopAllTasksForThisPage();
        }
    }
}
