using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using 淼喵妙神奇工具库.键鼠库.动作;
using 淼喵妙神奇工具库.键鼠库.动作.流程节点;
using 淼喵妙神奇工具库.键鼠库.监听.获取;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙用户界面.Dialogs;
using 淼喵妙用户界面.Services;

namespace 淼喵妙用户界面.ViewModels
{
    /// <summary>
    /// 脚本页面视图模型 - 管理单个脚本的编辑和执行
    /// </summary>
    public class ScriptPageViewModel : ViewModelBase
    {
        private string _scriptName;
        /// <summary>
        /// 脚本名称
        /// </summary>
        public string ScriptName
        {
            get => _scriptName;
            set
            {
                _scriptName = value;
                OnPropertyChanged(nameof(ScriptName));
            }
        }

        /// <summary>
        /// 页面名称（同脚本名称）
        /// </summary>
        public string PageName => ScriptName;

        private string _currentSavePath;
        /// <summary>
        /// 当前脚本保存路径
        /// </summary>
        public string CurrentSavePath
        {
            get => _currentSavePath;
            set
            {
                _currentSavePath = value;
                OnPropertyChanged(nameof(CurrentSavePath));
            }
        }

        private bool _isBranchScript;
        public bool IsBranchScript
        {
            get => _isBranchScript;
            set
            {
                _isBranchScript = value;
                OnPropertyChanged(nameof(IsBranchScript));
            }
        }

        private bool _isRunning;
        /// <summary>
        /// 脚本是否正在运行
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
            }
        }

        private string _windowTitle;
        /// <summary>
        /// 绑定的窗口标题
        /// </summary>
        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        private string _processName;
        /// <summary>
        /// 绑定的进程名称
        /// </summary>
        public string ProcessName
        {
            get => _processName;
            set
            {
                _processName = value;
                OnPropertyChanged(nameof(ProcessName));
            }
        }

        private string _applicationPath;
        /// <summary>
        /// 绑定的应用程序路径
        /// </summary>
        public string ApplicationPath
        {
            get => _applicationPath;
            set
            {
                _applicationPath = value;
                OnPropertyChanged(nameof(ApplicationPath));
            }
        }

        private string _remark;
        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark
        {
            get => _remark;
            set
            {
                _remark = value;
                OnPropertyChanged(nameof(Remark));
            }
        }

        private IntPtr _hWnd = IntPtr.Zero;
        /// <summary>
        /// 绑定的窗口句柄
        /// </summary>
        public IntPtr HWnd
        {
            get => _hWnd;
            set
            {
                _hWnd = value;
                OnPropertyChanged(nameof(HWnd));
            }
        }

        /// <summary>
        /// 脚本节点列表
        /// </summary>
        public ObservableCollection<控制节点> Nodes { get; } = new ObservableCollection<控制节点>();

        private 控制节点 _selectedNode;
        /// <summary>
        /// 当前选中的节点（向后兼容，始终指向多选中第一个或 null）
        /// </summary>
        public 控制节点 SelectedNode
        {
            get => SelectedNodes.FirstOrDefault();
            set
            {
                _selectedNode = value;
                OnPropertyChanged(nameof(SelectedNode));
            }
        }

        /// <summary>
        /// 多选节点集合
        /// </summary>
        public ObservableCollection<控制节点> SelectedNodes { get; } = new ObservableCollection<控制节点>();

        /// <summary>
        /// 从 ListBox.SelectedItems 同步到 SelectedNodes
        /// </summary>
        public void SyncSelectedNodes(IList selectedItems)
        {
            SelectedNodes.Clear();
            foreach (var item in selectedItems)
            {
                if (item is 控制节点 node)
                    SelectedNodes.Add(node);
            }
        }

        private static List<控制节点> _clipboard = new List<控制节点>();

        public bool HasClipboard
        {
            get => _clipboard.Count > 0;
        }

        private static List<控制节点> 深拷贝节点列表(IEnumerable<控制节点> nodes)
        {
            var temp = new 自动任务脚本(IntPtr.Zero);
            foreach (var node in nodes)
                temp.节点列表.Add(node);
            var serialized = temp.保存为字符串();
            var parsed = new 自动任务脚本(IntPtr.Zero, serialized);
            foreach (var node in parsed.节点列表)
            {
                node.成功后跳转 = null;
                node.失败后跳转 = null;
            }
            return parsed.节点列表;
        }

        public void CopySelectedNodes()
        {
            if (SelectedNodes.Count == 0)
                return;
            _clipboard = 深拷贝节点列表(SelectedNodes);
            OnPropertyChanged(nameof(HasClipboard));
        }

        public void PasteNodesAfter(控制节点 targetNode)
        {
            if (_clipboard.Count == 0)
                return;
            var copies = 深拷贝节点列表(_clipboard);
            if (targetNode == null)
            {
                foreach (var copy in copies)
                    Nodes.Add(copy);
            }
            else
            {
                var targetIndex = Nodes.IndexOf(targetNode);
                if (targetIndex < 0)
                    return;
                for (int i = 0; i < copies.Count; i++)
                    Nodes.Insert(targetIndex + 1 + i, copies[i]);
            }
            OnPropertyChanged(nameof(Nodes));
        }

        /// <summary>
        /// 脚本保存事件委托
        /// </summary>
        public delegate void ScriptSavedHandler(string scriptName, string scriptPath);
        /// <summary>
        /// 脚本保存事件
        /// </summary>
        public event ScriptSavedHandler ScriptSaved;

        /// <summary>
        /// 脚本运行事件委托
        /// </summary>
        public delegate void ScriptRunHandler(ScriptPageViewModel page);
        /// <summary>
        /// 脚本运行事件
        /// </summary>
        public event ScriptRunHandler ScriptRun;
        public event ScriptRunHandler BranchScriptRun;
        /// <summary>
        /// 从节点运行事件 - 携带页面和起始节点索引
        /// </summary>
        public event Action<ScriptPageViewModel, int> ScriptRunFromNode;
        /// <summary>
        /// 执行单个节点事件 - 仅执行该节点的动作，不继续后续节点
        /// </summary>
        public event Action<ScriptPageViewModel, 控制节点> RunSingleNodeRequested;
        /// <summary>
        /// 保存事件 - 立即保存用户数据
        /// </summary>
        public event Action Save;

        private readonly IFileService _fileService;

        /// <summary>
        /// 构造函数 - 创建一个新的脚本页面
        /// </summary>
        /// <param name="scriptName">脚本名称</param>
        /// <param name="mvm">主窗口视图模型引用</param>
        /// <param name="fileService">文件服务（可选）</param>
        public ScriptPageViewModel(string scriptName, MainWindowViewModel mvm, IFileService fileService = null)
        {
            ScriptName = scriptName;
            IsRunning = false;
            WindowTitle = "";
            ProcessName = "";
            Remark = "";
            Save = mvm.SaveUserData;
            _fileService = fileService ?? new FileService();
        }

        /// <summary>
        /// 绑定窗口 - 让用户选择要绑定的窗口
        /// </summary>
        public void BindWindow()
        {
            ShowToastNotification("请点击要绑定的窗口", "鼠标点击任意窗口完成绑定");
            
            System.Threading.Tasks.Task.Run(() =>
            {
                System.Threading.Thread.Sleep(500);
                IntPtr resultHwnd = IntPtr.Zero;
                var 窗口获取器 = new 获取窗口();
                resultHwnd = 窗口获取器.获取窗口句柄();

                _hWnd = resultHwnd;

                if (_hWnd != IntPtr.Zero)
                {
                    string windowTitle = 窗口处理器.获取窗口标题(_hWnd);
                    WindowTitle = windowTitle ?? string.Empty;

                    try
                    {
                        uint processId = 0;
                        窗口处理器.GetWindowThreadProcessId(_hWnd, out processId);
                        var process = Process.GetProcessById((int)processId);
                        ProcessName = process.ProcessName;
                        ApplicationPath = process.MainModule?.FileName;
                       通知工具.信息弹窗($"窗口绑定成功!\n窗口标题: {WindowTitle}\n进程名称: {ProcessName}\n应用路径: {ApplicationPath}");
                    }
                    catch
                    {
                        ProcessName = "未知";
                        ApplicationPath = null;
                       通知工具.信息弹窗($"窗口绑定成功!\n窗口标题: {WindowTitle}");
                    }
                }
                else
                {
                   通知工具.警告弹窗("未找到窗口!");
                }
            });
        }

        /// <summary>
        /// 显示通知提示
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        private void ShowToastNotification(string title, string message)
        {
            通知工具.吐司通知(message);
            通知工具.信息弹窗(message);
        }

        /// <summary>
        /// 运行脚本
        /// </summary>
        public void RunScript()
        {
            if (IsBranchScript)
                BranchScriptRun?.Invoke(this);
            else
                ScriptRun?.Invoke(this);
        }

        /// <summary>
        /// 从指定节点开始运行 - 将该脚本加入执行队列，从选中节点开始执行
        /// </summary>
        /// <param name="node">起始节点</param>
        public void RunScriptFromNode(控制节点 node)
        {
            if (node == null) return;
            int index = Nodes.IndexOf(node);
            if (index < 0) return;
            ScriptRunFromNode?.Invoke(this, index);
        }

        /// <summary>
        /// 仅执行当前节点 - 将该节点加入执行队列，执行完毕后不继续后续节点
        /// </summary>
        /// <param name="node">要执行的节点</param>
        public void ExecuteSingleNode(控制节点 node)
        {
            if (node == null) return;
            RunSingleNodeRequested?.Invoke(this, node);
        }

        /// <summary>
        /// 保存脚本到文件
        /// </summary>
        public async System.Threading.Tasks.Task SaveScript()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "脚本文件 (*.script)|*.script|所有文件 (*.*)|*.*",
                FileName = ScriptName,
                DefaultExt = ".script"
            };

            if (dialog.ShowDialog() == true)
            {
                string scriptContent = "";
                
                await System.Threading.Tasks.Task.Run(() =>
                {
                    var script = new 自动任务脚本(IntPtr.Zero);
                    script.绑定进程名 = ProcessName;
                    script.绑定窗口标题 = WindowTitle;
                    script.脚本备注 = Remark;
                    foreach (var node in Nodes)
                    {
                        script.节点列表.Add(node);
                    }
                    scriptContent = script.保存为字符串();
                });
                
                await System.Threading.Tasks.Task.Run(() => _fileService.WriteAllText(dialog.FileName, scriptContent));
                CurrentSavePath = dialog.FileName;
                ScriptName = Path.GetFileNameWithoutExtension(dialog.FileName);
                ScriptSaved?.Invoke(ScriptName, dialog.FileName);
                // 不再直接调用Save()进行立即保存，而是通过ScriptSaved事件触发OnScriptSaved处理函数
                // 在OnScriptSaved中会设置_needsSave = true，由自动保存计时器统一处理延迟保存
                通知工具.信息弹窗("脚本保存成功！");
            }
        }

        /// <summary>
        /// 添加节点到脚本
        /// </summary>
        /// <param name="node">要添加的节点</param>
        public void AddNode(控制节点 node)
        {
            Nodes.Add(node);
        }

        /// <summary>
        /// 从脚本中删除节点
        /// </summary>
        /// <param name="node">要删除的节点</param>
        public void DeleteNode(控制节点 node)
        {
            if (node != null)
            {
                Nodes.Remove(node);
                OnPropertyChanged(nameof(Nodes));
            }
        }

        /// <summary>
        /// 移动节点到指定位置 - 用于拖拽排序时调整节点顺序
        /// </summary>
        /// <param name="sourceIndex">源索引位置</param>
        /// <param name="targetIndex">目标索引位置</param>
        public void MoveNode(int sourceIndex, int targetIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= Nodes.Count ||
                targetIndex < 0 || targetIndex >= Nodes.Count ||
                sourceIndex == targetIndex)
                return;

            Nodes.Move(sourceIndex, targetIndex);
        }

        /// <summary>
        /// 重命名节点
        /// </summary>
        /// <param name="node">要重命名的节点</param>
        public void RenameNode(控制节点 node)
        {
            if (node != null)
            {
                var result = 通知工具.输入弹窗("请输入新的节点名称:", "重命名节点", node.节点名字?.ToString() ?? "");
                if (!string.IsNullOrEmpty(result))
                {
                    node.节点名字 = result;
                    OnPropertyChanged(nameof(Nodes));
                }
            }
        }

        /// <summary>
        /// 编辑节点属性
        /// </summary>
        /// <param name="node">要编辑的节点</param>
        public void EditNode(控制节点 node)
        {
            if (node != null)
            {
                var dialog = new NodeEditDialog(node, Nodes, _hWnd);
                if (dialog.ShowDialog() == true)
                {
                    OnPropertyChanged(nameof(Nodes));
                }
            }
        }

        public void BatchEditNodes()
        {
            if (SelectedNodes.Count == 0) return;

            var dialog = new BatchEditDialog(SelectedNodes.Count);
            if (dialog.ShowDialog() == true)
            {
                foreach (var node in SelectedNodes)
                {
                    if (dialog.修改成功后等待)
                        node.成功后等待 = dialog.成功后等待值;
                    if (dialog.修改失败后等待)
                        node.失败后等待 = dialog.失败后等待值;
                    if (dialog.修改节点备注)
                        node.节点备注 = dialog.节点备注值;
                }
                OnPropertyChanged(nameof(Nodes));
            }
        }

        public void AddCondition(控制节点 node)
        {
            if (node != null)
            {
                node.添加条件(_hWnd);
                OnPropertyChanged(nameof(Nodes));
            }
        }

        /// <summary>
        /// 设置窗口绑定信息
        /// </summary>
        /// <param name="processName">进程名称</param>
        /// <param name="windowTitle">窗口标题</param>
        /// <param name="applicationPath">应用程序路径</param>
        /// <param name="remark">备注</param>
        public void SetWindowBinding(string processName, string windowTitle, string applicationPath = null, string remark = null)
        {
            ProcessName = processName;
            WindowTitle = windowTitle;
            ApplicationPath = applicationPath;
            Remark = remark ?? "";
        }

        /// <summary>
        /// 清空窗口绑定信息 - 将窗口标题、进程名称、应用路径和句柄全部重置
        /// </summary>
        public void ClearWindowBinding()
        {
            WindowTitle = "";
            ProcessName = "";
            ApplicationPath = "";
            HWnd = IntPtr.Zero;
        }

        /// <summary>
        /// 重命名脚本 - 弹出输入框让用户输入新名称
        /// </summary>
        public void RenameScript()
        {
            string newName = 通知工具.输入弹窗("请输入新的脚本名称:", "改名", ScriptName ?? "");
            if (!string.IsNullOrEmpty(newName) && newName != ScriptName)
            {
                ScriptName = newName;
            }
        }

        /// <summary>
        /// 编辑备注 - 弹出输入框让用户修改备注
        /// </summary>
        public void EditRemark()
        {
            string newRemark = 通知工具.输入弹窗("请输入备注信息:", "修改备注", Remark ?? "");
            Remark = newRemark ?? "";
        }

        /// <summary>
        /// 一键停止请求事件 - 由 MainWindowViewModel 订阅处理
        /// </summary>
        public event Action<ScriptPageViewModel> StopAllTasksRequested;

        /// <summary>
        /// 一键停止当前页面所有关联任务
        /// </summary>
        public void StopAllTasksForThisPage()
        {
            StopAllTasksRequested?.Invoke(this);
        }

        /// <summary>
        /// 生成脚本内容字符串
        /// </summary>
        /// <returns>脚本内容</returns>
        public string GenerateScriptContent()
        {
            var script = new 自动任务脚本(IntPtr.Zero);
            script.绑定进程名 = ProcessName;
            script.绑定窗口标题 = WindowTitle;
            script.脚本备注 = Remark;
            foreach (var node in Nodes)
            {
                script.节点列表.Add(node);
            }
            return script.保存为字符串();
        }
    }
}