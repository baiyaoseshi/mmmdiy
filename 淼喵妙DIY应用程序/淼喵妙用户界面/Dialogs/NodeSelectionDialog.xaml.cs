using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using 淼喵妙神奇工具库.键鼠库.动作;
using 淼喵妙神奇工具库.键鼠库.动作.流程节点;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙用户界面.Dialogs
{
    public partial class NodeSelectionDialog : Window
    {
        public 控制节点 SelectedNode { get; private set; }
        private NodeTypeInfo SelectedNodeType { get; set; }
        private IntPtr _pageHwnd;

        public NodeSelectionDialog(IntPtr hWnd)
        {
            _pageHwnd = hWnd;
            InitializeComponent();
            LoadNodeTypesAsync();
        }
        public async Task<控制节点> GetSelectedNode()
        {
            while (SelectedNode?.节点名字 == null)
            {
                await Task.Delay(100);
            }
            return SelectedNode;
        }

        private async void LoadNodeTypesAsync()
        {
            await System.Threading.Tasks.Task.Run(() =>
            {
                var assembly = typeof(控制节点).Assembly;
                var nodeTypes = assembly.GetTypes()
                    .Where(t => typeof(控制节点).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    .ToList();

                var groupedNodes = new Dictionary<string, List<NodeTypeInfo>>();

                foreach (var type in nodeTypes)
                {
                    string category = GetCategory(type);
                    if (!groupedNodes.ContainsKey(category))
                    {
                        groupedNodes[category] = new List<NodeTypeInfo>();
                    }
                    groupedNodes[category].Add(new NodeTypeInfo { Type = type, DisplayName = type.Name });
                }

                var categoryGroups = groupedNodes.Select(kv => new CategoryGroup
                {
                    CategoryName = kv.Key,
                    NodeTypes = kv.Value.OrderBy(n => n.DisplayName).ToList()
                }).OrderBy(g => g.CategoryName).ToList();

                Dispatcher.Invoke(() =>
                {
                    CategoryItemsControl.ItemsSource = categoryGroups;
                });
            });
        }

        private string GetCategory(Type type)
        {
            string name = type.Name;

            // 指令与流程控制
            if (name == "PowerShell指令" || name == "停止任务" || name == "执行任务" || name == "显示变量"
                || name == "提示弹窗" || name == "智能执行任务" || name == "计算函数" || name == "空节点"
                || name == "从列表中按序读取" || name == "远程指令" || name == "插队" || name == "选定执行") return "指令与流程控制";

            // 应用与窗口
            if (name == "打开应用" || name == "绑定运行应用" || name == "调整窗口尺寸") return "应用与窗口";

            // 按键输入
            if (name == "单击按键" || name == "快捷键" || name == "输入文本" || name == "长按按键") return "按键输入";

            // 鼠标输入
            if (name.StartsWith("鼠标") || name == "点击文字" || name == "点击串联图片" || name == "点击图片") return "鼠标输入";

            // 判断与读取
            if (name == "感应屏幕" || name == "区域截图" || name == "图片位置" ||
                name == "识别文字" || name == "文字位置" ||
                name == "识别方框" || name == "识别进度条" || name == "智能识别目标" ||
                name == "读取Excel到全局" || name == "读取标题") return "判断与读取";

            // 高级拓展
            if (name == "HTTP请求" || name == "CSharp脚本" || name == "外部脚本") return "高级拓展";

            return "其他";
        }

        private void NodeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                SelectedNodeType = button.Tag as NodeTypeInfo;
                OKButton.IsEnabled = true;

                foreach (var child in FindVisualChildren<Button>(CategoryItemsControl))
                {
                    child.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(172, 172, 172));
                }
                button.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 122, 204));
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedNodeType != null)
            {
                try
                {
                    var createMethod = SelectedNodeType.Type.GetMethod("创建节点", new[] { typeof(IntPtr) });
                    if (createMethod != null)
                    {
                        OKButton.IsEnabled = false;

                        var flow = ExecutionContext.SuppressFlow();
                    try
                    {
                        Task.Run(() =>
                        {
                            SelectedNode = createMethod.Invoke(null, new object[] { _pageHwnd }) as 控制节点;
                                  
                            if (SelectedNode != null && string.IsNullOrEmpty(SelectedNode.节点名字))
                            {
                                SelectedNode.节点名字 = SelectedNodeType.DisplayName;
                            }
                        });
                    }
                    finally
                    {
                        flow.Undo();
                    }

                        DialogResult = true;

                        OKButton.IsEnabled = true;
                    }
                    else
                    {
                        通知工具.错误弹窗($"节点类型 {SelectedNodeType.DisplayName} 没有创建节点方法");
                    }
                }
                catch (Exception ex)
                {
                    this.Cursor = System.Windows.Input.Cursors.Arrow;
                    OKButton.IsEnabled = true;
                    通知工具.错误弹窗($"创建节点失败: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    yield return t;
                foreach (var item in FindVisualChildren<T>(child))
                    yield return item;
            }
        }

        public class NodeTypeInfo
        {
            public Type Type { get; set; }
            public string DisplayName { get; set; }
        }

        public class CategoryGroup
        {
            public string CategoryName { get; set; }
            public List<NodeTypeInfo> NodeTypes { get; set; }
        }
    }
}