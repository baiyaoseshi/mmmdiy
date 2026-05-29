using System.Windows;
using 淼喵妙用户界面.ViewModels;

namespace 淼喵妙用户界面.Dialogs
{
    public partial class ScheduledTaskDialog : Window
    {
        private ScheduledTaskViewModel ViewModel { get; }

        public ScheduledTaskDialog(ScheduledTaskViewModel viewModel, bool 编辑模式 = false)
        {
            InitializeComponent();
            ViewModel = viewModel;
            viewModel.编辑模式 = 编辑模式;
            DataContext = viewModel;
            Title = 编辑模式 ? "编辑定时任务" : "添加定时任务";
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectScript();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Save())
            {
                DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}