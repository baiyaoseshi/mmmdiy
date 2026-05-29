using System;
using 淼喵妙神奇工具库.键鼠库.动作;

namespace 淼喵妙用户界面.ViewModels
{
    /// <summary>
    /// 任务项视图模型 - 表示执行队列中的一个任务，继承 ViewModelBase 以支持属性变更通知
    /// </summary>
    public class TaskItemViewModel : ViewModelBase
    {
        /// <summary>
        /// 任务唯一标识
        /// </summary>
        public Guid Id { get; }
        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 任务类型（手动添加、定时任务、触发任务等）
        /// </summary>
        public string Type { get; }
        /// <summary>
        /// 任务状态（队列中、等待中、运行中、完成、失败）
        /// </summary>
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }
        private string _status;
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; }
        /// <summary>
        /// 计划执行时间（如果是定时任务）
        /// </summary>
        public DateTime? ScheduledTime { get; }
        /// <summary>
        /// 要执行的脚本
        /// </summary>
        public 自动任务脚本 Script { get; }
        /// <summary>
        /// 关联的脚本页面（可选）- 用于执行完毕后同步窗口绑定信息
        /// </summary>
        public ScriptPageViewModel Page { get; set; }
        /// <summary>
        /// 起始节点索引，默认为 0。用于"从此运行"功能，执行时传给 script.执行(起始节点索引)
        /// </summary>
        public int 起始节点索引 = 0;

        /// <summary>
        /// 构造函数 - 创建一个新的任务项
        /// </summary>
        /// <param name="name">任务名称</param>
        /// <param name="type">任务类型</param>
        /// <param name="script">要执行的脚本</param>
        /// <param name="scheduledTime">计划执行时间（可选）</param>
        public TaskItemViewModel(string name, string type, 自动任务脚本 script, DateTime? scheduledTime = null)
        {
            Id = Guid.NewGuid();
            Name = name;
            Type = type;
            Script = script;
            CreatedTime = DateTime.Now;
            ScheduledTime = scheduledTime;
            _status = scheduledTime.HasValue ? TaskConstants.Status.等待中 : TaskConstants.Status.队列中;
        }

        /// <summary>
        /// 删除此任务
        /// </summary>
        public void Delete()
        {
            DeleteRequested?.Invoke(this);
        }

        /// <summary>
        /// 删除请求事件
        /// </summary>
        public event Action<TaskItemViewModel> DeleteRequested;
    }
}