using System;
using System.IO;
using System.Windows;
using 淼喵妙用户界面.ViewModels;
using 淼喵妙用户界面.Controls;

namespace 淼喵妙用户界面
{
    /// <summary>
    /// 主窗口类 - 应用程序的主界面
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 获取主窗口的视图模型
        /// </summary>
        private MainWindowViewModel ViewModel => DataContext as MainWindowViewModel;
        
        /// <summary>
        /// 系统托盘图标
        /// </summary>
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        
        /// <summary>
        /// 标记托盘图标是否已释放
        /// </summary>
        private bool _notifyIconDisposed;

        /// <summary>
        /// 构造函数 - 初始化主窗口
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            
            // 初始化系统托盘图标
            InitializeNotifyIcon();
            
            // 监听视图模型的属性变化
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            // 初始化 AI 页面
            AIPage.初始化(ViewModel.GetAllCategoryNames());
            ViewModel.AIPage = AIPage;
        }

        /// <summary>
        /// 初始化系统托盘图标
        /// </summary>
        private void InitializeNotifyIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Text = "淼喵妙脚本DIY";
            
            // 尝试加载自定义图标，失败则使用默认图标
            string iconPath = "Resources/app.ico";
            if (System.IO.File.Exists(iconPath))
            {
                _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
            }
            else
            {
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            
            _notifyIcon.Visible = false;
            _notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            
            // 创建托盘右键菜单
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("显示窗口", null, ShowWindow_Click);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("退出", null, Exit_Click);
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        /// <summary>
        /// 托盘菜单 - 显示窗口
        /// </summary>
        private void ShowWindow_Click(object sender, EventArgs e)
        {
            this.Show();
            _notifyIcon.Visible = false;
            this.WindowState = WindowState.Normal;
        }

        /// <summary>
        /// 托盘菜单 - 退出应用程序
        /// </summary>
        private void Exit_Click(object sender, EventArgs e)
        {
            _notifyIconDisposed = true;
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            ViewModel?.SaveUserData();
            Application.Current.Shutdown();
        }

        /// <summary>
        /// 视图模型属性变化事件处理
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.SelectedPage) || e.PropertyName == nameof(ViewModel.IsAIPage))
            {
                UpdatePageVisibility();
            }
        }

        /// <summary>
        /// 更新页面可见性 - 在首页、脚本页面和AI页面之间切换
        /// </summary>
        private void UpdatePageVisibility()
        {
            if (ViewModel?.IsAIPage == true)
            {
                MainPage.Visibility = Visibility.Collapsed;
                AIPage.Visibility = Visibility.Visible;
                ScriptContent.Visibility = Visibility.Collapsed;
            }
            else if (ViewModel?.SelectedPage == null)
            {
                MainPage.Visibility = Visibility.Visible;
                AIPage.Visibility = Visibility.Collapsed;
                ScriptContent.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainPage.Visibility = Visibility.Collapsed;
                AIPage.Visibility = Visibility.Collapsed;
                ScriptContent.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// +按钮点击事件 - 添加新脚本页面
        /// </summary>
        private void AddNewPageButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel?.AddNewPage();
        }

        /// <summary>
        /// 首页按钮点击事件 - 返回首页
        /// </summary>
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsAIPage = false;
            ViewModel.SelectedPage = null;
        }

        /// <summary>
        /// 智能化按钮点击事件 - 切换到 AI 页面
        /// </summary>
        private void AIPageButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedPage = null;
            ViewModel.IsAIPage = true;
        }

        /// <summary>
        /// 窗口拖放事件 - 处理拖放的脚本文件
        /// </summary>
        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                
                // 遍历拖放的文件，打开符合条件的脚本
                foreach (string file in files)
                {
                    if (File.Exists(file) && (file.EndsWith(".script", StringComparison.OrdinalIgnoreCase) || 
                                              file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
                    {
                        ViewModel?.OpenScriptFromPath(file);
                    }
                }
            }
        }

        /// <summary>
        /// 窗口关闭事件 - 最小化到托盘而不是直接关闭
        /// </summary>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_notifyIconDisposed)
            {
                ViewModel?.Dispose();
                return;
            }
            
            ViewModel?.SaveUserData();
            
            e.Cancel = true;
            this.Hide();
            if (_notifyIcon != null)
                _notifyIcon.Visible = true;
        }

        /// <summary>
        /// 托盘图标双击事件 - 显示主窗口
        /// </summary>
        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            _notifyIcon.Visible = false;
            this.WindowState = WindowState.Normal;
        }
    }
}