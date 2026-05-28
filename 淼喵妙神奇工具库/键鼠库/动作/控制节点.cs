using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using 淼喵妙神奇工具库.感知库;
using 淼喵妙神奇工具库.感知库.串联;
using 淼喵妙神奇工具库.输出库;
using 淼喵妙神奇工具库.键鼠库.动作.流程节点;
using 淼喵妙神奇工具库.键鼠库.监听.获取;

namespace 淼喵妙神奇工具库.键鼠库.动作
{
    public abstract class 控制节点 : INotifyPropertyChanged 
    {
        public virtual bool 需要窗口句柄 => false;
        public virtual void 初始化()
        {
        }
        public virtual bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            return true;
        }

        /// <summary>
        /// 从全局字典解析节点参数的最终值（辅助方法，简化子类调用）
        /// </summary>
        protected static T 从全局解析<T>(节点参数<T> 参数, Dictionary<string, object> 全局)
        {
            return 参数.解析值(全局);
        }

        public virtual string 保存为字符串(自动任务脚本 脚本)
        {
            var 条件条目列表 = new List<条件条目数据>();
            foreach (var 条件 in 条件列表)
            {
                if (条件.Item1 != null)
                {
                    条件条目列表.Add(条件条目数据.从条件创建(条件));
                }
            }
            return $"节点名字[{节点名字}],\n" +
                $"节点备注[{节点备注}],\n" +
                $"成功后跳转[{脚本.节点列表.IndexOf(成功后跳转 ?? new 停止任务())}],\n" +
                $"失败后跳转[{脚本.节点列表.IndexOf(失败后跳转 ?? new 停止任务())}],\n" +
                $"成功后等待[{成功后等待}],\n" +
                $"失败后等待[{失败后等待}],\n" +
                $"条件列表[{JsonConvert.SerializeObject(条件条目列表)}],\n";
        }
        public static void 解析基类字段(控制节点 节点, string 字符串, 自动任务脚本 脚本)
        {
            Regex regex = new Regex(@"节点名字\[([^\]]*)\],");
            Match match = regex.Match(字符串);
            if (match.Success)
            {
                节点.节点名字 = match.Groups[1].Value;
            }
            
            regex = new Regex(@"节点备注\[([^\]]*)\],");
            match = regex.Match(字符串);
            if (match.Success)
            {
                节点.节点备注 = match.Groups[1].Value;
            }
            regex = new Regex(@"成功后跳转\[([^\]]+)\],");
            match = regex.Match(字符串);
            if (match.Success && match.Groups[1].Value != "-1")
            {
                int 索引 = int.Parse(match.Groups[1].Value);
                if (索引 >= 0 && 索引 < 脚本.节点列表.Count)
                {
                    节点.成功后跳转 = 脚本.节点列表[索引];
                }
            }
            regex = new Regex(@"失败后跳转\[([^\]]+)\],");
            match = regex.Match(字符串);
            if (match.Success && match.Groups[1].Value != "-1")
            {
                int 索引 = int.Parse(match.Groups[1].Value);
                if (索引 >= 0 && 索引 < 脚本.节点列表.Count)
                {
                    节点.失败后跳转 = 脚本.节点列表[索引];
                }
            }
            regex = new Regex(@"成功后等待\[([^\]]+)\],");
            match = regex.Match(字符串);
            if (match.Success)
            {
                节点.成功后等待 = int.Parse(match.Groups[1].Value);
            }
            regex = new Regex(@"失败后等待\[([^\]]+)\],");
            match = regex.Match(字符串);
            if (match.Success)
            {
                节点.失败后等待 = int.Parse(match.Groups[1].Value);
            }
            regex = new Regex(@"条件列表\[(.+)\],");
            match = regex.Match(字符串);
            if (match.Success && !string.IsNullOrEmpty(match.Groups[1].Value))
            {
                var json = match.Groups[1].Value;
                try
                {
                    var 条目列表 = JsonConvert.DeserializeObject<List<条件条目数据>>(json);
                    if (条目列表 != null)
                    {
                        foreach (var 条目 in 条目列表)
                        {
                            if (!string.IsNullOrEmpty(条目.类型))
                            {
                                节点.条件列表.Add(条目.还原为条件());
                            }
                        }
                    }
                }
                catch
                {
                    var 条件列表数据 = JsonConvert.DeserializeObject<List<(串联图片数据, bool)>>(json);
                    if (条件列表数据 != null)
                    {
                        foreach (var 条件数据 in 条件列表数据)
                        {
                            if (条件数据.Item1 != null)
                            {
                                节点.条件列表.Add((条件数据.Item1.还原为串联图(), 条件数据.Item2));
                            }
                        }
                    }
                }
            }
        }
        public 控制节点? 成功后跳转;
        public 控制节点? 失败后跳转;
        public int 成功后等待 { get; set; }
        public int 失败后等待 { get; set; }
        public string 节点备注 { get; set; } = "";
        private string _节点名字 = "";
        public string 节点名字
        {
            get => _节点名字;
            set
            {
                if (_节点名字 != value)
                {
                    _节点名字 = value;
                    OnPropertyChanged(nameof(节点名字));
                }
            }
        }
        public 控制节点(int 成功后等待 = 1000, int 失败后等待 = 1000)
        {
            this.成功后等待 = 成功后等待;
            this.失败后等待 = 失败后等待;
            this.节点名字 = this.GetType().Name;
        }
        public List<(串联抽象类, bool)> 条件列表 = new List<(串联抽象类, bool)>();
        public bool 执行条件(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (条件列表.Count == 0)
                return true;
            foreach (var 条件 in 条件列表)
                if (条件.Item1 != null && (条件.Item1.判断(hWnd, 全局) ^ 条件.Item2))
                    return true;
            return false;
        }
        public 控制节点? 执行(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            状态 = 节点状态.正在运行;
            try
            {
                if (执行条件(hWnd, 全局) && 节点动作(hWnd, 全局))
                {
                    等待(成功后等待);
                    return 成功后跳转;
                }
                等待(失败后等待);
                return 失败后跳转;
            }
            finally
            {
                状态 = 节点状态.未活跃;
            }
        }

        public void 添加条件(IntPtr hWnd)
        {
            if (通知工具.确认弹窗("添加变量条件? (否=图像条件)"))
            {
                var 变量名 = 通知工具.输入弹窗("请输入全局变量名:", "", "");
                if (string.IsNullOrEmpty(变量名))
                    return;
                条件列表.Add((new 串联条件(变量名), 通知工具.确认弹窗("是否反转条件:")));
            }
            else
            {
                var 条件 = 串联图片.创建(hWnd, out _);
                if (条件 == null)
                    return;
                条件列表.Add((条件, 通知工具.确认弹窗("是否反转条件:")));
            }
        }
        private DateTime _executionStartTime;
        private DateTime _waitStartTime;
        private int _waitDurationMs;
        public enum 节点状态 { 未活跃, 正在运行, 正在等待 }
        private 节点状态 _状态;
        public 节点状态 状态
        {
            get => _状态;
            set
            {
                _状态 = value;
                if (value == 节点状态.正在运行)
                {
                    _executionStartTime = DateTime.Now;
                }
                OnPropertyChanged(nameof(状态));
                OnPropertyChanged(nameof(状态显示文本));
            }
        }
        public string 状态显示文本
        {
            get
            {
                switch (状态)
                {
                    case 节点状态.正在运行:
                        double 运行秒 = Math.Round((DateTime.Now - _executionStartTime).TotalSeconds, 1);
                        return $"运行中 {运行秒:F1}s";
                    case 节点状态.正在等待:
                        double 已过秒 = Math.Round((DateTime.Now - _waitStartTime).TotalSeconds, 1);
                        double 总秒 = Math.Round(_waitDurationMs / 1000.0, 1);
                        return $"等待中 {已过秒:F1}s/{总秒:F1}s";
                    default:
                        return "";
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        /// <summary>
        /// 刷新状态显示文本 - 由外部定时器调用（每0.1秒），使运行/等待中的节点实时更新耗时；
        /// 统一使用 DateTime 计时替代笨重的计数器增量
        /// </summary>
        public void 刷新状态显示()
        {
            if (状态 == 节点状态.正在运行 || 状态 == 节点状态.正在等待)
            {
                OnPropertyChanged(nameof(状态显示文本));
            }
        }

        public void 等待(int 毫秒)
        {
            _waitStartTime = DateTime.Now;
            _waitDurationMs = 毫秒;
            状态 = 节点状态.正在等待;
            while ((DateTime.Now - _waitStartTime).TotalMilliseconds < 毫秒)
            {
                if (任务控制管理器.实例.IsCanceled)
                {
                    throw new OperationCanceledException();
                }
                
                if (!任务控制管理器.实例.IsPaused)
                {
                    Thread.Sleep(5);
                }
            }
        }
    }
}
