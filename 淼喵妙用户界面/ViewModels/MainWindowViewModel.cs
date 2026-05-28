using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Diagnostics;
using 淼喵妙神奇工具库;
using 淼喵妙神奇工具库.键鼠库.动作;
using 淼喵妙神奇工具库.键鼠库.监听.获取;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙用户界面.Data;
using 淼喵妙用户界面.Services;
using 淼喵妙用户界面.Helpers;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙用户界面.ViewModels
{
    /// <summary>
    /// 主窗口视图模型 - 管理整个应用程序的核心逻辑
    /// </summary>
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private ScriptPageViewModel _selectedPage;
        /// <summary>
        /// 当前选中的脚本页面
        /// </summary>
        public ScriptPageViewModel SelectedPage
        {
            get => _selectedPage;
            set
            {
                _selectedPage = value;
                OnPropertyChanged(nameof(SelectedPage));
                OnPropertyChanged(nameof(IsHomePage));
            }
        }

        /// <summary>
        /// 是否在首页
        /// </summary>
        public bool IsHomePage => SelectedPage == null && !IsAIPage;

        private bool _isAIPage = false;
        public Controls.AIChatPage AIPage { get; set; }
        /// <summary>
        /// 是否在智能化页面
        /// </summary>
        public bool IsAIPage
        {
            get => _isAIPage;
            set
            {
                bool wasAIPage = _isAIPage;
                _isAIPage = value;
                OnPropertyChanged(nameof(IsAIPage));
                OnPropertyChanged(nameof(IsHomePage));

                if (wasAIPage && !_isAIPage)
                {
                    if (AIPage != null && AIPage.有未总结消息)
                    {
                        触发AI经验总结();
                    }
                }
            }
        }

        private bool _isTaskManagementMode = true;
        /// <summary>
        /// 是否在任务管理模式
        /// </summary>
        public bool IsTaskManagementMode
        {
            get => _isTaskManagementMode;
            set
            {
                _isTaskManagementMode = value;
                OnPropertyChanged(nameof(IsTaskManagementMode));
            }
        }

        /// <summary>
        /// 脚本页面集合
        /// </summary>
        public ObservableCollection<ScriptPageViewModel> ScriptPages { get; } = new ObservableCollection<ScriptPageViewModel>();
        /// <summary>
        /// 任务执行队列
        /// </summary>
        public ObservableCollection<TaskItemViewModel> TaskQueue { get; } = new ObservableCollection<TaskItemViewModel>();
        /// <summary>
        /// 定时任务集合
        /// </summary>
        public ObservableCollection<ScheduledTaskViewModel> ScheduledTasks { get; } = new ObservableCollection<ScheduledTaskViewModel>();
        /// <summary>
        /// 触发任务集合
        /// </summary>
        public ObservableCollection<TriggerTaskViewModel> TriggerTasks { get; } = new ObservableCollection<TriggerTaskViewModel>();
        /// <summary>
        /// 任务分类集合
        /// </summary>
        public ObservableCollection<TaskCategoryViewModel> TaskCategories { get; } = new ObservableCollection<TaskCategoryViewModel>();
        /// <summary>
        /// 已保存任务集合
        /// </summary>
        public ObservableCollection<SavedTaskViewModel> SavedTasks { get; } = new ObservableCollection<SavedTaskViewModel>();

        private TaskCategoryViewModel _selectedCategory;
        /// <summary>
        /// 当前选中的任务分类
        /// </summary>
        public TaskCategoryViewModel SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                _selectedCategory = value;
                OnPropertyChanged(nameof(SelectedCategory));
                OnPropertyChanged(nameof(FilteredSavedTasks));
            }
        }

        private string _filteredCategoryName;
        /// <summary>
        /// 当前筛选的分类名
        /// </summary>
        public string FilteredCategoryName
        {
            get => _filteredCategoryName;
            set
            {
                _filteredCategoryName = value;
                OnPropertyChanged(nameof(FilteredCategoryName));
                OnPropertyChanged(nameof(FilteredSavedTasks));
            }
        }

        /// <summary>
        /// 过滤后的已保存任务
        /// </summary>
        public IEnumerable<SavedTaskViewModel> FilteredSavedTasks
        {
            get
            {
                if (string.IsNullOrEmpty(FilteredCategoryName))
                    return SavedTasks;
                return SavedTasks.Where(t => t.CategoryName == FilteredCategoryName);
            }
        }

        private int _pageCounter = 1;
        private System.Timers.Timer _schedulerTimer;
        private System.Timers.Timer _countdownTimer;
        private System.Timers.Timer _autoSaveTimer;
        private bool _isProcessingQueue = false;
        private bool _needsSave = false;

        private 远程指令配置数据 _远程指令配置 = new 远程指令配置数据();
        public 远程指令配置数据 远程指令配置
        {
            get => _远程指令配置;
            set
            {
                _远程指令配置 = value;
                OnPropertyChanged(nameof(远程指令配置));
                更新远程指令状态文本();
                _needsSave = true;
            }
        }

        private string _远程指令状态文本 = "已停止";
        public string 远程指令状态文本
        {
            get => _远程指令状态文本;
            set
            {
                _远程指令状态文本 = value;
                OnPropertyChanged(nameof(远程指令状态文本));
            }
        }

        public void 更新远程指令状态文本()
        {
            if (_远程指令配置.是否启用 && 远程指令服务器.是否运行中)
                远程指令状态文本 = $"运行中 (端口:{_远程指令配置.端口号})";
            else if (_远程指令配置.是否启用)
                远程指令状态文本 = "启动失败";
            else
                远程指令状态文本 = "已停止";
        }

        public void 应用远程指令配置()
        {
            if (_远程指令配置.是否启用)
            {
                if (远程指令服务器.是否运行中)
                {
                    远程指令服务器.停止();
                }
                if (远程指令服务器.启动(_远程指令配置.端口号, _远程指令配置.认证令牌))
                {
                    远程指令状态文本 = $"运行中 (端口:{_远程指令配置.端口号})";
                }
                else
                {
                    远程指令状态文本 = "启动失败（端口可能被占用）";
                }
            }
            else
            {
                远程指令服务器.停止();
                远程指令状态文本 = "已停止";
            }
            _needsSave = true;
        }

        private readonly IFileService _fileService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fileService">文件服务（可选）</param>
        public MainWindowViewModel(IFileService fileService = null)
        {
            _fileService = fileService ?? new FileService();

            任务控制管理器.实例.插队请求 += 处理插队请求;

            任务控制管理器.实例.脚本执行开始 += (脚本) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var task = TaskQueue.FirstOrDefault(t => t.Status == TaskConstants.Status.运行中);
                    if (task != null)
                        task.Status = TaskConstants.Status.运行中;
                });
            };

            任务控制管理器.实例.脚本执行完成 += (脚本, 成功, 错误信息) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var task = TaskQueue.FirstOrDefault(t => t.Status == TaskConstants.Status.运行中);
                    if (task != null)
                    {
                        if (成功)
                        {
                            task.Status = TaskConstants.Status.完成;

                            if (task.Page != null && 脚本.HWnd != IntPtr.Zero)
                            {
                                string newWindowTitle = 窗口处理器.获取窗口标题(脚本.HWnd);
                                if (!string.IsNullOrEmpty(newWindowTitle))
                                {
                                    string newProcessName = "未知";
                                    string newAppPath = null;
                                    try
                                    {
                                        uint processId = 0;
                                        窗口处理器.GetWindowThreadProcessId(脚本.HWnd, out processId);
                                        var process = Process.GetProcessById((int)processId);
                                        newProcessName = process.ProcessName;
                                        newAppPath = process.MainModule?.FileName;
                                    }
                                    catch { }
                                    task.Page.HWnd = 脚本.HWnd;
                                    task.Page.WindowTitle = newWindowTitle;
                                    task.Page.ProcessName = newProcessName;
                                    task.Page.ApplicationPath = newAppPath;
                                }
                            }
                        }
                        else
                        {
                            task.Status = TaskConstants.Status.失败;
                        }

                        TaskQueue.Remove(task);
                        if (task.Page != null)
                            task.Page.IsRunning = false;
                    }

                    ProcessQueue();
                });
            };

            InitializeTimers();

            try
            {
                LoadUserData();
            }
            catch
            {
                // 用户数据异常时清空数据文件，避免每次启动都崩溃
                DataPersistenceService.ClearUserData();
                LoadUserData();
            }
        }

        /// <summary>
        /// 切换到任务管理模式
        /// </summary>
        public void SwitchToTaskManagement()
        {
            IsTaskManagementMode = true;
        }

        /// <summary>
        /// 切换到运行状态模式
        /// </summary>
        public void SwitchToRunningStatus()
        {
            IsTaskManagementMode = false;
        }

        /// <summary>
        /// 切换到智能化页面
        /// </summary>
        public void SwitchToAIPage()
        {
            SelectedPage = null;
            IsAIPage = true;
        }

        /// <summary>
        /// 初始化计时器
        /// </summary>
        private void InitializeTimers()
        {
            _schedulerTimer = new System.Timers.Timer(1000);
            _schedulerTimer.Elapsed += CheckScheduledTasks;
            _schedulerTimer.Start();

            _countdownTimer = new System.Timers.Timer(100);
            _countdownTimer.Elapsed += UpdateCountdowns;
            _countdownTimer.Start();

            _autoSaveTimer = new System.Timers.Timer(5000);
            _autoSaveTimer.Elapsed += AutoSaveUserData;
            _autoSaveTimer.Start();
        }

        /// <summary>
        /// 自动保存用户数据
        /// </summary>
        private void AutoSaveUserData(object sender, ElapsedEventArgs e)
        {
            if (_needsSave)
            {
                SaveUserData();
                _needsSave = false;
            }
        }

        /// <summary>
        /// 更新倒计时与节点状态显示（每0.1秒） - 使用统一的 DateTime 计时，替代计数器增量方式
        /// </summary>
        private void UpdateCountdowns(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var task in ScheduledTasks)
                {
                    task.UpdateCountdown();
                }

                // 刷新所有脚本页面中正在运行的节点的状态显示（显示实时耗时）
                foreach (var page in ScriptPages)
                {
                    foreach (var node in page.Nodes)
                    {
                        node.刷新状态显示();
                    }
                }
            });
        }

        /// <summary>
        /// 加载用户数据
        /// </summary>
        private void LoadUserData()
        {
            UserData userData = DataPersistenceService.LoadUserData();

            // 加载脚本页面
            foreach (var pageData in userData.ScriptPages)
            {
                try
                {
                    var newPage = new ScriptPageViewModel(pageData.Name, this);
                    newPage.ScriptSaved += OnScriptSaved;
                    newPage.ScriptRun += OnScriptRun;
                    newPage.BranchScriptRun += OnBranchScriptRun;
                    newPage.ScriptRunFromNode += OnScriptRunFromNode;
                    newPage.RunSingleNodeRequested += OnRunSingleNode;
                    newPage.StopAllTasksRequested += StopAllTasksForPage;
                    newPage.SetWindowBinding(pageData.ProcessName, pageData.WindowTitle, pageData.ApplicationPath, pageData.Remark);
                    
                    if (!string.IsNullOrEmpty(pageData.SavePath) && _fileService.Exists(pageData.SavePath))
                    {
                        newPage.CurrentSavePath = pageData.SavePath;
                        string scriptContent = _fileService.ReadAllText(pageData.SavePath);
                        var script = new 自动任务脚本(IntPtr.Zero, scriptContent);
                        foreach (var node in script.节点列表)
                        {
                            newPage.AddNode(node);
                        }
                    }
                    
                    ScriptPages.Add(newPage);
                    
                    if (!string.IsNullOrEmpty(pageData.SavePath))
                    {
                        _pageCounter = Math.Max(_pageCounter, int.TryParse(System.Text.RegularExpressions.Regex.Match(pageData.Name, @"\d+").Value, out int num) ? num + 1 : _pageCounter);
                    }

                    if (!string.IsNullOrEmpty(pageData.SavePath) && _fileService.Exists(pageData.SavePath))
                    {
                        AddSavedTask(newPage.PageName, pageData.SavePath, TaskConstants.默认分类名称);
                    }
                }
                catch { }
            }

            // 加载任务分类
            foreach (var categoryData in userData.TaskCategories)
            {
                加载分类数据(categoryData, null);
            }

            // 如果没有分类，添加默认的"未分类"
            if (TaskCategories.Count == 0)
            {
                TaskCategories.Add(new TaskCategoryViewModel { CategoryName = TaskConstants.默认分类名称, CategoryColor = TaskConstants.默认分类颜色 });
            }

            // 根据分类的 TaskPaths 修正已存在任务的分类（修复：AddSavedTask 的去重逻辑会跳过已在脚本页面循环中以"未分类"添加的任务，导致正确的分类名丢失）
            foreach (var categoryData in 扁平化分类数据(userData.TaskCategories))
            {
                foreach (var taskPath in categoryData.TaskPaths)
                {
                    var existingTask = SavedTasks.FirstOrDefault(t => t.ScriptPath == taskPath);
                    if (existingTask != null)
                    {
                        existingTask.CategoryName = categoryData.CategoryName;
                        existingTask.CategoryColor = GetCategoryColor(categoryData.CategoryName);
                    }
                }
            }

            // 加载定时任务
            foreach (var taskData in userData.ScheduledTasks)
            {
                try
                {
                    if (_fileService.Exists(taskData.ScriptPath))
                    {
                        var task = new ScheduledTaskViewModel
                        {
                            TaskName = taskData.TaskName,
                            ScriptPath = taskData.ScriptPath,
                            IsEnabled = taskData.IsEnabled,
                            CategoryName = taskData.CategoryName,
                            当前调度模式 = taskData.调度模式 ?? TaskConstants.调度模式.绝对时间,
                            倒计时总秒数 = taskData.倒计时总秒数 > 0 ? taskData.倒计时总秒数 : 300,
                            重复模式 = taskData.重复模式 ?? TaskConstants.重复周期.每天,
                            循环间隔秒数 = taskData.循环间隔秒数 > 0 ? taskData.循环间隔秒数 : 1800
                        };

                        if (TimeSpan.TryParse(taskData.每日重复时间, out var repeatTime))
                            task.每日重复时间 = repeatTime;

                        if (DateTime.TryParse(taskData.ScheduledDateTime, out DateTime scheduledTime))
                        {
                            if (task.当前调度模式 == TaskConstants.调度模式.绝对时间)
                            {
                                if (scheduledTime > DateTime.Now)
                                    task.ScheduledDateTime = scheduledTime;
                                else
                                {
                                    task.ScheduledDateTime = DateTime.Now.AddMinutes(5);
                                    task.IsEnabled = false;
                                }
                            }
                            else
                            {
                                task.重新计算下次触发时间();
                            }
                        }
                        else
                        {
                            task.ScheduledDateTime = DateTime.Now.AddMinutes(5);
                        }

                        task.CategoryColor = GetCategoryColor(task.CategoryName);
                        task.DeleteRequested += RemoveScheduledTask;
                        ScheduledTasks.Add(task);
                        AddSavedTask(task.TaskName, task.ScriptPath, task.CategoryName);
                    }
                }
                catch { }
            }

            // 加载触发任务
            foreach (var taskData in userData.TriggerTasks)
            {
                try
                {
                    if (_fileService.Exists(taskData.ScriptPath))
                    {
                        var triggerType = taskData.TriggerType;
                        if (triggerType == "窗口出现")
                        {
                            triggerType = TaskConstants.TriggerType.指令;
                        }

                        var task = new TriggerTaskViewModel
                        {
                            TaskName = taskData.TaskName,
                            ScriptPath = taskData.ScriptPath,
                            TriggerType = triggerType,
                            DelaySeconds = taskData.DelaySeconds,
                            IsEnabled = taskData.IsEnabled,
                            CategoryName = taskData.CategoryName
                        };
                        
                        task.CategoryColor = GetCategoryColor(task.CategoryName);
                        task.DeleteRequested += RemoveTriggerTask;
                        TriggerTasks.Add(task);
                        AddSavedTask(task.TaskName, task.ScriptPath, task.CategoryName);
                    }
                }
                catch { }
            }

            _远程指令配置 = userData.远程指令配置 ?? new 远程指令配置数据();
            更新远程指令状态文本();
            // 注意：远程指令服务器的启动不在此处，由 App.OnStartup 统一处理
        }

        /// <summary>
        /// 获取分类的颜色（递归搜索所有分类和子分类）
        /// </summary>
        private string GetCategoryColor(string categoryName)
        {
            foreach (var c in TaskCategories)
            {
                var found = 查找分类颜色(c, categoryName);
                if (found != null) return found;
            }
            return TaskConstants.默认分类颜色;
        }

        private string 查找分类颜色(TaskCategoryViewModel category, string categoryName)
        {
            if (category.CategoryName == categoryName)
                return category.CategoryColor;
            foreach (var sub in category.SubCategories)
            {
                var found = 查找分类颜色(sub, categoryName);
                if (found != null) return found;
            }
            return null;
        }

        private void 加载分类数据(TaskCategoryData data, TaskCategoryViewModel parent)
        {
            var category = new TaskCategoryViewModel
            {
                CategoryName = data.CategoryName,
                CategoryColor = data.CategoryColor,
                AI规则 = data.AI规则 ?? "",
                ParentCategory = parent
            };
            foreach (var taskPath in data.TaskPaths)
            {
                category.TaskPaths.Add(taskPath);
                if (_fileService.Exists(taskPath))
                {
                    string taskName = Path.GetFileNameWithoutExtension(taskPath);
                    AddSavedTask(taskName, taskPath, data.CategoryName);
                }
            }
            if (parent != null)
            {
                parent.SubCategories.Add(category);
            }
            else
            {
                TaskCategories.Add(category);
            }
            foreach (var subData in data.SubCategories)
            {
                加载分类数据(subData, category);
            }
        }

        private TaskCategoryData 保存分类数据(TaskCategoryViewModel category)
        {
            var categoryName = category.CategoryName;
            return new TaskCategoryData
            {
                CategoryName = category.CategoryName,
                CategoryColor = category.CategoryColor,
                AI规则 = category.AI规则,
                TaskPaths = SavedTasks.Where(t => t.CategoryName == categoryName).Select(t => t.ScriptPath).ToList(),
                SubCategories = category.SubCategories.Select(保存分类数据).ToList()
            };
        }

        private List<TaskCategoryData> 扁平化分类数据(List<TaskCategoryData> categories)
        {
            var result = new List<TaskCategoryData>();
            foreach (var cat in categories)
            {
                result.Add(cat);
                result.AddRange(扁平化分类数据(cat.SubCategories));
            }
            return result;
        }

        /// <summary>
        /// 添加已保存任务
        /// </summary>
        private void AddSavedTask(string taskName, string scriptPath, string categoryName)
        {
            if (SavedTasks.All(t => t.ScriptPath != scriptPath))
            {
                var savedTask = new SavedTaskViewModel
                {
                    TaskName = taskName,
                    ScriptPath = scriptPath,
                    CategoryName = categoryName,
                    CategoryColor = GetCategoryColor(categoryName)
                };
                savedTask.AddToQueueRequested += AddSavedTaskToQueue;
                savedTask.DeleteRequested += RemoveSavedTask;
                savedTask.EditRequested += OpenSavedTask;
                SavedTasks.Add(savedTask);
            }
        }

        /// <summary>
        /// 移除已保存任务
        /// </summary>
        private void RemoveSavedTask(SavedTaskViewModel task)
        {
            SavedTasks.Remove(task);
            var scheduledTask = ScheduledTasks.FirstOrDefault(t => t.ScriptPath == task.ScriptPath);
            if (scheduledTask != null)
            {
                ScheduledTasks.Remove(scheduledTask);
            }
            var triggerTask = TriggerTasks.FirstOrDefault(t => t.ScriptPath == task.ScriptPath);
            if (triggerTask != null)
            {
                TriggerTasks.Remove(triggerTask);
            }
            _needsSave = true;
        }

        /// <summary>
        /// 打开已保存任务进行编辑
        /// </summary>
        private void OpenSavedTask(SavedTaskViewModel task)
        {
            OpenScriptFromPath(task.ScriptPath);
        }

        /// <summary>
        /// 将已保存任务添加到队列
        /// </summary>
        private void AddSavedTaskToQueue(SavedTaskViewModel task)
        {
            AddTaskToQueue(task.TaskName, TaskConstants.TaskType.手动添加, task.ScriptPath);
        }

        /// <summary>
        /// 保存用户数据
        /// </summary>
        public void SaveUserData()
        {
            var userData = new UserData();

            // 保存脚本页面
            foreach (var page in ScriptPages)
            {
                userData.ScriptPages.Add(new ScriptPageData
                {
                    Name = page.PageName,
                    SavePath = page.CurrentSavePath,
                    WindowTitle = page.WindowTitle,
                    ProcessName = page.ProcessName,
                    ApplicationPath = page.ApplicationPath,
                    Remark = page.Remark
                });
            }

            // 保存任务分类（递归处理子分类）
            foreach (var category in TaskCategories)
            {
                userData.TaskCategories.Add(保存分类数据(category));
            }

            // 保存定时任务
            foreach (var task in ScheduledTasks)
            {
                userData.ScheduledTasks.Add(new ScheduledTaskData
                {
                    TaskName = task.TaskName,
                    ScriptPath = task.ScriptPath,
                    ScheduledDateTime = task.ScheduledDateTime.ToString("o"),
                    IsEnabled = task.IsEnabled,
                    CategoryName = task.CategoryName,
                    调度模式 = task.当前调度模式,
                    倒计时总秒数 = task.倒计时总秒数,
                    重复模式 = task.重复模式,
                    每日重复时间 = task.每日重复时间.ToString(),
                    循环间隔秒数 = task.循环间隔秒数
                });
            }

            // 保存触发任务
            foreach (var task in TriggerTasks)
            {
                userData.TriggerTasks.Add(new TriggerTaskData
                {
                    TaskName = task.TaskName,
                    ScriptPath = task.ScriptPath,
                    TriggerType = task.TriggerType,
                    DelaySeconds = task.DelaySeconds,
                    IsEnabled = task.IsEnabled,
                    CategoryName = task.CategoryName
                });
            }

            userData.远程指令配置 = _远程指令配置;

            DataPersistenceService.SaveUserData(userData);
        }

        /// <summary>
        /// 检查定时任务是否到期
        /// </summary>
        private void CheckScheduledTasks(object sender, ElapsedEventArgs e)
        {
            DateTime now = DateTime.Now;
            foreach (var task in ScheduledTasks.ToArray())
            {
                if (!task.IsEnabled) continue;

                if (task.ScheduledDateTime <= now)
                {
                    AddTaskToQueue(task.TaskName, TaskConstants.TaskType.定时任务, task.ScriptPath);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        switch (task.当前调度模式)
                        {
                            case TaskConstants.调度模式.绝对时间:
                            case TaskConstants.调度模式.倒计时:
                                task.IsEnabled = false;
                                break;
                            case TaskConstants.调度模式.重复:
                                task.重新计算下次触发时间();
                                break;
                            case TaskConstants.调度模式.循环:
                                task.ScheduledDateTime = DateTime.Now.AddSeconds(task.循环间隔秒数);
                                break;
                        }
                    });
                }
            }
        }


        /// <summary>
        /// 添加任务到执行队列 - 添加后自动触发队列处理
        /// </summary>
        private void AddTaskToQueue(string name, string type, string scriptPath)
        {
            try
            {
                string scriptContent = _fileService.ReadAllText(scriptPath);
                var script = new 自动任务脚本(IntPtr.Zero, scriptContent);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var taskItem = new TaskItemViewModel(name, type, script);
                    taskItem.DeleteRequested += RemoveTaskFromQueue;
                    TaskQueue.Add(taskItem);
                    // 添加任务后自动触发队列处理，按FIFO顺序依次执行
                    ProcessQueue();
                });
            }
            catch { }
        }

        /// <summary>
        /// 通过远程指令触发指定的触发任务
        /// </summary>
        public void 触发指令任务(string 任务名)
        {
            任务控制管理器.实例.触发命名任务(任务名);
        }

        /// <summary>
        /// 直接添加任务到队列（通过文件对话框）- 添加后自动触发队列处理
        /// </summary>
        public void AddTaskToQueueDirectly()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "脚本文件 (*.script)|*.script|所有文件 (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string scriptContent = _fileService.ReadAllText(dialog.FileName);
                    var script = new 自动任务脚本(IntPtr.Zero, scriptContent);
                    
                    var taskItem = new TaskItemViewModel(Path.GetFileNameWithoutExtension(dialog.FileName), TaskConstants.TaskType.手动添加, script);
                    taskItem.DeleteRequested += RemoveTaskFromQueue;
                    TaskQueue.Add(taskItem);
                    // 添加任务后自动触发队列处理，按FIFO顺序依次执行
                    ProcessQueue();
                }
                catch { }
            }
        }

        private void 处理插队请求(string 脚本路径)
        {
            try
            {
                if (!_fileService.Exists(脚本路径)) return;

                string 内容 = _fileService.ReadAllText(脚本路径);
                var 脚本 = new 自动任务脚本(IntPtr.Zero, 内容);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var taskItem = new TaskItemViewModel(
                        System.IO.Path.GetFileNameWithoutExtension(脚本路径),
                        TaskConstants.TaskType.脚本运行, 脚本);
                    taskItem.DeleteRequested += RemoveTaskFromQueue;
                    taskItem.Status = TaskConstants.Status.运行中;
                    TaskQueue.Add(taskItem);
                });

                任务控制管理器.实例.优先入队(脚本);
            }
            catch (Exception ex)
            {
                通知工具.错误弹窗($"插队失败: {ex.Message}");
            }
        }

        private void OnBranchScriptRun(ScriptPageViewModel page)
        {
            try
            {
                IntPtr targetHwnd = page.HWnd;
                if (targetHwnd == IntPtr.Zero && !string.IsNullOrEmpty(page.ProcessName))
                {
                    targetHwnd = WindowFinder.FindWindowByProcessName(page.ProcessName);
                }
                if (targetHwnd == IntPtr.Zero && !string.IsNullOrEmpty(page.WindowTitle))
                {
                    targetHwnd = WindowFinder.FindWindowByTitle(page.WindowTitle);
                }

                var script = new 自动任务脚本(targetHwnd);
                script.绑定进程名 = page.ProcessName;
                script.绑定窗口标题 = page.WindowTitle;
                script.是否为支线脚本 = true;
                foreach (var node in page.Nodes)
                {
                    script.节点列表.Add(node);
                }

                page.IsRunning = true;

                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        任务控制管理器.实例.重置取消();
                        script.执行();
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                           通知工具.错误弹窗($"支线脚本执行出错: {ex.Message}");
                        });
                    }
                    finally
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            page.IsRunning = false;
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                通知工具.错误弹窗($"启动支线脚本失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从队列中移除任务，若任务正在运行则先取消脚本执行
        /// </summary>
        private void RemoveTaskFromQueue(TaskItemViewModel task)
        {
            if (task.Status == TaskConstants.Status.运行中)
            {
                任务控制管理器.实例.取消();
            }
            TaskQueue.Remove(task);
        }

        /// <summary>
        /// 一键停止当前页面的所有关联任务 - 取消正在运行的脚本并移除所有关联任务
        /// </summary>
        public void StopAllTasksForPage(ScriptPageViewModel page)
        {
            if (page == null) return;

            var pageTasks = TaskQueue.Where(t => t.Page == page).ToList();
            if (pageTasks.Count == 0) return;

            // 若有运行中的任务，取消脚本执行
            if (pageTasks.Any(t => t.Status == TaskConstants.Status.运行中))
            {
                任务控制管理器.实例.取消();
            }

            // 移除所有关联任务
            foreach (var task in pageTasks)
            {
                TaskQueue.Remove(task);
            }

            // 恢复页面运行状态
            page.IsRunning = false;
        }

        /// <summary>
        /// 处理任务队列 - 按FIFO顺序依次执行队列中的所有任务
        /// </summary>
        private void ProcessQueue()
        {
            if (_isProcessingQueue) return;
            if (TaskQueue.Count == 0) return;

            var firstTask = TaskQueue.FirstOrDefault(t => t.Status == TaskConstants.Status.队列中 || t.Status == TaskConstants.Status.等待中);
            if (firstTask == null) return;

            _isProcessingQueue = true;
            firstTask.Status = TaskConstants.Status.运行中;

            try
            {
                任务控制管理器.实例.添加到队列(firstTask.Script, firstTask.起始节点索引);
            }
            finally
            {
                _isProcessingQueue = false;
            }
        }

        /// <summary>
        /// 显示定时任务对话框
        /// </summary>
        public void ShowScheduledTaskDialog()
        {
            var vm = new ScheduledTaskViewModel();
            vm.SaveRequested += AddScheduledTask;
            
            var dialog = new Dialogs.ScheduledTaskDialog(vm);
            dialog.ShowDialog();
        }

        /// <summary>
        /// 显示编辑定时任务对话框
        /// </summary>
        public void ShowScheduledTaskEditDialog(ScheduledTaskViewModel existingVm)
        {
            existingVm.SaveRequested += OnScheduledTaskEdited;

            var dialog = new Dialogs.ScheduledTaskDialog(existingVm, 编辑模式: true);
            dialog.ShowDialog();
        }

        private void OnScheduledTaskEdited(ScheduledTaskViewModel task)
        {
            task.CategoryName = task.CategoryName ?? TaskConstants.默认分类名称;
            task.CategoryColor = GetCategoryColor(task.CategoryName);
            _needsSave = true;
        }

        /// <summary>
        /// 添加定时任务
        /// </summary>
        private void AddScheduledTask(ScheduledTaskViewModel task)
        {
            task.CategoryName = task.CategoryName ?? TaskConstants.默认分类名称;
            task.CategoryColor = GetCategoryColor(task.CategoryName);
            task.DeleteRequested += RemoveScheduledTask;
            ScheduledTasks.Add(task);
            AddSavedTask(task.TaskName, task.ScriptPath, task.CategoryName);
            _needsSave = true;
        }

        /// <summary>
        /// 移除定时任务
        /// </summary>
        private void RemoveScheduledTask(ScheduledTaskViewModel task)
        {
            ScheduledTasks.Remove(task);
            var savedTask = SavedTasks.FirstOrDefault(t => t.ScriptPath == task.ScriptPath);
            if (savedTask != null && !TriggerTasks.Any(t => t.ScriptPath == task.ScriptPath))
            {
                SavedTasks.Remove(savedTask);
            }
            _needsSave = true;
        }

        /// <summary>
        /// 显示触发任务对话框
        /// </summary>
        public void ShowTriggerTaskDialog()
        {
            var vm = new TriggerTaskViewModel();
            vm.SaveRequested += AddTriggerTask;
            
            var dialog = new Dialogs.TriggerTaskDialog(vm);
            dialog.ShowDialog();
        }

        /// <summary>
        /// 添加触发任务
        /// </summary>
        private void AddTriggerTask(TriggerTaskViewModel task)
        {
            task.CategoryName = task.CategoryName ?? "未分类";
            task.CategoryColor = GetCategoryColor(task.CategoryName);
            task.DeleteRequested += RemoveTriggerTask;
            TriggerTasks.Add(task);
            AddSavedTask(task.TaskName, task.ScriptPath, task.CategoryName);
            _needsSave = true;
        }

        /// <summary>
        /// 移除触发任务
        /// </summary>
        private void RemoveTriggerTask(TriggerTaskViewModel task)
        {
            TriggerTasks.Remove(task);
            var savedTask = SavedTasks.FirstOrDefault(t => t.ScriptPath == task.ScriptPath);
            if (savedTask != null && !ScheduledTasks.Any(t => t.ScriptPath == task.ScriptPath))
            {
                SavedTasks.Remove(savedTask);
            }
            _needsSave = true;
        }

        /// <summary>
        /// 添加任务分类
        /// </summary>
        public void AddCategory()
        {
            string categoryName = 通知工具.输入弹窗("请输入分类名称:", "添加分类", "新分类");
            if (!string.IsNullOrEmpty(categoryName) && !TaskCategories.Any(c => c.CategoryName == categoryName))
            {
                var colors = new[] { "#FFE91E63", "#FF9C27B0", "#FF673AB7", "#FF3F51B5", "#FF2196F3", "#FF03A9F4", "#FF00BCD4", "#FF009688" };
                var random = new Random();
                TaskCategories.Add(new TaskCategoryViewModel
                {
                    CategoryName = categoryName,
                    CategoryColor = colors[random.Next(colors.Length)]
                });
                _needsSave = true;
            }
        }

        /// <summary>
        /// 编辑分类AI规则
        /// </summary>
        public void EditCategoryAIRule(TaskCategoryViewModel category)
        {
            if (category == null) return;
            string currentRule = category.AI规则 ?? "";
            string newRule = 通知工具.输入弹窗(
                $"请输入{category.CategoryName}分类的AI使用规则（AI调用此分类下工具时将遵循此规则）：",
                $"编辑分类AI规则 - {category.CategoryName}",
                currentRule);
            if (!string.IsNullOrEmpty(newRule))
            {
                category.AI规则 = newRule;
                _needsSave = true;
            }
        }

        /// <summary>
        /// 移除任务分类（递归收集其所有子分类的脚本一并移到未分类）
        /// </summary>
        public void RemoveCategory(TaskCategoryViewModel category)
        {
            if (category == null || category.CategoryName == TaskConstants.默认分类名称)
                return;

            var allCategoryNames = 收集分类及子分类名(category);

            foreach (var savedTask in SavedTasks.Where(t => allCategoryNames.Contains(t.CategoryName)).ToList())
            {
                savedTask.CategoryName = TaskConstants.默认分类名称;
                savedTask.CategoryColor = GetCategoryColor(TaskConstants.默认分类名称);
            }

            foreach (var scheduledTask in ScheduledTasks.Where(t => allCategoryNames.Contains(t.CategoryName)).ToList())
            {
                scheduledTask.CategoryName = TaskConstants.默认分类名称;
                scheduledTask.CategoryColor = GetCategoryColor(TaskConstants.默认分类名称);
            }

            foreach (var triggerTask in TriggerTasks.Where(t => allCategoryNames.Contains(t.CategoryName)).ToList())
            {
                triggerTask.CategoryName = TaskConstants.默认分类名称;
                triggerTask.CategoryColor = GetCategoryColor(TaskConstants.默认分类名称);
            }

            if (category.ParentCategory != null)
            {
                category.ParentCategory.SubCategories.Remove(category);
            }
            else
            {
                TaskCategories.Remove(category);
            }

            if (SelectedCategory == category)
            {
                SelectedCategory = TaskCategories.FirstOrDefault();
            }
            if (FilteredCategoryName == category.CategoryName)
            {
                FilteredCategoryName = null;
            }
            _needsSave = true;
        }

        private HashSet<string> 收集分类及子分类名(TaskCategoryViewModel category)
        {
            var names = new HashSet<string> { category.CategoryName };
            foreach (var sub in category.SubCategories)
            {
                names.UnionWith(收集分类及子分类名(sub));
            }
            return names;
        }

        /// <summary>
        /// 将已保存任务移动到目标分类（支持子分类名）
        /// </summary>
        public void MoveSavedTaskToCategory(SavedTaskViewModel task, string targetCategoryName)
        {
            if (task == null || string.IsNullOrEmpty(targetCategoryName)) return;
            if (task.CategoryName == targetCategoryName) return;

            var oldCategoryName = task.CategoryName;
            task.CategoryName = targetCategoryName;
            task.CategoryColor = GetCategoryColor(targetCategoryName);

            var oldCategory = 查找分类ByName(oldCategoryName);
            if (oldCategory != null)
            {
                oldCategory.TaskPaths.Remove(task.ScriptPath);
            }

            var newCategory = 查找分类ByName(targetCategoryName);
            if (newCategory != null && !newCategory.TaskPaths.Contains(task.ScriptPath))
            {
                newCategory.TaskPaths.Add(task.ScriptPath);
            }

            var scheduledTask = ScheduledTasks.FirstOrDefault(t => t.ScriptPath == task.ScriptPath);
            if (scheduledTask != null)
            {
                scheduledTask.CategoryName = targetCategoryName;
                scheduledTask.CategoryColor = GetCategoryColor(targetCategoryName);
            }

            var triggerTask = TriggerTasks.FirstOrDefault(t => t.ScriptPath == task.ScriptPath);
            if (triggerTask != null)
            {
                triggerTask.CategoryName = targetCategoryName;
                triggerTask.CategoryColor = GetCategoryColor(targetCategoryName);
            }

            OnPropertyChanged(nameof(FilteredSavedTasks));
            _needsSave = true;
        }

        private TaskCategoryViewModel 查找分类ByName(string categoryName)
        {
            foreach (var c in TaskCategories)
            {
                var found = 递归查找分类(c, categoryName);
                if (found != null) return found;
            }
            return null;
        }

        private TaskCategoryViewModel 递归查找分类(TaskCategoryViewModel category, string categoryName)
        {
            if (category.CategoryName == categoryName)
                return category;
            foreach (var sub in category.SubCategories)
            {
                var found = 递归查找分类(sub, categoryName);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// 添加子分类
        /// </summary>
        public void AddSubCategory(TaskCategoryViewModel parent)
        {
            if (parent == null) return;
            string categoryName = 通知工具.输入弹窗("请输入子分类名称:", "添加子分类", "新子分类");
            if (!string.IsNullOrEmpty(categoryName) && !parent.SubCategories.Any(c => c.CategoryName == categoryName))
            {
                var colors = new[] { "#FFE91E63", "#FF9C27B0", "#FF673AB7", "#FF3F51B5", "#FF2196F3", "#FF03A9F4", "#FF00BCD4", "#FF009688" };
                var random = new Random();
                parent.SubCategories.Add(new TaskCategoryViewModel
                {
                    CategoryName = categoryName,
                    CategoryColor = colors[random.Next(colors.Length)],
                    ParentCategory = parent
                });
                _needsSave = true;
            }
        }

        /// <summary>
        /// 重命名分类
        /// </summary>
        public void RenameCategory(TaskCategoryViewModel category)
        {
            if (category == null || category.CategoryName == TaskConstants.默认分类名称) return;
            var oldName = category.CategoryName;
            string newName = 通知工具.输入弹窗("请输入新分类名称:", "重命名分类", oldName);
            if (string.IsNullOrEmpty(newName) || newName == oldName) return;

            var 同级列表 = category.ParentCategory?.SubCategories
                ?? (IEnumerable<TaskCategoryViewModel>)TaskCategories;
            if (同级列表.Any(c => c != category && c.CategoryName == newName))
            {
                通知工具.信息弹窗("该名称已存在，请使用其他名称");
                return;
            }

            category.CategoryName = newName;

            foreach (var task in SavedTasks.Where(t => t.CategoryName == oldName).ToList())
                task.CategoryName = newName;
            foreach (var task in ScheduledTasks.Where(t => t.CategoryName == oldName).ToList())
                task.CategoryName = newName;
            foreach (var task in TriggerTasks.Where(t => t.CategoryName == oldName).ToList())
                task.CategoryName = newName;

            if (FilteredCategoryName == oldName)
                FilteredCategoryName = newName;

            _needsSave = true;
        }

        /// <summary>
        /// 移除子分类
        /// </summary>
        public void RemoveSubCategory(TaskCategoryViewModel subCategory)
        {
            if (subCategory == null || subCategory.ParentCategory == null) return;

            var parent = subCategory.ParentCategory;

            foreach (var savedTask in SavedTasks.Where(t => t.CategoryName == subCategory.CategoryName).ToList())
            {
                savedTask.CategoryName = parent.CategoryName;
                savedTask.CategoryColor = GetCategoryColor(parent.CategoryName);
            }

            foreach (var scheduledTask in ScheduledTasks.Where(t => t.CategoryName == subCategory.CategoryName).ToList())
            {
                scheduledTask.CategoryName = parent.CategoryName;
                scheduledTask.CategoryColor = GetCategoryColor(parent.CategoryName);
            }

            foreach (var triggerTask in TriggerTasks.Where(t => t.CategoryName == subCategory.CategoryName).ToList())
            {
                triggerTask.CategoryName = parent.CategoryName;
                triggerTask.CategoryColor = GetCategoryColor(parent.CategoryName);
            }

            parent.SubCategories.Remove(subCategory);

            if (FilteredCategoryName == subCategory.CategoryName)
            {
                FilteredCategoryName = null;
            }
            _needsSave = true;
        }

        /// <summary>
        /// 将分类移动到目标父分类下
        /// </summary>
        public void MoveCategoryToParent(TaskCategoryViewModel category, TaskCategoryViewModel targetParent)
        {
            if (category == null || targetParent == null) return;
            if (category == targetParent) return;

            if (分类是祖先(targetParent, category)) return;

            if (category.ParentCategory != null)
            {
                category.ParentCategory.SubCategories.Remove(category);
            }
            else
            {
                TaskCategories.Remove(category);
            }

            targetParent.SubCategories.Add(category);
            category.ParentCategory = targetParent;
            _needsSave = true;
        }

        private bool 分类是祖先(TaskCategoryViewModel ancestor, TaskCategoryViewModel child)
        {
            var current = child;
            while (current != null)
            {
                if (current == ancestor) return true;
                current = current.ParentCategory;
            }
            return false;
        }

        /// <summary>
        /// 将子分类提升为顶级分类
        /// </summary>
        public void PromoteSubCategory(TaskCategoryViewModel subCategory)
        {
            if (subCategory == null || subCategory.ParentCategory == null) return;

            subCategory.ParentCategory.SubCategories.Remove(subCategory);
            subCategory.ParentCategory = null;
            TaskCategories.Add(subCategory);
            _needsSave = true;
        }

        /// <summary>
        /// 切换分类展开状态
        /// </summary>
        public void ToggleCategoryExpanded(TaskCategoryViewModel category)
        {
            if (category == null) return;
            if (category.SubCategories.Count > 0)
            {
                category.IsExpanded = !category.IsExpanded;
            }
        }

        /// <summary>
        /// 设置筛选分类
        /// </summary>
        public void SetFilterCategory(TaskCategoryViewModel category)
        {
            if (category == null)
            {
                FilteredCategoryName = null;
                return;
            }
            FilteredCategoryName = category.CategoryName;
        }

        /// <summary>
        /// 获取所有分类名（扁平遍历含子分类，子分类用"父分类名/子分类名"格式）
        /// </summary>
        public List<string> GetAllCategoryNames()
        {
            var names = new List<string>();
            foreach (var cat in TaskCategories)
            {
                收集分类名(cat, null, names);
            }
            return names;
        }

        private void 收集分类名(TaskCategoryViewModel category, string prefix, List<string> names)
        {
            var fullName = prefix == null ? category.CategoryName : $"{prefix}/{category.CategoryName}";
            names.Add(fullName);
            foreach (var sub in category.SubCategories)
            {
                收集分类名(sub, fullName, names);
            }
        }

        /// <summary>
        /// 添加新的脚本页面 - 检查未保存脚本名称，确保不重名
        /// </summary>
        public void AddNewPage()
        {
            // 生成不与其他未保存脚本重名的默认名称：检查所有未保存的脚本页（CurrentSavePath为空的）
            string newPageName;
            do
            {
                newPageName = $"脚本{_pageCounter++}";
            } while (ScriptPages.Any(p => string.IsNullOrEmpty(p.CurrentSavePath) && p.ScriptName == newPageName));

            var newPage = new ScriptPageViewModel(newPageName, this);
            newPage.ScriptSaved += OnScriptSaved;
            newPage.ScriptRun += OnScriptRun;
            newPage.BranchScriptRun += OnBranchScriptRun;
            newPage.ScriptRunFromNode += OnScriptRunFromNode;
            newPage.RunSingleNodeRequested += OnRunSingleNode;
            newPage.StopAllTasksRequested += StopAllTasksForPage;
            ScriptPages.Add(newPage);
            SelectedPage = newPage;
            _needsSave = true;
        }

        /// <summary>
        /// 脚本运行事件处理
        /// </summary>
        private void OnScriptRun(ScriptPageViewModel page)
        {
            try
            {
                // 优先使用页面已绑定的窗口句柄，否则通过进程名或窗口标题查找
                IntPtr targetHwnd = page.HWnd;
                if (targetHwnd == IntPtr.Zero && !string.IsNullOrEmpty(page.ProcessName))
                {
                    targetHwnd = WindowFinder.FindWindowByProcessName(page.ProcessName);
                }
                if (targetHwnd == IntPtr.Zero && !string.IsNullOrEmpty(page.WindowTitle))
                {
                    targetHwnd = WindowFinder.FindWindowByTitle(page.WindowTitle);
                }

                var script = new 自动任务脚本(targetHwnd);
                script.绑定进程名 = page.ProcessName;
                script.绑定窗口标题 = page.WindowTitle;
                foreach (var node in page.Nodes)
                {
                    script.节点列表.Add(node);
                }

                var taskItem = new TaskItemViewModel(page.ScriptName, TaskConstants.TaskType.脚本运行, script);
                taskItem.DeleteRequested += RemoveTaskFromQueue;
                taskItem.Page = page;
                TaskQueue.Add(taskItem);

                page.IsRunning = true;
                ProcessQueue();
            }
            catch (Exception ex)
            {
                通知工具.错误弹窗($"添加任务失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从指定节点运行脚本事件处理 - 将该脚本加入执行队列，并设置起始节点索引
        /// </summary>
        private void OnScriptRunFromNode(ScriptPageViewModel page, int nodeIndex)
        {
            try
            {
                // 优先使用页面已绑定的窗口句柄，否则通过进程名或窗口标题查找
                IntPtr targetHwnd = page.HWnd;
                if (targetHwnd == IntPtr.Zero && !string.IsNullOrEmpty(page.ProcessName))
                {
                    targetHwnd = WindowFinder.FindWindowByProcessName(page.ProcessName);
                }
                if (targetHwnd == IntPtr.Zero && !string.IsNullOrEmpty(page.WindowTitle))
                {
                    targetHwnd = WindowFinder.FindWindowByTitle(page.WindowTitle);
                }

                var script = new 自动任务脚本(targetHwnd);
                script.绑定进程名 = page.ProcessName;
                script.绑定窗口标题 = page.WindowTitle;
                foreach (var node in page.Nodes)
                {
                    script.节点列表.Add(node);
                }

                if (nodeIndex < 0 || nodeIndex >= script.节点列表.Count)
                {
                    bool confirmed = 通知工具.确认弹窗("节点越界，点击确认以从文件中刷新列表修复");
                    if (confirmed)
                    {
                        if (!string.IsNullOrEmpty(page.CurrentSavePath) && _fileService.Exists(page.CurrentSavePath))
                        {
                            RefreshPageFromFile(page, page.CurrentSavePath);
                        }
                    }
                    return;
                }

                var taskItem = new TaskItemViewModel(page.ScriptName, TaskConstants.TaskType.脚本运行, script);
                taskItem.DeleteRequested += RemoveTaskFromQueue;
                taskItem.Page = page;
                taskItem.起始节点索引 = nodeIndex;
                TaskQueue.Add(taskItem);

                page.IsRunning = true;
                ProcessQueue();
            }
            catch (Exception ex)
            {
                通知工具.错误弹窗($"添加任务失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行单个节点事件处理 - 创建仅包含该节点的脚本，加入执行队列
        /// </summary>
        private void OnRunSingleNode(ScriptPageViewModel page, 控制节点 node)
        {
            try
            {
                // 优先使用页面已绑定的窗口句柄，否则通过进程名或窗口标题查找
                IntPtr targetHwnd = page.HWnd;
                if (targetHwnd == IntPtr.Zero && !string.IsNullOrEmpty(page.ProcessName))
                {
                    targetHwnd = WindowFinder.FindWindowByProcessName(page.ProcessName);
                }
                if (targetHwnd == IntPtr.Zero && !string.IsNullOrEmpty(page.WindowTitle))
                {
                    targetHwnd = WindowFinder.FindWindowByTitle(page.WindowTitle);
                }

                var script = new 自动任务脚本(targetHwnd);
                script.绑定进程名 = page.ProcessName;
                script.绑定窗口标题 = page.WindowTitle;
                script.节点列表.Add(node);

                var taskItem = new TaskItemViewModel(page.ScriptName, TaskConstants.TaskType.脚本运行, script);
                taskItem.DeleteRequested += RemoveTaskFromQueue;
                taskItem.Page = page;
                TaskQueue.Add(taskItem);

                page.IsRunning = true;
                ProcessQueue();
            }
            catch (Exception ex)
            {
                通知工具.错误弹窗($"执行节点失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 脚本保存事件处理 - 将脚本路径添加到已保存任务列表，并标记需要保存用户数据
        /// </summary>
        private void OnScriptSaved(string scriptName, string scriptPath)
        {
            AddSavedTask(scriptName, scriptPath, TaskConstants.默认分类名称);
            _needsSave = true;
        }

        /// <summary>
        /// 关闭脚本页面 - 关闭前检查是否有队列中的关联任务，有关联任务则提示用户确认
        /// </summary>
        public void ClosePage(ScriptPageViewModel page)
        {
            if (page == null) return;

            // 检查该页面是否有任务在队列中
            var pageTasks = TaskQueue.Where(t => t.Page == page).ToList();
            if (pageTasks.Count > 0)
            {
                var result = 通知工具.确认弹窗("此脚本页面的任务正在队列中，关闭将停止它们。确认关闭？");
                if (!result)
                    return;

                StopAllTasksForPage(page);
            }

            int index = ScriptPages.IndexOf(page);
            ScriptPages.Remove(page);

            if (ScriptPages.Count > 0)
            {
                SelectedPage = ScriptPages[index > 0 ? index - 1 : 0];
            }
            _needsSave = true;
        }

        /// <summary>
        /// 保存当前页面
        /// </summary>
        public void SaveCurrentPage()
        {
            if (SelectedPage != null && !SelectedPage.IsRunning)
            {
                SelectedPage.SaveScript();
            }
        }

        /// <summary>
        /// 保存指定页面
        /// </summary>
        public void SavePage(ScriptPageViewModel page)
        {
            if (page != null && !page.IsRunning)
            {
                page.SaveScript();
            }
        }

        /// <summary>
        /// 从文件刷新脚本页面内容 - 重新加载脚本文件并更新页面
        /// </summary>
        /// <param name="page">要刷新的脚本页面</param>
        /// <param name="filePath">脚本文件路径</param>
        private void RefreshPageFromFile(ScriptPageViewModel page, string filePath)
        {
            string scriptContent = _fileService.ReadAllText(filePath);
            var script = new 自动任务脚本(IntPtr.Zero, scriptContent);

            page.Nodes.Clear();
            page.SetWindowBinding(script.绑定进程名, script.绑定窗口标题, remark: script.脚本备注);

            foreach (var node in script.节点列表)
            {
                page.AddNode(node);
            }
        }

        /// <summary>
        /// 从指定路径打开脚本 - 检查已保存脚本的路径是否重复，若重复则刷新对应标签页
        /// </summary>
        public void OpenScriptFromPath(string filePath)
        {
            try
            {
                // 检查已保存的脚本中是否已经存在相同路径的页面
                var existingPage = ScriptPages.FirstOrDefault(p =>
                    !string.IsNullOrEmpty(p.CurrentSavePath) &&
                    string.Equals(p.CurrentSavePath, filePath, StringComparison.OrdinalIgnoreCase));

                if (existingPage != null)
                {
                    // 路径重复，刷新对应标签页内容并选中
                    RefreshPageFromFile(existingPage, filePath);
                    SelectedPage = existingPage;
                    return;
                }

                string scriptContent = _fileService.ReadAllText(filePath);
                var script = new 自动任务脚本(IntPtr.Zero, scriptContent);
                
                var newPage = new ScriptPageViewModel(Path.GetFileNameWithoutExtension(filePath), this);
                newPage.ScriptSaved += OnScriptSaved;
                newPage.ScriptRun += OnScriptRun;
                newPage.BranchScriptRun += OnBranchScriptRun;
                newPage.ScriptRunFromNode += OnScriptRunFromNode;
                newPage.RunSingleNodeRequested += OnRunSingleNode;
                newPage.StopAllTasksRequested += StopAllTasksForPage;
                newPage.SetWindowBinding(script.绑定进程名, script.绑定窗口标题, remark: script.脚本备注);
                newPage.CurrentSavePath = filePath;
                
                foreach (var node in script.节点列表)
                {
                    newPage.AddNode(node);
                }
                
                ScriptPages.Add(newPage);
                SelectedPage = newPage;
            }
            catch (Exception ex)
            {
                通知工具.错误弹窗($"打开脚本失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 通过文件对话框打开脚本 - 检查已保存脚本的路径是否重复，若重复则刷新对应标签页
        /// </summary>
        public async System.Threading.Tasks.Task OpenScript()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "脚本文件 (*.script)|*.script|所有文件 (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // 检查已保存的脚本中是否已经存在相同路径的页面
                    var existingPage = ScriptPages.FirstOrDefault(p =>
                        !string.IsNullOrEmpty(p.CurrentSavePath) &&
                        string.Equals(p.CurrentSavePath, dialog.FileName, StringComparison.OrdinalIgnoreCase));

                    if (existingPage != null)
                    {
                        // 路径重复，刷新对应标签页内容并选中
                        await System.Threading.Tasks.Task.Run(() => RefreshPageFromFile(existingPage, dialog.FileName));
                        SelectedPage = existingPage;
                        return;
                    }

                    string scriptContent = await System.Threading.Tasks.Task.Run(() => _fileService.ReadAllText(dialog.FileName));
                    var script = await System.Threading.Tasks.Task.Run(() => new 自动任务脚本(IntPtr.Zero, scriptContent));
                    
                    var newPage = new ScriptPageViewModel(Path.GetFileNameWithoutExtension(dialog.FileName), this);
                    newPage.SetWindowBinding(script.绑定进程名, script.绑定窗口标题);
                    newPage.CurrentSavePath = dialog.FileName;
                    
                    foreach (var node in script.节点列表)
                    {
                        newPage.AddNode(node);
                    }
                    
                    ScriptPages.Add(newPage);
                    SelectedPage = newPage;
                }
                catch (Exception ex)
                {
                    通知工具.错误弹窗($"打开脚本失败: {ex.Message}");
                }
            }
        }

        private async void 触发AI经验总结()
        {
            if (!AI配置管理器.获取启用自主学习()) return;
            try
            {
                var 对话 = AI配置管理器.获取当前对话();
                if (对话 == null) return;
                var 总结AI配置 = AI配置管理器.获取全局配置();
                await AI使用经验管理器.触发经验总结(对话, 总结AI配置);
                AIPage?.重置新消息计数();
            }
            catch { }
        }

        /// <summary>
        /// 释放所有资源 - 停止并释放4个Timer
        /// </summary>
        public void Dispose()
        {
            _schedulerTimer?.Stop();
            _schedulerTimer?.Dispose();
            _countdownTimer?.Stop();
            _countdownTimer?.Dispose();
            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Dispose();
        }
    }
}
