using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using 淼喵妙神奇工具库;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙用户界面.ViewModels;

namespace 淼喵妙用户界面.Controls
{
    public partial class GlobalAISettings : UserControl
    {
        private ObservableCollection<AINamedConfig> _配置列表 = new ObservableCollection<AINamedConfig>();
        private AINamedConfig _当前编辑配置;
        private ObservableCollection<QuickCommandViewModel> _快捷指令列表 = new ObservableCollection<QuickCommandViewModel>();

        public class QuickCommandViewModel : INotifyPropertyChanged
        {
            public string 名称 { get; set; }
            public string 触发短语 { get; set; }
            public string 完整Prompt { get; set; }
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event Action 关闭;
        public event Action 保存;

        public GlobalAISettings()
        {
            InitializeComponent();
            QuickCommandsList.ItemsSource = _快捷指令列表;
        }

        public void 加载()
        {
            var 原始列表 = AI配置管理器.获取配置列表();
            _配置列表.Clear();
            foreach (var item in 原始列表)
                _配置列表.Add(item);

            ConfigListBox.ItemsSource = _配置列表;

            if (_配置列表.Count > 0)
            {
                ConfigListBox.SelectedIndex = 0;
                从配置加载编辑器(_配置列表[0]);
            }

            CustomRulesTextBox.Text = AI配置管理器.获取全局自定义规则();
            EnableSelfLearningCheckBox.IsChecked = AI配置管理器.获取启用自主学习();
            EnableStatisticsCheckBox.IsChecked = AI配置管理器.获取启用增量记录();
            FilterPrivateMessagesCheckBox.IsChecked = AI配置管理器.获取过滤私有消息();
            更新快捷指令列表(AI配置管理器.获取全局快捷指令());

            var ollama列表 = 原始列表
                .Where(c => !string.IsNullOrEmpty(c.配置?.Ollama模型)
                    && c.配置?.提供者类型 != "OpenAI 兼容 API"
                    && c.配置?.提供者类型 != "DashScope (阿里云)")
                .ToList();
            var 经验Id = ExperienceAIConfigComboBox;
            if (经验Id != null)
            {
                经验Id.ItemsSource = null;
                经验Id.ItemsSource = ollama列表;
                经验Id.DisplayMemberPath = "名称";
                if (ollama列表.Count > 0)
                    经验Id.SelectedIndex = 0;
            }

            var mainVM = System.Windows.Application.Current.MainWindow?.DataContext as MainWindowViewModel;
            if (mainVM != null)
            {
                mainVM.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(mainVM.训练进度))
                        Dispatcher.InvokeAsync(() => TrainingProgressText.Text = mainVM.训练进度);
                };
                TrainingProgressText.Text = mainVM.训练进度;
            }
        }

        private void 保存编辑器值到当前配置()
        {
            if (_当前编辑配置 == null) return;
            _当前编辑配置.名称 = SelectedConfigNameTextBox.Text;
            var c = _当前编辑配置.配置;
            if (c == null)
            {
                c = new AIConfigData();
                _当前编辑配置.配置 = c;
            }
            c.提供者类型 = SelectedProviderComboBox.SelectedIndex switch { 0 => "Ollama本地", 2 => "DashScope (阿里云)", _ => "OpenAI 兼容 API" };
            c.Ollama地址 = SelectedOllamaHostTextBox.Text;
            c.Ollama模型 = SelectedOllamaModelTextBox.Text;
            c.远程API地址 = SelectedRemoteAPIUrlTextBox.Text;
            c.加密API密钥 = AI配置管理器.加密密钥(SelectedAPIKeyPasswordBox.Password);
            c.远程模型 = SelectedRemoteModelTextBox.Text;
            c.加密GoogleAPI密钥 = AI配置管理器.加密密钥(SelectedGoogleAPIKeyPasswordBox.Password);
            c.Google搜索引擎ID = SelectedGoogleCXTextBox.Text;
            c.最大输出Token = int.TryParse(SelectedMaxTokensTextBox.Text, out var mt) && mt > 0 ? mt : 8192;
            c.温度 = float.TryParse(SelectedTemperatureTextBox.Text, out var t) ? t : (float?)null;
        }

        private void 从配置加载编辑器(AINamedConfig config)
        {
            _当前编辑配置 = config;
            if (config == null) return;
            SelectedConfigNameTextBox.Text = config.名称 ?? "";
            var c = config.配置 ?? new AIConfigData();
            SelectedProviderComboBox.SelectedIndex = c.提供者类型 switch
            {
                "Ollama本地" => 0,
                "DashScope (阿里云)" => 2,
                _ => 1
            };
            SelectedOllamaHostTextBox.Text = c.Ollama地址 ?? "http://localhost:11434";
            SelectedOllamaModelTextBox.Text = c.Ollama模型 ?? "qwen2:0.5b";
            SelectedRemoteAPIUrlTextBox.Text = c.远程API地址 ?? "";
            SelectedAPIKeyPasswordBox.Password = AI配置管理器.解密密钥(c.加密API密钥);
            SelectedRemoteModelTextBox.Text = c.远程模型 ?? "deepseek-chat";
            SelectedGoogleAPIKeyPasswordBox.Password = AI配置管理器.解密密钥(c.加密GoogleAPI密钥);
            SelectedGoogleCXTextBox.Text = c.Google搜索引擎ID ?? "";
            SelectedMaxTokensTextBox.Text = c.最大输出Token > 0 ? c.最大输出Token.ToString() : "8192";
            SelectedTemperatureTextBox.Text = c.温度?.ToString() ?? "";
        }

        private void ConfigListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is AINamedConfig config)
            {
                if (_当前编辑配置 != null && _当前编辑配置 != config)
                    保存编辑器值到当前配置();
                从配置加载编辑器(config);
            }
        }

        private void AddConfigButton_Click(object sender, RoutedEventArgs e)
        {
            保存编辑器值到当前配置();
            var 新配置 = new AINamedConfig { 名称 = "新配置", 配置 = new AIConfigData() };
            _配置列表.Add(新配置);
            ConfigListBox.SelectedItem = 新配置;
        }

        private void DeleteConfigButton_Click(object sender, RoutedEventArgs e)
        {
            if (_配置列表.Count <= 1)
            {
                通知工具.警告弹窗("至少需要保留一个配置，不能删除。");
                return;
            }
            var selected = ConfigListBox.SelectedItem as AINamedConfig;
            if (selected == null) return;
            if (!通知工具.确认弹窗($"确认删除配置「{selected.名称}」？")) return;
            _配置列表.Remove(selected);
            ConfigListBox.SelectedIndex = 0;
        }

        private void SaveCurrentConfig_Click(object sender, RoutedEventArgs e)
        {
            if (_当前编辑配置 == null) return;
            保存编辑器值到当前配置();
            var 持久化列表 = AI配置管理器.获取配置列表();
            if (持久化列表.Any(c => c.Id == _当前编辑配置.Id))
                AI配置管理器.更新配置(_当前编辑配置);
            else
                AI配置管理器.添加配置(_当前编辑配置);

            ConfigListBox.ItemsSource = null;
            ConfigListBox.ItemsSource = _配置列表;
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            保存编辑器值到当前配置();
            if (_当前编辑配置 == null || _当前编辑配置.配置 == null)
            {
                通知工具.警告弹窗("请先选择一个配置。");
                return;
            }

            var cfg = _当前编辑配置.配置;
            var 按钮 = sender as Button;
            if (按钮 != null) 按钮.IsEnabled = false;

            try
            {
                if (cfg.提供者类型 == "Ollama本地")
                {
                    await 测试Ollama连接(cfg);
                }
                else
                {
                    await 测试远程API连接(cfg);
                }
            }
            finally
            {
                if (按钮 != null) 按钮.IsEnabled = true;
            }
        }

        private async Task 测试Ollama连接(AIConfigData cfg)
        {
            string host = string.IsNullOrEmpty(cfg.Ollama地址) ? "http://localhost:11434" : cfg.Ollama地址;
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            try
            {
                var response = await client.GetAsync($"{host}/api/tags").ConfigureAwait(true);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var models = doc.RootElement.TryGetProperty("models", out var arr) ? arr.GetArrayLength() : 0;
                    通知工具.信息弹窗($"Ollama 连接成功！\n地址: {host}\n已加载模型数: {models}");
                }
                else
                {
                    通知工具.警告弹窗($"Ollama 连接失败\n地址: {host}\nHTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
                }
            }
            catch (TaskCanceledException)
            {
                通知工具.警告弹窗($"Ollama 连接超时\n地址: {host}\n请检查 Ollama 服务是否已启动。");
            }
            catch (HttpRequestException ex)
            {
                通知工具.警告弹窗($"无法连接到 Ollama\n地址: {host}\n{ex.Message}");
            }
        }

        private async Task 测试远程API连接(AIConfigData cfg)
        {
            string apiUrl = cfg.远程API地址;
            string apiKey = AI配置管理器.解密密钥(cfg.加密API密钥);
            string model = string.IsNullOrEmpty(cfg.远程模型) ? "gpt-4o-mini" : cfg.远程模型;

            if (string.IsNullOrEmpty(apiUrl))
            {
                通知工具.警告弹窗("远程 API 地址为空，请填写 API 地址。");
                return;
            }
            if (string.IsNullOrEmpty(apiKey))
            {
                通知工具.警告弹窗("API 密钥为空，请填写密钥。");
                return;
            }

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var 请求体 = new
            {
                model,
                messages = new[] { new { role = "user", content = "hi" } },
                max_tokens = 1
            };
            var content = new StringContent(JsonSerializer.Serialize(请求体), Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync(apiUrl, content).ConfigureAwait(true);
                if (response.IsSuccessStatusCode)
                {
                    通知工具.信息弹窗($"API 连接成功！\n地址: {apiUrl}\n模型: {model}");
                }
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    string 摘要 = body.Length > 200 ? body.Substring(0, 200) + "..." : body;
                    通知工具.警告弹窗($"API 连接失败\n地址: {apiUrl}\n模型: {model}\nHTTP {(int)response.StatusCode}\n{摘要}");
                }
            }
            catch (TaskCanceledException)
            {
                通知工具.警告弹窗($"API 连接超时\n地址: {apiUrl}\n请检查网络和 API 地址是否正确。");
            }
            catch (HttpRequestException ex)
            {
                通知工具.警告弹窗($"无法连接到 API\n地址: {apiUrl}\n{ex.Message}");
            }
        }

        private void SelectedProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedOllamaPanel == null || SelectedRemoteAPIPanel == null) return;
            if (SelectedProviderComboBox.SelectedIndex == 0)
            {
                SelectedOllamaPanel.Visibility = Visibility.Visible;
                SelectedRemoteAPIPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                SelectedOllamaPanel.Visibility = Visibility.Collapsed;
                SelectedRemoteAPIPanel.Visibility = Visibility.Visible;
            }
        }

        private void 更新快捷指令列表(List<AIQuickCommand> commands)
        {
            _快捷指令列表.Clear();
            if (commands != null)
            {
                foreach (var cmd in commands)
                {
                    _快捷指令列表.Add(new QuickCommandViewModel
                    {
                        名称 = cmd.名称,
                        触发短语 = cmd.触发短语,
                        完整Prompt = cmd.完整Prompt
                    });
                }
            }
        }

        private void AddQuickCommand_Click(object sender, RoutedEventArgs e)
        {
            string 名称 = 通知工具.输入弹窗("快捷指令名称:", "添加快捷指令", "");
            if (string.IsNullOrEmpty(名称)) return;

            string 触发短语 = 通知工具.输入弹窗("触发短语（如 /login）:", "添加快捷指令", "");
            if (string.IsNullOrEmpty(触发短语)) return;

            string 完整Prompt = 通知工具.输入弹窗("完整 Prompt:", "添加快捷指令", "");
            if (string.IsNullOrEmpty(完整Prompt)) return;

            _快捷指令列表.Add(new QuickCommandViewModel { 名称 = 名称, 触发短语 = 触发短语, 完整Prompt = 完整Prompt });
        }

        private void DeleteQuickCommand_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string 短语)
            {
                var item = _快捷指令列表.FirstOrDefault(c => c.触发短语 == 短语);
                if (item != null)
                    _快捷指令列表.Remove(item);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            保存编辑器值到当前配置();

            var 持久化列表 = AI配置管理器.获取配置列表();
            var 持久化Id集合 = new HashSet<string>(持久化列表.Select(c => c.Id));

            foreach (var cfg in _配置列表)
            {
                if (持久化Id集合.Contains(cfg.Id))
                    AI配置管理器.更新配置(cfg);
                else
                    AI配置管理器.添加配置(cfg);
            }

            AI配置管理器.更新全局自定义规则(CustomRulesTextBox.Text);
            AI配置管理器.设置启用自主学习(EnableSelfLearningCheckBox.IsChecked == true);
            AI配置管理器.设置启用增量记录(EnableStatisticsCheckBox.IsChecked == true);
            AI配置管理器.设置过滤私有消息(FilterPrivateMessagesCheckBox.IsChecked == true);
            AI配置管理器.更新全局快捷指令(_快捷指令列表.Select(c => new AIQuickCommand
            {
                名称 = c.名称,
                触发短语 = c.触发短语,
                完整Prompt = c.完整Prompt
            }).ToList());

            if (ExperienceAIConfigComboBox?.SelectedItem is AINamedConfig 选中经验)
                AI配置管理器.更新经验AI配置(选中经验.Id);

            保存?.Invoke();
        }

        private void ClearLearningCache_Click(object sender, RoutedEventArgs e)
        {
            AI使用经验管理器.清空所有统计();
            通知工具.信息弹窗("工具调用统计数据已清除。");
        }

        private async void StartTraining_Click(object sender, RoutedEventArgs e)
        {
            var mainVM = System.Windows.Application.Current.MainWindow?.DataContext as MainWindowViewModel;
            if (mainVM == null) return;
            await mainVM.手动触发训练();
        }

        private void CleanData_Click(object sender, RoutedEventArgs e)
        {
            AI使用经验管理器.数据清洗();
            通知工具.信息弹窗("数据清洗完成。");
        }

        private void BuildEvalSet_Click(object sender, RoutedEventArgs e)
        {
            AI使用经验管理器.构建评测集();
            通知工具.信息弹窗("评测集构建完成。");
        }

        private void ClearImpressions_Click(object sender, RoutedEventArgs e)
        {
            if (!通知工具.确认弹窗("确认清空所有工具印象？此操作不可恢复。"))
                return;
            AI使用经验管理器.清空所有印象();
            通知工具.信息弹窗("所有工具印象已清空。");
        }

        private void ViewTrainingStats_Click(object sender, RoutedEventArgs e)
        {
            var stats = AI使用经验管理器.获取训练数据统计();
            通知工具.信息弹窗($"训练数据统计：\n总样本数: {stats.总样本数}\n高质量: {stats.高质量}\n低质量: {stats.低质量}\n孤立样本: {stats.孤立样本}");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            关闭?.Invoke();
        }
    }
}
