using System.Collections.Generic;

namespace 淼喵妙用户界面.Data
{
    /// <summary>
    /// 用户数据类 - 存储应用程序的所有用户配置
    /// </summary>
    public class UserData
    {
        /// <summary>
        /// 脚本页面列表
        /// </summary>
        public List<ScriptPageData> ScriptPages { get; set; } = new List<ScriptPageData>();
        
        /// <summary>
        /// 任务分类列表
        /// </summary>
        public List<TaskCategoryData> TaskCategories { get; set; } = new List<TaskCategoryData>();
        
        /// <summary>
        /// 定时任务列表
        /// </summary>
        public List<ScheduledTaskData> ScheduledTasks { get; set; } = new List<ScheduledTaskData>();
        
        /// <summary>
        /// 触发任务列表
        /// </summary>
        public List<TriggerTaskData> TriggerTasks { get; set; } = new List<TriggerTaskData>();
        
        /// <summary>
        /// 窗口宽度
        /// </summary>
        public int? WindowWidth { get; set; }
        
        /// <summary>
        /// 窗口高度
        /// </summary>
        public int? WindowHeight { get; set; }
        
        /// <summary>
        /// 窗口左边位置
        /// </summary>
        public int? WindowLeft { get; set; }
        
        /// <summary>
        /// 窗口顶部位置
        /// </summary>
        public int? WindowTop { get; set; }

        /// <summary>
        /// 远程指令配置
        /// </summary>
        public 远程指令配置数据 远程指令配置 { get; set; } = new 远程指令配置数据();
    }

    /// <summary>
    /// 脚本页面数据 - 存储单个脚本页面的配置
    /// </summary>
    public class ScriptPageData
    {
        /// <summary>
        /// 页面名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 脚本保存路径
        /// </summary>
        public string SavePath { get; set; }
        
        /// <summary>
        /// 绑定的窗口标题
        /// </summary>
        public string WindowTitle { get; set; }
        
        /// <summary>
        /// 绑定的进程名称
        /// </summary>
        public string ProcessName { get; set; }
        
        /// <summary>
        /// 应用程序路径
        /// </summary>
        public string ApplicationPath { get; set; }
        
        /// <summary>
        /// 备注信息
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// 任务分类数据 - 存储任务分类信息
    /// </summary>
    public class TaskCategoryData
    {
        /// <summary>
        /// 分类名称
        /// </summary>
        public string CategoryName { get; set; }
        
        /// <summary>
        /// 分类颜色
        /// </summary>
        public string CategoryColor { get; set; }
        
        /// <summary>
        /// 该分类下的任务路径列表
        /// </summary>
        public List<string> TaskPaths { get; set; } = new List<string>();

        /// <summary>
        /// 该分类的AI使用规则
        /// </summary>
        public string AI规则 { get; set; } = "";

        /// <summary>
        /// 子分类列表
        /// </summary>
        public List<TaskCategoryData> SubCategories { get; set; } = new List<TaskCategoryData>();
    }

    /// <summary>
    /// 定时任务数据 - 存储定时任务配置
    /// </summary>
    public class ScheduledTaskData
    {
        public string TaskName { get; set; }
        public string ScriptPath { get; set; }
        public string ScheduledDateTime { get; set; }
        public bool IsEnabled { get; set; }
        public string CategoryName { get; set; }
        public string 调度模式 { get; set; } = "绝对时间";
        public int 倒计时总秒数 { get; set; } = 300;
        public string 重复模式 { get; set; } = "每天";
        public string 每日重复时间 { get; set; } = "08:00:00";
        public int 循环间隔秒数 { get; set; } = 1800;
    }

    /// <summary>
    /// 触发任务数据 - 存储触发任务配置
    /// </summary>
    public class TriggerTaskData
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName { get; set; }
        
        /// <summary>
        /// 脚本文件路径
        /// </summary>
        public string ScriptPath { get; set; }
        
        /// <summary>
        /// 触发类型
        /// </summary>
        public string TriggerType { get; set; }
        
        /// <summary>
        /// 触发窗口标题
        /// </summary>
        public string WindowTitle { get; set; }
        
        /// <summary>
        /// 延迟秒数
        /// </summary>
        public int DelaySeconds { get; set; }
        
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }
        
        /// <summary>
        /// 所属分类名称
        /// </summary>
        public string CategoryName { get; set; }
    }

    /// <summary>
    /// 远程指令配置数据 - 存储远程指令服务器的配置
    /// </summary>
    public class 远程指令配置数据
    {
        /// <summary>
        /// 是否启用远程指令监听
        /// </summary>
        public bool 是否启用 { get; set; } = false;

        /// <summary>
        /// 监听端口号
        /// </summary>
        public int 端口号 { get; set; } = 18888;

        /// <summary>
        /// 认证令牌（为空则不验证）
        /// </summary>
        public string 认证令牌 { get; set; } = "";
    }
}