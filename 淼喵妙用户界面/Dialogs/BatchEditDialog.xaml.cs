using System.Windows;

namespace 淼喵妙用户界面.Dialogs
{
    public partial class BatchEditDialog : Window
    {
        public bool 修改成功后等待 => 修改成功后等待Check.IsChecked == true;
        public int 成功后等待值
        {
            get
            {
                int.TryParse(成功后等待TextBox.Text, out var val);
                return val;
            }
        }

        public bool 修改失败后等待 => 修改失败后等待Check.IsChecked == true;
        public int 失败后等待值
        {
            get
            {
                int.TryParse(失败后等待TextBox.Text, out var val);
                return val;
            }
        }

        public bool 修改节点备注 => 修改节点备注Check.IsChecked == true;
        public string 节点备注值 => 节点备注TextBox.Text;

        public BatchEditDialog(int nodeCount)
        {
            InitializeComponent();
            InfoText.Text = $"已选中 {nodeCount} 个节点";
        }

        private void 确定按钮_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void 取消按钮_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
