using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using 淼喵妙神奇工具库;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙用户界面.ViewModels;

namespace 淼喵妙用户界面
{
    /// <summary>
    /// 应用程序入口类 - 初始化通知系统
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 应用程序启动时初始化通知工具
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MCP工具管理器.清空MCP上下文();

            通知工具.初始化UI(
                信息弹窗: (message) =>
                {
                    Current.Dispatcher.Invoke(() =>
                        MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information));
                },
                输入弹窗: (prompt, title, defaultResponse) =>
                {
                    return Current.Dispatcher.Invoke(() =>
                        Microsoft.VisualBasic.Interaction.InputBox(prompt, title, defaultResponse));
                },
                吐司通知: (message) => ToastNotification.Show(message),
                确认弹窗: (message) =>
                {
                    return Current.Dispatcher.Invoke(() =>
                        MessageBox.Show(message, "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);
                },
                错误弹窗: (message) =>
                {
                    Current.Dispatcher.Invoke(() =>
                        MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error));
                },
                警告弹窗: (message) =>
                {
                    Current.Dispatcher.Invoke(() =>
                        MessageBox.Show(message, "警告", MessageBoxButton.OK, MessageBoxImage.Warning));
                },
                选项弹窗: (prompt, options) => ShowOptionDialog(prompt, options),
                图片查看器: (title, images) =>
                {
                    Current.Dispatcher.Invoke(() =>
                    {
                        var dialog = new Dialogs.ImageViewerDialog(title, images);
                        dialog.ShowDialog();
                    });
                });

            // 初始化远程指令服务器（延迟到空闲时执行，等待用户数据加载完成）
            Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var mainWindow = Current.MainWindow as MainWindow;
                    var vm = mainWindow?.DataContext as ViewModels.MainWindowViewModel;
                    if (vm?.远程指令配置 == null) return;

                    var config = vm.远程指令配置;
                    if (config.是否启用)
                    {
                        if (远程指令服务器.启动(config.端口号, config.认证令牌))
                        {
                            通知工具.吐司通知($"远程指令已开启，端口：{config.端口号}");
                        }
                    }

                    远程指令服务器.收到任务指令 += (指令) =>
                    {
                        vm.触发指令任务(指令.任务名);
                    };
                }
                catch { }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 应用程序退出时停止远程指令服务器
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            远程指令服务器.停止();
            base.OnExit(e);
        }

        /// <summary>
        /// 显示选项选择对话框
        /// </summary>
        /// <param name="prompt">提示信息</param>
        /// <param name="options">选项列表</param>
        /// <returns>用户选择的选项</returns>
        private string ShowOptionDialog(string prompt, List<string> options)
        {
            var dispatcher = Current.Dispatcher;

            return dispatcher.Invoke(() =>
            {
                 var dialog = new Dialogs.OptionSelectionDialog(prompt, options);
                return dialog.ShowDialog() == true ? dialog.SelectedOption : options[0];
            });
        }


    }

    /// <summary>
    /// 吐司通知静态类 - 管理通知的显示
    /// </summary>
    public static class ToastNotification
    {
        /// <summary>
        /// 线程同步锁对象
        /// </summary>
        private static readonly object _lock = new object();
        
        /// <summary>
        /// 通知计数器，用于管理多个通知的位置
        /// </summary>
        private static int _toastCount = 0;

        /// <summary>
        /// 显示吐司通知
        /// </summary>
        /// <param name="message">通知消息内容</param>
        public static void Show(string message)
        {
            var dispatcher = Application.Current.Dispatcher;

            dispatcher.BeginInvoke(new Action(() =>
            {
                lock (_lock)
                {
                    var toast = new ToastWindow(message, _toastCount);
                    // 最多同时显示6个通知，循环使用位置
                    _toastCount = (_toastCount + 1) % 6;
                    toast.Show();
                }
            }));
        }
    }

    /// <summary>
    /// 吐司通知窗口 - 显示在屏幕右下角的浮动通知
    /// </summary>
    public class ToastWindow : Window
    {
        /// <summary>
        /// 构造函数 - 创建吐司通知窗口
        /// </summary>
        /// <param name="message">通知消息</param>
        /// <param name="offset">通知偏移量，用于显示多个通知</param>
        public ToastWindow(string message, int offset)
        {
            Title = "通知";
            Width = 380;
            Height = 100;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.Manual;

            // 设置通知位置在屏幕右下角
            var screenWidth = System.Windows.SystemParameters.WorkArea.Width;
            var screenHeight = System.Windows.SystemParameters.WorkArea.Height;
            Left = screenWidth - Width - 20;
            Top = screenHeight - Height - 20 - (offset * 110);

            // 创建通知边框
            var border = new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                Padding = new Thickness(16),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(64, 158, 255))
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            // 通知图标
            var icon = new TextBlock
            {
                Text = "🔔",
                FontSize = 32,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // 通知文本
            var textBlock = new TextBlock
            {
                Text = message,
                FontSize = 16,
                Foreground = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            stackPanel.Children.Add(icon);
            stackPanel.Children.Add(textBlock);
            border.Child = stackPanel;
            Content = border;

            // 3秒后自动关闭通知
            new Thread(() =>
            {
                Thread.Sleep(3000);
                Dispatcher.Invoke(() => Close());
            }).Start();
        }
    }

}
