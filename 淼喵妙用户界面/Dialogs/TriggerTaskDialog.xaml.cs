using System.Windows;
using 淼喵妙用户界面.ViewModels;

namespace 淼喵妙用户界面.Dialogs
{
    public partial class TriggerTaskDialog : Window
    {
        private TriggerTaskViewModel ViewModel { get; }

        public TriggerTaskDialog(TriggerTaskViewModel viewModel)
        {
            InitializeComponent();
            ViewModel = viewModel;
            DataContext = viewModel;
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