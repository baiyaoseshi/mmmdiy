using System;
using System.Windows;
using 淼喵妙用户界面.Services;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙用户界面.ViewModels
{
    public class ScheduledTaskViewModel : ViewModelBase
    {
        public Guid Id { get; } = Guid.NewGuid();

        private string _taskName = "定时任务";
        public string TaskName
        {
            get => _taskName;
            set { _taskName = value; OnPropertyChanged(nameof(TaskName)); }
        }

        private DateTime _scheduledDateTime = DateTime.Now.AddMinutes(5);
        public DateTime ScheduledDateTime
        {
            get => _scheduledDateTime;
            set
            {
                _scheduledDateTime = value;
                OnPropertyChanged(nameof(ScheduledDateTime));
                OnPropertyChanged(nameof(执行时间文本));
                UpdateCountdown();
            }
        }

        public string 执行时间文本
        {
            get => _scheduledDateTime.ToString("HH:mm:ss");
            set
            {
                if (TimeSpan.TryParse(value, out var ts))
                {
                    var newDateTime = _scheduledDateTime.Date.Add(ts);
                    if (newDateTime != _scheduledDateTime)
                    {
                        _scheduledDateTime = newDateTime;
                        OnPropertyChanged(nameof(ScheduledDateTime));
                        OnPropertyChanged(nameof(执行时间文本));
                        UpdateCountdown();
                        重新计算下次触发时间();
                    }
                }
            }
        }

        private TimeSpan? _countdown;
        public TimeSpan? Countdown
        {
            get => _countdown;
            set { _countdown = value; OnPropertyChanged(nameof(Countdown)); }
        }

        private string _scriptPath;
        public string ScriptPath
        {
            get => _scriptPath;
            set { _scriptPath = value; OnPropertyChanged(nameof(ScriptPath)); }
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(nameof(IsEnabled)); }
        }

        private string _categoryName;
        public string CategoryName
        {
            get => _categoryName;
            set { _categoryName = value; OnPropertyChanged(nameof(CategoryName)); }
        }

        private string _categoryColor = TaskConstants.默认分类颜色;
        public string CategoryColor
        {
            get => _categoryColor;
            set { _categoryColor = value; OnPropertyChanged(nameof(CategoryColor)); }
        }

        private string _当前调度模式 = TaskConstants.调度模式.绝对时间;
        public string 当前调度模式
        {
            get => _当前调度模式;
            set
            {
                _当前调度模式 = value;
                OnPropertyChanged(nameof(当前调度模式));
                OnPropertyChanged(nameof(Is绝对时间));
                OnPropertyChanged(nameof(Is倒计时));
                OnPropertyChanged(nameof(Is重复));
                OnPropertyChanged(nameof(Is循环));
                OnPropertyChanged(nameof(调度描述));
                重新计算下次触发时间();
            }
        }

        public bool Is绝对时间 => 当前调度模式 == TaskConstants.调度模式.绝对时间;
        public bool Is倒计时 => 当前调度模式 == TaskConstants.调度模式.倒计时;
        public bool Is重复 => 当前调度模式 == TaskConstants.调度模式.重复;
        public bool Is循环 => 当前调度模式 == TaskConstants.调度模式.循环;

        private int _倒计时总秒数 = 300;
        public int 倒计时总秒数
        {
            get => _倒计时总秒数;
            set { _倒计时总秒数 = value; OnPropertyChanged(nameof(倒计时总秒数)); 重新计算下次触发时间(); }
        }

        private string _重复模式 = TaskConstants.重复周期.每天;
        public string 重复模式
        {
            get => _重复模式;
            set { _重复模式 = value; OnPropertyChanged(nameof(重复模式)); 重新计算下次触发时间(); }
        }

        private TimeSpan _每日重复时间 = new TimeSpan(8, 0, 0);
        public TimeSpan 每日重复时间
        {
            get => _每日重复时间;
            set { _每日重复时间 = value; OnPropertyChanged(nameof(每日重复时间)); OnPropertyChanged(nameof(每日重复时间文本)); 重新计算下次触发时间(); }
        }

        public string 每日重复时间文本
        {
            get => $"{(int)每日重复时间.TotalHours:D2}:{每日重复时间.Minutes:D2}:{每日重复时间.Seconds:D2}";
            set
            {
                if (TimeSpan.TryParse(value, out var ts))
                    每日重复时间 = ts;
            }
        }

        private int _循环间隔秒数 = 1800;
        public int 循环间隔秒数
        {
            get => _循环间隔秒数;
            set { _循环间隔秒数 = value; OnPropertyChanged(nameof(循环间隔秒数)); 重新计算下次触发时间(); }
        }

        private bool _编辑模式;
        public bool 编辑模式
        {
            get => _编辑模式;
            set { _编辑模式 = value; OnPropertyChanged(nameof(编辑模式)); }
        }

        public string 调度描述
        {
            get
            {
                switch (当前调度模式)
                {
                    case TaskConstants.调度模式.绝对时间:
                        return $"绝对 {ScheduledDateTime:MM-dd HH:mm:ss}";
                    case TaskConstants.调度模式.倒计时:
                        return $"倒计时 {格式化时间长度(倒计时总秒数)}";
                    case TaskConstants.调度模式.重复:
                        var repeatLabel = 重复模式 switch
                        {
                            TaskConstants.重复周期.每天 => "每天",
                            TaskConstants.重复周期.每周 => "每周",
                            TaskConstants.重复周期.每月 => "每月",
                            _ => 重复模式
                        };
                        return $"{repeatLabel} {每日重复时间文本}";
                    case TaskConstants.调度模式.循环:
                        return $"每{格式化时间长度(循环间隔秒数)}";
                    default:
                        return ScheduledDateTime.ToString("MM-dd HH:mm:ss");
                }
            }
        }

        public ScheduledTaskViewModel()
        {
            重新计算下次触发时间();
            UpdateCountdown();
        }

        public void UpdateCountdown()
        {
            TimeSpan remaining = ScheduledDateTime - DateTime.Now;
            Countdown = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public void 重新计算下次触发时间()
        {
            ScheduledDateTime = 当前调度模式 switch
            {
                TaskConstants.调度模式.绝对时间 => ScheduledDateTime,
                TaskConstants.调度模式.倒计时 => DateTime.Now.AddSeconds(倒计时总秒数),
                TaskConstants.调度模式.重复 => 计算下次重复时间(),
                TaskConstants.调度模式.循环 => DateTime.Now.AddSeconds(循环间隔秒数),
                _ => ScheduledDateTime
            };
            OnPropertyChanged(nameof(调度描述));
        }

        private DateTime 计算下次重复时间()
        {
            var today = DateTime.Today;
            var target = today.Add(每日重复时间);
            if (target > DateTime.Now) return target;

            return 重复模式 switch
            {
                TaskConstants.重复周期.每周 => target.AddDays(7),
                TaskConstants.重复周期.每月 => target.AddMonths(1),
                _ => target.AddDays(1)
            };
        }

        public void SelectScript()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "脚本文件 (*.script)|*.script|所有文件 (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                ScriptPath = dialog.FileName;
            }
        }

        public bool Save()
        {
            if (string.IsNullOrEmpty(ScriptPath))
            {
                通知工具.警告弹窗("请选择脚本文件");
                return false;
            }

            if (ScheduledDateTime <= DateTime.Now && 当前调度模式 != TaskConstants.调度模式.重复)
            {
                通知工具.警告弹窗("定时时间必须大于当前时间");
                return false;
            }

            重新计算下次触发时间();

            SaveRequested?.Invoke(this);
            if (!编辑模式)
            {
                通知工具.信息弹窗("定时任务已设置");
            }
            return true;
        }

        public void Delete()
        {
            DeleteRequested?.Invoke(this);
        }

        public event Action<ScheduledTaskViewModel> SaveRequested;
        public event Action<ScheduledTaskViewModel> DeleteRequested;

        private static string 格式化时间长度(int 总秒数)
        {
            if (总秒数 < 60) return $"{总秒数}秒";
            if (总秒数 < 3600) return $"{总秒数 / 60}分{总秒数 % 60}秒";
            var h = 总秒数 / 3600;
            var m = (总秒数 % 3600) / 60;
            var s = 总秒数 % 60;
            if (s > 0) return $"{h}时{m}分{s}秒";
            if (m > 0) return $"{h}时{m}分";
            return $"{h}时";
        }
    }
}
