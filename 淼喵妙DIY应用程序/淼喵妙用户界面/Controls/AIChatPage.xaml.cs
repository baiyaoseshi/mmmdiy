using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using 淼喵妙用户界面.ViewModels;
using 淼喵妙神奇工具库;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙用户界面.Controls
{
    public partial class AIChatPage : UserControl
    {
        private ObservableCollection<ConversationListItem> _对话列表项 = new ObservableCollection<ConversationListItem>();
        private bool _设置面板打开 = false;
        private bool _全局设置面板打开 = false;
        private List<string> _所有分类 = new List<string>();

        private readonly Barrier _文本屏障 = new Barrier(2);
        private Task _文本同步Task;
        private readonly Barrier _思考屏障 = new Barrier(2);
        private Task _思考同步Task;
        private AIChatMessage _当前进度消息;
        private CancellationTokenSource _aiCts;

        public class ConversationListItem : INotifyPropertyChanged
        {
            public string Id { get; set; }
            private string _名称;
            public string 名称
            {
                get => _名称;
                set { _名称 = value; OnPropertyChanged(nameof(名称)); }
            }
            private int _消息数量;
            public int 消息数量
            {
                get => _消息数量;
                set { _消息数量 = value; OnPropertyChanged(nameof(消息数量)); }
            }
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string name) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public AIChatPage()
        {
            InitializeComponent();
            ConversationListBox.ItemsSource = _对话列表项;
            ChatControl.消息发送 += OnMessageSend;
            ChatControl.打断请求 += OnStopRequest;
            等待管理器.等待完成 += OnWaitCompleted;
            ChatControl.文本更新完成 += () =>
            {
                if (_文本同步Task == null || _文本同步Task.IsCompleted)
                    _文本同步Task = Task.Run(async () =>
                    {
                        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                        _文本屏障.SignalAndWait();
                    });
            };
            ChatControl.思考更新完成 += () =>
            {
                if (_思考同步Task == null || _思考同步Task.IsCompleted)
                    _思考同步Task = Task.Run(async () =>
                    {
                        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);
                        _思考屏障.SignalAndWait();
                    });
            };

            SettingsPanel.关闭 += OnSettingsClose;
            SettingsPanel.保存 += OnSettingsSave;
            GlobalSettingsPanel.关闭 += OnGlobalSettingsClose;
            GlobalSettingsPanel.保存 += OnGlobalSettingsSave;
        }

        private void OnStopRequest()
        {
            _aiCts?.Cancel();
        }

        public void 初始化(IEnumerable<string> 所有分类)
        {
            _所有分类 = 所有分类?.ToList() ?? new List<string>();
            刷新对话列表();
        }

        private void 刷新对话列表()
        {
            var 对话列表 = AI配置管理器.获取所有对话();
            _对话列表项.Clear();

            foreach (var d in 对话列表)
            {
                _对话列表项.Add(new ConversationListItem
                {
                    Id = d.Id,
                    名称 = d.名称,
                    消息数量 = d.消息列表?.Count ?? 0
                });
            }

            string 当前Id = AI配置管理器.获取当前对话Id();
            if (!string.IsNullOrEmpty(当前Id))
            {
                var item = _对话列表项.FirstOrDefault(i => i.Id == 当前Id);
                if (item != null)
                {
                    ConversationListBox.SelectedItem = item;
                    加载当前对话();
                }
            }
            else if (_对话列表项.Count > 0)
            {
                ConversationListBox.SelectedIndex = 0;
                加载当前对话();
            }
        }

        private void 加载当前对话()
        {

            var 对话 = AI配置管理器.获取当前对话();
            if (对话 == null)
            {
                ChatControl.显示引导提示();
                return;
            }

            刷新聊天界面();
        }

        private void 刷新聊天界面()
        {
            var 对话 = AI配置管理器.获取当前对话();
            if (对话 == null) return;

            ChatControl.设置过滤私有消息(AI配置管理器.获取过滤私有消息());
            ChatControl.设置消息(对话.消息列表 ?? new List<AIChatMessage>());
            ChatControl.设置快捷指令(AI配置管理器.获取全局快捷指令());

            if (!AI配置管理器.配置是否有效(AI配置管理器.获取全局配置()))
            {
                ChatControl.显示引导提示();
            }
        }

        private async void OnMessageSend(string text)
        {
            var 对话 = AI配置管理器.获取当前对话();
            if (对话 == null || string.IsNullOrEmpty(text)) return;

            if (等待管理器.取消等待(对话.Id))
            {
                var waitMsg = new AIChatMessage { 角色 = "系统", 内容 = "⏰ 等待已取消（用户发送了新消息）。", 时间戳 = DateTime.Now };
                ChatControl.添加系统消息(waitMsg.内容);
                AI配置管理器.追加消息(对话.Id, waitMsg);
            }

            if (!AI配置管理器.配置是否有效(AI配置管理器.获取全局配置()))
            {
                var cfgMsg = new AIChatMessage { 角色 = "系统", 内容 = "请先点击右下角 ⚙️ 设置按钮，配置 AI 服务后再发送消息。", 时间戳 = DateTime.Now };
                ChatControl.添加系统消息(cfgMsg.内容);
                AI配置管理器.追加消息(对话.Id, cfgMsg);
                return;
            }

            ChatControl.设置是否正在发送(true);

            if (text.StartsWith("/spec", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await 处理Spec指令(text, 对话);
                }
                catch (Exception ex)
                {
                    var specSysMsg = new AIChatMessage { 角色 = "系统", 内容 = $"AI 调用异常: {ex.Message}", 时间戳 = DateTime.Now };
                    ChatControl.添加系统消息(specSysMsg.内容);
                    AI配置管理器.追加消息(对话.Id, specSysMsg);
                }
                ChatControl.设置是否正在发送(false);
                return;
            }

            bool 是联网指令 = text.StartsWith("/web ", StringComparison.OrdinalIgnoreCase) || text.Equals("/web", StringComparison.OrdinalIgnoreCase);
            bool 是视觉指令 = text.StartsWith("/screen ", StringComparison.OrdinalIgnoreCase) || text.Equals("/screen", StringComparison.OrdinalIgnoreCase);

            if (是联网指令 || 是视觉指令)
            {
                string 系统指令;
                if (是联网指令)
                {
                    系统指令 = "【强制要求】在回答之前，你必须**首先**调用 web_search 工具搜索互联网上的相关信息。结合搜索结果来回答用户的问题。如果 web_search 未配置（返回配置指引），则直接告知用户需要先配置 Google API Key 和 CX。";
                    text = text.Substring(4).Trim();
                }
                else
                {
                    系统指令 = "【强制要求】在回答之前，你必须**首先**调用 ask_vision_ai 工具查看当前屏幕。结合屏幕内容来回答用户的问题。如果视觉AI未配置（返回配置指引），则直接告知用户需要先配置视觉AI。";
                    text = text.Substring(7).Trim();
                }
                if (string.IsNullOrEmpty(text))
                    text = "请根据上述要求回答";

                AI配置管理器.追加消息(对话.Id, new AIChatMessage { 角色 = "系统", 内容 = 系统指令, 时间戳 = DateTime.Now, 是否私有 = true });
            }

            bool 成功 = false;
            _aiCts = new CancellationTokenSource();
            try
            {
                ChatControl.添加用户消息(text);
                AI配置管理器.追加消息(对话.Id, new AIChatMessage { 角色 = "用户", 内容 = text, 时间戳 = DateTime.Now });

                var 消息历史 = new List<AIChatMessage>(对话.消息列表 ?? new List<AIChatMessage>());
                ChatControl.开始AI回复();

                List<MCPToolDefinition> 工具列表;
                工具列表 = MCP工具管理器.加载工具列表(对话.工具库分类列表 ?? new List<string>());
                var 通用工具列表 = 通用MCP工具.获取通用工具列表();
                if (!对话.启用网页搜索)
                    通用工具列表.RemoveAll(t => t.工具ID == "web_search");
                工具列表.AddRange(通用工具列表);

                string 规则 = 拼接规则(AI配置管理器.获取全局自定义规则(), 对话.自定义规则);
                AI配置管理器.当前对话Id上下文.Value = 对话.Id;

                bool 需要新AI消息 = false;
                Func<string, Task> 文本回调 = chunk =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (需要新AI消息)
                        {
                            ChatControl.开始AI回复();
                            需要新AI消息 = false;
                        }
                        ChatControl.追加AI回复(chunk);
                    }));
                    _文本屏障.SignalAndWait();
                    return Task.CompletedTask;
                };
                Func<string, Task> 思考回调 = chunk =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (需要新AI消息)
                        {
                            ChatControl.开始AI回复();
                            需要新AI消息 = false;
                        }
                        ChatControl.追加AI思考(chunk);
                    }));
                    _思考屏障.SignalAndWait();
                    return Task.CompletedTask;
                };
                Func<string, Task> 进度回调 = progressMsg =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_当前进度消息 == null)
                            _当前进度消息 = ChatControl.开始工具进度(progressMsg);
                        else
                            ChatControl.更新工具进度(_当前进度消息, progressMsg);
                    }));
                    return Task.CompletedTask;
                };

                string 完整回复 = await AI配置管理器.调用AI流式带工具(解析AI配置(对话), 消息历史, 规则, 工具列表, 文本回调, toolMsg =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (!需要新AI消息)
                        {
                            var 已完成的AI消息 = ChatControl.完成当前流式消息();
                            if (已完成的AI消息 != null && !string.IsNullOrEmpty(已完成的AI消息.内容))
                            {
                                AI配置管理器.追加消息(对话.Id, new AIChatMessage { 角色 = "AI", 内容 = 已完成的AI消息.内容, 思考内容 = 已完成的AI消息.思考内容, 时间戳 = DateTime.Now });
                            }
                            需要新AI消息 = true;
                        }
                        if (_当前进度消息 != null)
                        {
                            ChatControl.更新工具进度(_当前进度消息, toolMsg);
                            ChatControl.完成工具进度(_当前进度消息);
                            _当前进度消息.是否私有 = true;
                            对话.消息列表.Add(_当前进度消息);
                            _当前进度消息 = null;
                        }
                        else
                        {
                            var msg = ChatControl.添加工具消息(toolMsg, 是否私有: true);
                            对话.消息列表.Add(msg);
                        }
                        AI配置管理器.保存数据();
                    });
                    return Task.CompletedTask;
                }, 思考回调, 进度回调, cancellationToken: _aiCts.Token);

                var 最后AI消息 = await Dispatcher.InvokeAsync(() => ChatControl.完成AI回复());

                if (最后AI消息 != null && !string.IsNullOrEmpty(最后AI消息.内容))
                    等待管理器.通知AI回复完成(对话.Id, 最后AI消息.内容);

                if (最后AI消息 != null && !string.IsNullOrEmpty(最后AI消息.内容))
                {
                    AI配置管理器.追加消息(对话.Id, new AIChatMessage { 角色 = "AI", 内容 = 最后AI消息.内容, 思考内容 = 最后AI消息.思考内容, 时间戳 = DateTime.Now });
                    成功 = true;
                }
                else if (!string.IsNullOrEmpty(完整回复))
                {
                    成功 = true;
                }
                else
                {
                    var sysMsg = new AIChatMessage { 角色 = "系统", 内容 = "AI 调用失败，请检查配置和网络连接。", 时间戳 = DateTime.Now };
                    ChatControl.添加系统消息(sysMsg.内容);
                    AI配置管理器.追加消息(对话.Id, sysMsg);
                }

            }
            catch (OperationCanceledException)
            {
                var cancelMsg = new AIChatMessage { 角色 = "系统", 内容 = "⏹ AI 回复已被用户中止", 时间戳 = DateTime.Now };
                ChatControl.添加系统消息(cancelMsg.内容);
                AI配置管理器.追加消息(对话.Id, cancelMsg);
            }
            catch (Exception ex)
            {
                var sysMsg = new AIChatMessage { 角色 = "系统", 内容 = $"AI 调用异常: {ex.Message}", 时间戳 = DateTime.Now };
                ChatControl.添加系统消息(sysMsg.内容);
                AI配置管理器.追加消息(对话.Id, sysMsg);
            }
            finally
            {
                ChatControl.设置是否正在发送(false);
                if (成功)
                {
                    更新侧边栏计数(对话.Id);
                }
            }
        }

        private void 更新侧边栏计数(string 对话Id)
        {
            var item = _对话列表项.FirstOrDefault(i => i.Id == 对话Id);
            if (item != null)
            {
                var 对话 = AI配置管理器.获取所有对话().FirstOrDefault(d => d.Id == 对话Id);
                if (对话 != null) item.消息数量 = 对话.消息列表?.Count ?? 0;
            }
        }

        private async Task 处理Spec指令(string input, AIConversation 对话)
        {
            ChatControl.添加用户消息(input);
            AI配置管理器.追加消息(对话.Id, new AIChatMessage { 角色 = "用户", 内容 = input, 时间戳 = DateTime.Now });

            string specContent = input.Substring(5).Trim();
            if (string.IsNullOrEmpty(specContent)) specContent = "请描述你想要的功能";

            var specPrompt = $"请按照以下格式生成功能规格文档。每个文档用```markdown代码块包裹，文档标题用markdown标题。\n\n"
                + $"# [功能名称] Spec\n\n## Why\n[简述]\n\n## What Changes\n- [变更列表]\n\n## Impact\n- [影响范围和文件]\n\n## ADDED Requirements\n### Requirement: [需求名]\n系统 SHALL ...\n\n#### Scenario: [场景名]\n- **WHEN** [条件]\n- **THEN** [预期结果]\n\n## 第三方库建议\n\n---\n\n# Tasks\n\n- [ ] Task 1: [任务描述]\n\n# Task Dependencies\n- [Task N] depends on [Task M]\n\n---\n\n# Checklist\n- [ ] [检查项]\n\n用户需求: {specContent}";

            var 消息历史 = new List<AIChatMessage>(对话.消息列表 ?? new List<AIChatMessage>());
            ChatControl.开始AI回复();

            string 规则 = 拼接规则(AI配置管理器.获取全局自定义规则(), 对话.自定义规则);
            AI配置管理器.当前对话Id上下文.Value = 对话.Id;

            Func<string, Task> 文本回调 = chunk =>
            {
                Dispatcher.BeginInvoke(new Action(() => ChatControl.追加AI回复(chunk)));
                _文本屏障.SignalAndWait();
                return Task.CompletedTask;
            };
            Func<string, Task> 思考回调 = chunk =>
            {
                Dispatcher.BeginInvoke(new Action(() => ChatControl.追加AI思考(chunk)));
                _思考屏障.SignalAndWait();
                return Task.CompletedTask;
            };

            string 完整回复 = await AI配置管理器.调用AI流式(AI配置管理器.获取全局配置(), 消息历史, 规则, 文本回调, 思考回调, cancellationToken: _aiCts.Token);

            var 最后AI消息 = await Dispatcher.InvokeAsync(() => ChatControl.完成AI回复());

            if (最后AI消息 != null && !string.IsNullOrEmpty(最后AI消息.内容))
            {
                AI配置管理器.追加消息(对话.Id, new AIChatMessage { 角色 = "AI", 内容 = 最后AI消息.内容, 思考内容 = 最后AI消息.思考内容, 时间戳 = DateTime.Now });
            }
            else if (!string.IsNullOrEmpty(完整回复))
            {
                AI配置管理器.追加消息(对话.Id, new AIChatMessage { 角色 = "AI", 内容 = 完整回复, 时间戳 = DateTime.Now });
            }
            else
            {
                var specFailMsg = new AIChatMessage { 角色 = "系统", 内容 = "AI 调用失败，请检查配置和网络连接。", 时间戳 = DateTime.Now };
                ChatControl.添加系统消息(specFailMsg.内容);
                AI配置管理器.追加消息(对话.Id, specFailMsg);
            }
        }

        private void NewConversationButton_Click(object sender, RoutedEventArgs e)
        {
            var 对话 = AI配置管理器.创建新对话();
            刷新对话列表();
        }

        private void ConversationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ConversationListBox.SelectedItem is ConversationListItem item)
            {
                AI配置管理器.设置当前对话(item.Id);
                加载当前对话();
            }
        }

        private void ConversationListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ConversationListBox.SelectedItem is ConversationListItem item)
            {
                string newName = 通知工具.输入弹窗("新名称:", "重命名对话", item.名称 ?? "");
                if (!string.IsNullOrEmpty(newName))
                {
                    var 对话 = AI配置管理器.获取所有对话().FirstOrDefault(d => d.Id == item.Id);
                    if (对话 != null)
                    {
                        对话.名称 = newName;
                        AI配置管理器.更新对话(对话);
                        item.名称 = newName;
                    }
                }
            }
        }

        private void ConversationListBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
        }

        private void DeleteConversationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ConversationListBox.SelectedItem is ConversationListItem item)
            {
                AI配置管理器.删除对话(item.Id);
                刷新对话列表();
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_设置面板打开)
            {
                关闭设置面板();
            }
            else
            {
                if (_全局设置面板打开) 关闭全局设置面板();
                打开设置面板();
            }
        }

        private void GlobalSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_全局设置面板打开)
            {
                关闭全局设置面板();
            }
            else
            {
                if (_设置面板打开) 关闭设置面板();
                打开全局设置面板();
            }
        }

        private void 打开设置面板()
        {
            var 对话 = AI配置管理器.获取当前对话();
            if (对话 == null)
            {
                对话 = AI配置管理器.创建新对话();
                刷新对话列表();
            }

            var 分类规则映射 = 收集分类规则();
            SettingsPanel.加载(对话, _所有分类, 分类规则映射);
            SettingsPanel.Visibility = Visibility.Visible;
            SettingsColumn.Width = new GridLength(300);
            _设置面板打开 = true;
        }

        private void 关闭设置面板()
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
            SettingsColumn.Width = new GridLength(0);
            _设置面板打开 = false;
        }

        private Dictionary<string, string> 收集分类规则()
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm == null) return new Dictionary<string, string>();
            var result = new Dictionary<string, string>();
            收集分类规则递归(vm.TaskCategories, "", result);
            return result;
        }

        private void 收集分类规则递归(IEnumerable<TaskCategoryViewModel> categories, string 前缀, Dictionary<string, string> result)
        {
            foreach (var cat in categories)
            {
                string 完整名 = string.IsNullOrEmpty(前缀) ? cat.CategoryName : $"{前缀}/{cat.CategoryName}";
                if (!string.IsNullOrEmpty(cat.AI规则))
                    result[完整名] = cat.AI规则;
                收集分类规则递归(cat.SubCategories, 完整名, result);
            }
        }

        private void 打开全局设置面板()
        {
            GlobalSettingsPanel.加载();
            GlobalSettingsPanel.Visibility = Visibility.Visible;
            SettingsColumn.Width = new GridLength(300);
            _全局设置面板打开 = true;
        }

        private void 关闭全局设置面板()
        {
            GlobalSettingsPanel.Visibility = Visibility.Collapsed;
            SettingsColumn.Width = new GridLength(0);
            _全局设置面板打开 = false;
        }

        private void OnSettingsClose()
        {
            关闭设置面板();
        }

        private void OnGlobalSettingsClose()
        {
            关闭全局设置面板();
        }

        private void OnGlobalSettingsSave()
        {
            ChatControl.设置快捷指令(AI配置管理器.获取全局快捷指令());
            刷新聊天界面();
            关闭全局设置面板();
        }

        private void OnSettingsSave(AIConversation 对话)
        {
            ChatControl.设置快捷指令(AI配置管理器.获取全局快捷指令());
            刷新对话列表();
            关闭设置面板();
        }

        private static string 拼接规则(string 全局规则, string 对话规则)
        {
            if (string.IsNullOrEmpty(全局规则) && string.IsNullOrEmpty(对话规则)) return "";
            if (string.IsNullOrEmpty(全局规则)) return 对话规则;
            if (string.IsNullOrEmpty(对话规则)) return 全局规则;
            return 全局规则 + "\n" + 对话规则;
        }

        private static AIConfigData 解析AI配置(AIConversation 对话)
        {
            var baseCfg = AI配置管理器.根据Id获取配置(对话.文本AI配置Id) ?? AI配置管理器.获取全局配置();

            var result = new AIConfigData
            {
                提供者类型 = baseCfg.提供者类型,
                Ollama地址 = baseCfg.Ollama地址,
                Ollama模型 = baseCfg.Ollama模型,
                远程API地址 = baseCfg.远程API地址,
                加密API密钥 = baseCfg.加密API密钥,
                远程模型 = baseCfg.远程模型,
                加密GoogleAPI密钥 = baseCfg.加密GoogleAPI密钥,
                Google搜索引擎ID = baseCfg.Google搜索引擎ID,
                最大输出Token = 对话.最大输出Token ?? baseCfg.最大输出Token,
                温度 = 对话.温度 ?? baseCfg.温度
            };
            return result;
        }

        private static AIConfigData 解析多模态AI配置(AIConversation 对话)
        {
            if (!string.IsNullOrEmpty(对话.多模态AI配置Id))
            {
                var cfg = AI配置管理器.根据Id获取配置(对话.多模态AI配置Id);
                if (cfg != null) return cfg;
            }
            return 解析AI配置(对话);
        }

        private async void OnWaitCompleted(string 对话Id, string 标题, string 描述)
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                var 对话 = AI配置管理器.获取当前对话();
                if (对话 == null || 对话.Id != 对话Id) return;
                if (ChatControl.IsSending) return;

                ChatControl.添加系统消息(描述);
                AI配置管理器.追加消息(对话.Id, new AIChatMessage { 角色 = "系统", 内容 = 描述, 时间戳 = DateTime.Now });

                bool 成功 = false;
                _aiCts = new CancellationTokenSource();
                try
                {
                    ChatControl.设置是否正在发送(true);

                    var 消息历史 = new List<AIChatMessage>(对话.消息列表 ?? new List<AIChatMessage>());
                    消息历史.Add(new AIChatMessage { 角色 = "用户", 内容 = $"（等待已完成：{描述}。请根据对话历史继续执行任务。）" });
                    ChatControl.开始AI回复();

                    List<MCPToolDefinition> 工具列表;
                    工具列表 = MCP工具管理器.加载工具列表(对话.工具库分类列表 ?? new List<string>());
                    var 通用工具列表2 = 通用MCP工具.获取通用工具列表();
                    if (!对话.启用网页搜索)
                        通用工具列表2.RemoveAll(t => t.工具ID == "web_search");
                    工具列表.AddRange(通用工具列表2);

                    string 规则2 = 拼接规则(AI配置管理器.获取全局自定义规则(), 对话.自定义规则);
                    AI配置管理器.当前对话Id上下文.Value = 对话.Id;

                    bool 需要新AI消息 = false;
                    Func<string, Task> 文本回调 = chunk =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (需要新AI消息)
                            {
                                ChatControl.开始AI回复();
                                需要新AI消息 = false;
                            }
                            ChatControl.追加AI回复(chunk);
                        }));
                        _文本屏障.SignalAndWait();
                        return Task.CompletedTask;
                    };
                    Func<string, Task> 思考回调 = chunk =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            if (需要新AI消息)
                            {
                                ChatControl.开始AI回复();
                                需要新AI消息 = false;
                            }
                            ChatControl.追加AI思考(chunk);
                        }));
                        _思考屏障.SignalAndWait();
                        return Task.CompletedTask;
                    };

                    string 完整回复 = await AI配置管理器.调用AI流式带工具(解析AI配置(对话), 消息历史, 规则2, 工具列表, 文本回调, toolMsg =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (!需要新AI消息)
                            {
                                var 已完成的AI消息 = ChatControl.完成当前流式消息();
                                if (已完成的AI消息 != null && !string.IsNullOrEmpty(已完成的AI消息.内容))
                                {
                                    AI配置管理器.追加消息(对话.Id, new AIChatMessage { 角色 = "AI", 内容 = 已完成的AI消息.内容, 思考内容 = 已完成的AI消息.思考内容, 时间戳 = DateTime.Now });
                                }
                                需要新AI消息 = true;
                            }
                            var msg = ChatControl.添加工具消息(toolMsg, 是否私有: true);
                            对话.消息列表.Add(msg);
                            AI配置管理器.保存数据();
                        });
                        return Task.CompletedTask;
                    }, 思考回调, cancellationToken: _aiCts.Token);

                    var 最后AI消息 = await Dispatcher.InvokeAsync(() => ChatControl.完成AI回复());

                    if (最后AI消息 != null && !string.IsNullOrEmpty(最后AI消息.内容))
                        等待管理器.通知AI回复完成(对话.Id, 最后AI消息.内容);

                    if (最后AI消息 != null && !string.IsNullOrEmpty(最后AI消息.内容))
                    {
                        AI配置管理器.追加消息(对话.Id, new AIChatMessage { 角色 = "AI", 内容 = 最后AI消息.内容, 思考内容 = 最后AI消息.思考内容, 时间戳 = DateTime.Now });
                        成功 = true;
                    }
                    else if (!string.IsNullOrEmpty(完整回复))
                    {
                        成功 = true;
                    }
                    else
                    {
                        var waitFailMsg = new AIChatMessage { 角色 = "系统", 内容 = "AI 调用失败，请检查配置和网络连接。", 时间戳 = DateTime.Now };
                        ChatControl.添加系统消息(waitFailMsg.内容);
                        AI配置管理器.追加消息(对话.Id, waitFailMsg);
                    }

                }
                catch (OperationCanceledException)
                {
                    var cancelMsg = new AIChatMessage { 角色 = "系统", 内容 = "⏹ AI 回复已被用户中止", 时间戳 = DateTime.Now };
                    ChatControl.添加系统消息(cancelMsg.内容);
                    AI配置管理器.追加消息(对话.Id, cancelMsg);
                }
                catch (Exception ex)
                {
                    var waitExMsg = new AIChatMessage { 角色 = "系统", 内容 = $"AI 调用异常: {ex.Message}", 时间戳 = DateTime.Now };
                    ChatControl.添加系统消息(waitExMsg.内容);
                    AI配置管理器.追加消息(对话.Id, waitExMsg);
                }
                finally
                {
                    ChatControl.设置是否正在发送(false);
                    if (成功)
                    {
                        更新侧边栏计数(对话.Id);
                    }
                }
            });
        }

        private static string 捕获屏幕截图() => AI配置管理器.捕获屏幕截图();
    }
}
