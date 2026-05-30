using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using 淼喵妙神奇工具库;

namespace 淼喵妙用户界面.Controls
{
    public partial class AIConversationSettings : UserControl
    {
        private AIConversation _当前对话;
        private Dictionary<string, string> _分类规则映射;

        public class CategoryCheckItem : INotifyPropertyChanged
        {
            public string 分类名 { get; set; }
            private bool _是否选中;
            public bool 是否选中
            {
                get => _是否选中;
                set { _是否选中 = value; OnPropertyChanged(nameof(是否选中)); 勾选变化?.Invoke(); }
            }
            public event PropertyChangedEventHandler PropertyChanged;
            public event Action 勾选变化;
            protected void OnPropertyChanged(string name) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class 配置选项
        {
            public string Id { get; set; }
            public string 显示名 { get; set; }
        }

        public ObservableCollection<CategoryCheckItem> CategoryItems = new ObservableCollection<CategoryCheckItem>();

        private List<AINamedConfig> _可用配置列表;

        public event Action 关闭;
        public event Action<AIConversation> 保存;

        public AIConversationSettings()
        {
            InitializeComponent();
            CategoryCheckList.ItemsSource = CategoryItems;
        }

        public void 加载(AIConversation 对话, IEnumerable<string> 所有分类, Dictionary<string, string> 分类规则映射 = null)
        {
            _当前对话 = 对话;
            _分类规则映射 = 分类规则映射;

            CustomRulesTextBox.Text = 对话.自定义规则 ?? "";

            if (对话.最大输出Token.HasValue)
                MaxTokensSlider.Value = 对话.最大输出Token.Value;
            else
                MaxTokensSlider.Value = AI配置管理器.获取全局配置().最大输出Token > 0 ? AI配置管理器.获取全局配置().最大输出Token : 8192;
            MaxTokensValueText.Text = ((int)MaxTokensSlider.Value).ToString();

            if (对话.温度.HasValue)
                TemperatureSlider.Value = (double)对话.温度.Value;
            else
                TemperatureSlider.Value = (double)(AI配置管理器.获取全局配置().温度 ?? 1.0f);
            更新温度显示();

            WebSearchCheckBox.IsChecked = 对话.启用网页搜索;

            更新分类列表(所有分类);

            _可用配置列表 = AI配置管理器.获取配置列表();

            TextModelComboBox.Items.Clear();
            foreach (var cfg in _可用配置列表)
            {
                TextModelComboBox.Items.Add(new 配置选项 { Id = cfg.Id, 显示名 = cfg.名称 });
            }

            MultimodalModelComboBox.Items.Clear();
            MultimodalModelComboBox.Items.Add(new 配置选项 { Id = "", 显示名 = "使用文本模型" });
            foreach (var cfg in _可用配置列表)
            {
                MultimodalModelComboBox.Items.Add(new 配置选项 { Id = cfg.Id, 显示名 = cfg.名称 });
            }

            string 上次文本Id = AI配置管理器.获取上次文本配置Id();
            配置选项 文本默认选中 = null;
            if (!string.IsNullOrEmpty(对话.文本AI配置Id))
                文本默认选中 = TextModelComboBox.Items.OfType<配置选项>().FirstOrDefault(i => i.Id == 对话.文本AI配置Id);
            if (文本默认选中 == null && !string.IsNullOrEmpty(上次文本Id))
                文本默认选中 = TextModelComboBox.Items.OfType<配置选项>().FirstOrDefault(i => i.Id == 上次文本Id);
            if (文本默认选中 == null && TextModelComboBox.Items.Count > 0)
                文本默认选中 = TextModelComboBox.Items[0] as 配置选项;
            TextModelComboBox.SelectedItem = 文本默认选中;

            string 上次多模态Id = AI配置管理器.获取上次多模态配置Id();
            配置选项 多模态默认选中 = null;
            if (!string.IsNullOrEmpty(对话.多模态AI配置Id))
                多模态默认选中 = MultimodalModelComboBox.Items.OfType<配置选项>().FirstOrDefault(i => i.Id == 对话.多模态AI配置Id);
            if (多模态默认选中 == null && !string.IsNullOrEmpty(上次多模态Id))
                多模态默认选中 = MultimodalModelComboBox.Items.OfType<配置选项>().FirstOrDefault(i => i.Id == 上次多模态Id);
            if (多模态默认选中 == null)
                多模态默认选中 = MultimodalModelComboBox.Items.OfType<配置选项>().FirstOrDefault(i => i.Id == "");
            MultimodalModelComboBox.SelectedItem = 多模态默认选中;
        }

        public void 更新分类列表(IEnumerable<string> 所有分类)
        {
            CategoryItems.Clear();
            foreach (var cat in 所有分类)
            {
                var item = new CategoryCheckItem
                {
                    分类名 = cat,
                    是否选中 = _当前对话?.工具库分类列表?.Contains(cat) == true
                };
                item.勾选变化 += () => 刷新规则摘要();
                CategoryItems.Add(item);
            }
            刷新规则摘要();
        }

        private void 刷新规则摘要()
        {
            if (_分类规则映射 == null || _分类规则映射.Count == 0)
            {
                RuleSummaryHeader.Visibility = Visibility.Collapsed;
                RuleSummaryText.Visibility = Visibility.Collapsed;
                return;
            }

            var sb = new StringBuilder();
            foreach (var item in CategoryItems)
            {
                if (item.是否选中 && _分类规则映射.TryGetValue(item.分类名, out var rule) && !string.IsNullOrEmpty(rule))
                {
                    sb.AppendLine($"【{item.分类名}】{rule}");
                    sb.AppendLine();
                }
            }

            if (sb.Length == 0)
            {
                RuleSummaryHeader.Visibility = Visibility.Collapsed;
                RuleSummaryText.Visibility = Visibility.Collapsed;
            }
            else
            {
                RuleSummaryHeader.Visibility = Visibility.Visible;
                RuleSummaryText.Text = sb.ToString().TrimEnd();
                RuleSummaryText.Visibility = Visibility.Visible;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_当前对话 == null) return;

            _当前对话.工具库分类列表 = CategoryItems.Where(c => c.是否选中).Select(c => c.分类名).ToList();
            _当前对话.自定义规则 = CustomRulesTextBox.Text;
            _当前对话.最大输出Token = (int)MaxTokensSlider.Value;
            _当前对话.温度 = (float)Math.Round(TemperatureSlider.Value, 1);
            _当前对话.启用网页搜索 = WebSearchCheckBox.IsChecked == true;

            _当前对话.文本AI配置Id = (TextModelComboBox.SelectedItem as 配置选项)?.Id;
            _当前对话.多模态AI配置Id = (MultimodalModelComboBox.SelectedItem as 配置选项)?.Id;
            AI配置管理器.更新上次使用的配置Id(_当前对话.文本AI配置Id, _当前对话.多模态AI配置Id);

            AI配置管理器.更新对话(_当前对话);

            保存?.Invoke(_当前对话);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            关闭?.Invoke();
        }

        private void MaxTokensSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MaxTokensValueText != null)
                MaxTokensValueText.Text = ((int)MaxTokensSlider.Value).ToString();
        }

        private void TemperatureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            更新温度显示();
        }

        private void 更新温度显示()
        {
            if (TemperatureValueText == null) return;
            TemperatureValueText.Text = TemperatureSlider.Value.ToString("F1");
        }
    }
}
