using System.Collections.Generic;
using System.Windows;

namespace 淼喵妙用户界面.Dialogs
{
    public partial class OptionSelectionDialog : Window
    {
        public string SelectedOption { get; private set; }

        public string Prompt { get; set; }

        public OptionSelectionDialog(string prompt, List<string> options)
        {
            InitializeComponent();
            Prompt = prompt;
            DataContext = this;
            OptionListBox.ItemsSource = options;
            if (options.Count > 0)
            {
                SelectedOption = options[0];
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedOption = OptionListBox.SelectedItem as string;
            DialogResult = true;
        }

    }
}