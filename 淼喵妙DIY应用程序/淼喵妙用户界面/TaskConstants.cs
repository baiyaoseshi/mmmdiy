namespace 淼喵妙用户界面
{
    /// <summary>
    /// 任务相关的常量定义 - 避免散布在代码中的魔法字符串
    /// </summary>
    public static class TaskConstants
    {
        /// <summary>
        /// 任务状态常量
        /// </summary>
        public static class Status
        {
            /// <summary>队列中</summary>
            public const string 队列中 = "队列中";
            /// <summary>等待中（定时任务尚未到执行时间）</summary>
            public const string 等待中 = "等待中";
            /// <summary>运行中</summary>
            public const string 运行中 = "运行中";
            /// <summary>执行完成</summary>
            public const string 完成 = "完成";
            /// <summary>执行失败</summary>
            public const string 失败 = "失败";
        }

        /// <summary>
        /// 任务类型常量
        /// </summary>
        public static class TaskType
        {
            /// <summary>手动添加的任务</summary>
            public const string 手动添加 = "手动添加";
            /// <summary>定时任务</summary>
            public const string 定时任务 = "定时任务";
            /// <summary>触发任务（条件触发）</summary>
            public const string 触发任务 = "触发任务";
            /// <summary>脚本运行产生的任务</summary>
            public const string 脚本运行 = "脚本运行";
        }

        /// <summary>
        /// 触发类型常量
        /// </summary>
        public static class TriggerType
        {
            /// <summary>远程指令触发</summary>
            public const string 指令 = "指令";
        }

        /// <summary>
        /// 默认分类名称
        /// </summary>
        public const string 默认分类名称 = "未分类";

        /// <summary>
        /// 调度模式常量
        /// </summary>
        public static class 调度模式
        {
            public const string 绝对时间 = "绝对时间";
            public const string 倒计时 = "倒计时";
            public const string 重复 = "重复";
            public const string 循环 = "循环";
        }

        /// <summary>
        /// 重复周期常量
        /// </summary>
        public static class 重复周期
        {
            public const string 每天 = "每天";
            public const string 每周 = "每周";
            public const string 每月 = "每月";
        }

        /// <summary>
        /// 默认分类颜色
        /// </summary>
        public const string 默认分类颜色 = "#FF808080";

        /// <summary>
        /// 启用状态文本
        /// </summary>
        public const string 启用 = "启用";
        /// <summary>
        /// 禁用状态文本
        /// </summary>
        public const string 禁用 = "禁用";
    }
}