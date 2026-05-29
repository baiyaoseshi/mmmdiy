using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using 淼喵妙神奇工具库;

namespace 淼喵妙用户界面.Controls
{
    public class StringNotEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolInvertConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : false;
        }
    }

    public partial class AIChatControl
    {
        private ObservableCollection<AIChatMessage> _消息列表 = new ObservableCollection<AIChatMessage>();
        private List<AIQuickCommand> _快捷指令 = new List<AIQuickCommand>();
        private bool _isSending = false;
        private AIChatMessage _当前流式消息;
        public bool 过滤私有消息 { get; set; } = true;

        public event Action<string> 消息发送;
        public event Action 文本更新完成;
        public event Action 思考更新完成;
        public event Action 打断请求;

        public AIChatControl()
        {
            InitializeComponent();
            MessagesItemsControl.ItemsSource = _消息列表;
        }

        public void 设置快捷指令(List<AIQuickCommand> commands)
        {
            _快捷指令 = commands ?? new List<AIQuickCommand>();
        }

        public void 设置过滤私有消息(bool 值)
        {
            过滤私有消息 = 值;
        }

        public void 设置消息(List<AIChatMessage> messages)
        {
            _消息列表 = new ObservableCollection<AIChatMessage>(
                (messages ?? new List<AIChatMessage>()).Where(m => !m.是否私有 || !过滤私有消息));
            MessagesItemsControl.ItemsSource = _消息列表;
            
        }

        public void 添加用户消息(string 内容)
        {
            var msg = new AIChatMessage { 角色 = "用户", 内容 = 内容, 时间戳 = DateTime.Now };
            _消息列表.Add(msg);
            滚动到底();
        }

        public AIChatMessage 开始AI回复()
        {
            var msg = new AIChatMessage { 角色 = "AI", 内容 = "", 时间戳 = DateTime.Now };
            _消息列表.Add(msg);
            
            _当前流式消息 = msg;
            return msg;
        }
        public void 追加AI回复(string 文本)
        {
            if (_当前流式消息 != null)
            {
                _当前流式消息.内容 += 文本;
                文本更新完成?.Invoke();

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (MessagesScrollViewer.VerticalOffset >= MessagesScrollViewer.ScrollableHeight - 15)
                        MessagesScrollViewer.ScrollToEnd();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            Thread.Sleep(5);
            文本更新完成?.Invoke();
        }

        public void 追加AI思考(string 文本)
        {
            if (_当前流式消息 != null)
            {
                _当前流式消息.思考内容 += 文本;
                思考更新完成?.Invoke();
            }
            Thread.Sleep(5);
            思考更新完成?.Invoke();
        }

        public string 获取当前思考内容() => _当前流式消息?.思考内容;

        public AIChatMessage 完成当前流式消息()
        {
            var msg = _当前流式消息;
            if (msg != null)
            {
                msg.思考已折叠 = true;
                折叠思考Expander(msg);
            }
            _当前流式消息 = null;
            return msg;
        }

        public AIChatMessage 完成AI回复()
        {
            var msg = _当前流式消息;
            if (msg != null)
            {
                msg.思考已折叠 = true;
                折叠思考Expander(msg);
            }
            _当前流式消息 = null;
            return msg;
        }

        private void 折叠思考Expander(AIChatMessage msg)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var container = MessagesItemsControl.ItemContainerGenerator.ContainerFromItem(msg);
                if (container != null)
                {
                    var expander = FindVisualChild<Expander>(container);
                    if (expander != null)
                        expander.IsExpanded = false;
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public void 添加系统消息(string 内容, bool 是否私有 = false)
        {
            var msg = new AIChatMessage { 角色 = "系统", 内容 = 内容, 时间戳 = DateTime.Now, 是否私有 = 是否私有 };
            if (!msg.是否私有 || !过滤私有消息)
                _消息列表.Add(msg);
            
        }

        public AIChatMessage 添加工具消息(string 内容, bool 是否私有 = false)
        {
            var msg = new AIChatMessage { 角色 = "系统", 内容 = 内容, 时间戳 = DateTime.Now, 是否私有 = 是否私有 };
            Dispatcher.Invoke(() =>
            {
                if (!msg.是否私有 || !过滤私有消息)
                    _消息列表.Add(msg);
                
            });
            return msg;
        }

        public AIChatMessage 开始工具进度(string 内容, bool 是否私有 = false)
        {
            var msg = new AIChatMessage { 角色 = "系统", 内容 = 内容, 时间戳 = DateTime.Now, 是否私有 = 是否私有 };
            Dispatcher.Invoke(() =>
            {
                if (!msg.是否私有 || !过滤私有消息)
                    _消息列表.Add(msg);
            });
            return msg;
        }

        public void 更新工具进度(AIChatMessage msg, string 内容)
        {
            Dispatcher.Invoke(() =>
            {
                msg.内容 = 内容;
            });
        }

        public void 完成工具进度(AIChatMessage msg)
        {
        }

        public void 设置是否正在发送(bool isSending)
        {
            _isSending = isSending;
            if (isSending)
            {
                SendButton.Content = "停止";
                SendButton.Background = new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36));
            }
            else
            {
                SendButton.Content = "发送";
                SendButton.Background = new SolidColorBrush(Color.FromRgb(0x21, 0x96, 0xF3));
            }
        }

        public bool IsSending => _isSending;

        public void 设置输入文本(string text)
        {
            InputTextBox.Text = text;
            InputTextBox.Focus();
        }

        public void 显示引导提示()
        {
            if (_消息列表.Count == 0)
            {
                添加系统消息("📋 请先配置 AI 服务\n点击右下角 ⚙️ 设置按钮，配置 Ollama 地址或远程 API 密钥后即可开始对话。");
            }
        }

        private void 滚动到底()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MessagesScrollViewer.ScrollToEnd();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                if (_isSending)
                {
                    打断请求?.Invoke();
                    return;
                }
                if (!string.IsNullOrWhiteSpace(InputTextBox.Text))
                {
                    string text = InputTextBox.Text;
                    InputTextBox.Clear();
                    消息发送?.Invoke(text);
                }
            }
            else if (e.Key == Key.Escape)
            {
                QuickCommandPopup.IsOpen = false;
            }
        }

        private void CopyMessage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is AIChatMessage msg && !string.IsNullOrEmpty(msg.内容))
            {
                Clipboard.SetText(msg.内容);
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSending)
            {
                打断请求?.Invoke();
                return;
            }
            if (!string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                string text = InputTextBox.Text;
                InputTextBox.Clear();
                消息发送?.Invoke(text);
            }
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = InputTextBox.Text;
            if (text.StartsWith("/"))
            {
                string prefix = text.Substring(1).ToLower();
                var matches = _快捷指令.Where(c =>
                    string.IsNullOrEmpty(prefix) || c.触发短语.ToLower().Contains(prefix) || c.名称.ToLower().Contains(prefix)
                ).ToList();

                if (string.IsNullOrEmpty(prefix) || "web".StartsWith(prefix) || prefix.StartsWith("web"))
                {
                    matches.Insert(0, new AIQuickCommand { 名称 = "/web 联网搜索后再回答", 触发短语 = "/web", 完整Prompt = "/web " });
                }
                if (string.IsNullOrEmpty(prefix) || "screen".StartsWith(prefix) || prefix.StartsWith("screen"))
                {
                    matches.Insert(0, new AIQuickCommand { 名称 = "/screen 视觉查看屏幕后再回答", 触发短语 = "/screen", 完整Prompt = "/screen " });
                }

                if (matches.Count > 0)
                {
                    QuickCommandListBox.ItemsSource = matches;
                    QuickCommandPopup.IsOpen = true;
                }
                else
                {
                    QuickCommandPopup.IsOpen = false;
                }
            }
            else
            {
                QuickCommandPopup.IsOpen = false;
            }
        }

        private void QuickCommandListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && QuickCommandListBox.SelectedItem is AIQuickCommand cmd)
            {
                QuickCommandPopup.IsOpen = false;
                InputTextBox.Text = cmd.完整Prompt;
                InputTextBox.CaretIndex = InputTextBox.Text.Length;
                InputTextBox.Focus();
                e.Handled = true;
            }
        }

        private void QuickCommandListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (QuickCommandListBox.SelectedItem is AIQuickCommand cmd)
            {
                QuickCommandPopup.IsOpen = false;
                InputTextBox.Text = cmd.完整Prompt;
                InputTextBox.CaretIndex = InputTextBox.Text.Length;
                InputTextBox.Focus();
            }
        }

        public List<AIQuickCommand> 解析Spec指令(string 输入文本)
        {
            if (输入文本.StartsWith("/spec", StringComparison.OrdinalIgnoreCase))
            {
                var list = new List<AIQuickCommand>();
                return list;
            }
            return null;
        }
    }
}
