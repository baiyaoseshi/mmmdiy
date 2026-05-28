using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using 淼喵妙用户界面.ViewModels;
using 淼喵妙用户界面.Dialogs;
using 淼喵妙神奇工具库.键鼠库.动作.流程节点;
using 淼喵妙神奇工具库.键鼠库.动作;

namespace 淼喵妙用户界面.Controls
{
    /// <summary>
    /// 脚本页面控件 - 用于编辑和管理自动化脚本
    /// </summary>
    public partial class ScriptPageControl : UserControl
    {
        /// <summary>
        /// 拖拽起始点 - 用于判断是否达到拖拽阈值
        /// </summary>
        private Point _dragStartPoint;
        /// <summary>
        /// 被拖拽的节点
        /// </summary>
        private 控制节点 _draggedNode;
        /// <summary>
        /// 拖拽源在节点列表中的索引
        /// </summary>
        private int _dragSourceIndex = -1;

        public ScriptPageControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 绑定窗口按钮点击事件 - 触发窗口绑定
        /// </summary>
        private void BindWindowButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            viewModel?.BindWindow();
        }

        /// <summary>
        /// 保存脚本按钮点击事件 - 保存脚本到文件
        /// </summary>
        private void SaveScriptButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            viewModel?.SaveScript();
        }

        /// <summary>
        /// 停止脚本按钮点击事件 - 一键停止当前页面所有关联任务
        /// </summary>
        private void StopScriptButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            viewModel?.StopAllTasksForThisPage();
        }

        /// <summary>
        /// 添加节点按钮点击事件 - 打开节点选择对话框
        /// </summary>
        private async void AddNodeButton_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            if (viewModel != null)
            {
                var dialog = new NodeSelectionDialog(viewModel.HWnd);
                if (dialog.ShowDialog() == true)
                {
                    viewModel.AddNode(await dialog.GetSelectedNode());
                }
            }
        }

        /// <summary>
        /// 删除节点菜单项点击事件 - 删除所有选中的节点
        /// </summary>
        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            if (viewModel == null || viewModel.SelectedNodes.Count == 0)
                return;

            var nodesToDelete = viewModel.SelectedNodes.ToList();
            foreach (var node in nodesToDelete)
            {
                viewModel.DeleteNode(node);
            }
        }

        /// <summary>
        /// 重命名节点菜单项点击事件 - 重命名选中的节点
        /// </summary>
        private void RenameMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            if (viewModel != null && viewModel.SelectedNode != null)
            {
                viewModel.RenameNode(viewModel.SelectedNode);
            }
        }

        /// <summary>
        /// 编辑节点菜单项点击事件 - 编辑选中节点的属性
        /// </summary>
        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            if (viewModel == null) return;

            if (viewModel.SelectedNodes.Count > 1)
            {
                viewModel.BatchEditNodes();
            }
            else if (viewModel.SelectedNode != null)
            {
                viewModel.EditNode(viewModel.SelectedNode);
            }
        }

        private void AddConditionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            if (viewModel != null && viewModel.SelectedNode != null)
            {
                viewModel.AddCondition(viewModel.SelectedNode);
            }
        }

        /// <summary>
        /// 从此运行按钮点击事件 - 从该节点开始运行脚本
        /// </summary>
        private void RunFromNodeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var node = button?.DataContext as 控制节点;
            var viewModel = DataContext as ScriptPageViewModel;
            viewModel?.RunScriptFromNode(node);
        }

        /// <summary>
        /// 执行此节点按钮点击事件 - 仅执行当前节点，不继续后续节点
        /// </summary>
        private void ExecuteSingleNodeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var node = button?.DataContext as 控制节点;
            var viewModel = DataContext as ScriptPageViewModel;
            viewModel?.ExecuteSingleNode(node);
        }

        /// <summary>
        /// 列表鼠标左键按下事件 - 记录拖拽起始位置和被拖拽的节点
        /// </summary>
        private void NodeListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;

            // 从原始源向上查找 ListBoxItem，确定用户点击了哪个节点项
            DependencyObject element = e.OriginalSource as DependencyObject;
            while (element != null && !(element is ListBoxItem))
            {
                element = VisualTreeHelper.GetParent(element);
            }

            if (element is ListBoxItem listBoxItem)
            {
                _draggedNode = listBoxItem.DataContext as 控制节点;
                _dragSourceIndex = listBox.Items.IndexOf(_draggedNode);
                _dragStartPoint = e.GetPosition(null);
            }
            else
            {
                _draggedNode = null;
                _dragSourceIndex = -1;
            }
        }

        /// <summary>
        /// 列表鼠标移动事件 - 当鼠标移动超过拖拽阈值时启动拖拽操作
        /// </summary>
        private void NodeListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _draggedNode == null)
                return;

            Point currentPos = e.GetPosition(null);
            Vector diff = _dragStartPoint - currentPos;

            // 检查鼠标移动是否超过系统拖拽阈值
            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var listBox = sender as ListBox;
                // 启动拖拽，传递源索引作为拖拽数据
                DragDrop.DoDragDrop(listBox, _dragSourceIndex, DragDropEffects.Move);
                _draggedNode = null;
                _dragSourceIndex = -1;
            }
        }

        /// <summary>
        /// 列表放置事件 - 接收拖拽数据并调整节点顺序，与Nodes集合保持同步
        /// </summary>
        private void NodeListBox_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(int)))
                return;

            int sourceIndex = (int)e.Data.GetData(typeof(int));
            var listBox = sender as ListBox;
            var viewModel = DataContext as ScriptPageViewModel;

            if (viewModel == null || sourceIndex < 0 || sourceIndex >= viewModel.Nodes.Count)
                return;

            // 计算鼠标在ListBox中的位置，确定目标插入位置
            Point dropPos = e.GetPosition(listBox);
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(listBox, dropPos);
            DependencyObject targetElement = hitTestResult?.VisualHit;

            // 从命中元素向上查找 ListBoxItem
            while (targetElement != null && !(targetElement is ListBoxItem))
            {
                targetElement = VisualTreeHelper.GetParent(targetElement);
            }

            int targetIndex;
            if (targetElement is ListBoxItem targetItem)
            {
                var targetNode = targetItem.DataContext as 控制节点;
                targetIndex = viewModel.Nodes.IndexOf(targetNode);
                if (targetIndex < 0)
                    targetIndex = viewModel.Nodes.Count - 1;
            }
            else
            {
                // 如果拖放到空白区域（列表末尾下方），则移到末尾
                targetIndex = viewModel.Nodes.Count - 1;
            }

            if (sourceIndex != targetIndex)
            {
                viewModel.MoveNode(sourceIndex, targetIndex);
            }
        }

        /// <summary>
        /// 列表选中变更事件 - 将 ListBox.SelectedItems 同步到 ViewModel.SelectedNodes
        /// </summary>
        private void NodeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            if (viewModel == null)
                return;
            viewModel.SyncSelectedNodes(NodeListBox.SelectedItems);
        }

        /// <summary>
        /// 复制菜单项点击事件 - 将选中节点深拷贝到内部剪贴板
        /// </summary>
        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            viewModel?.CopySelectedNodes();
        }

        /// <summary>
        /// 粘贴菜单项点击事件 - 将剪贴板节点插入到右键目标节点之后
        /// </summary>
        private void PasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ScriptPageViewModel;
            if (viewModel == null)
                return;

            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var targetNode = (contextMenu?.PlacementTarget as FrameworkElement)?.DataContext as 控制节点;
            viewModel.PasteNodesAfter(targetNode);
        }

    }
}