using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 执行任务 : 控制节点
    {
        public 节点参数<string> 任务路径 = new 节点参数<string>();

        执行任务() : base() { 任务路径 = new 节点参数<string>(); }

        public 执行任务(节点参数<string> 任务路径) : base()
        {
            this.任务路径 = 任务路径;
        }

        public 执行任务(string 任务路径) : base()
        {
            this.任务路径 = new 节点参数<string>(任务路径);
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            string 路径 = 从全局解析(任务路径, 全局);
            if (string.IsNullOrEmpty(路径) || !File.Exists(路径))
            {
                return false;
            }

            try
            {
                string 脚本内容 = File.ReadAllText(路径);
                var 子脚本 = new 自动任务脚本(hWnd, 脚本内容);
                return 子脚本.执行();
            }
            catch
            {
                return false;
            }
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@执行任务:\n" +
                   $"任务路径[{节点参数.序列化(任务路径)}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 执行任务 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@执行任务:"))
            {
                var 节点 = new 执行任务();
                Regex regex = new Regex(@"任务路径\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.任务路径 = 节点参数.反序列化<string>(match.Groups[1].Value);
                }
                控制节点.解析基类字段(节点, 字符串, null);
                return 节点;
            }
            return null;
        }

        public static 执行任务 创建节点(IntPtr hWnd)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "脚本文件 (*.script)|*.script|所有文件 (*.*)|*.*",
                Title = "选择要执行的任务"
            };

            if (dialog.ShowDialog() == true)
            {
                return new 执行任务(new 节点参数<string>(dialog.FileName));
            }
            return null;
        }
    }
}