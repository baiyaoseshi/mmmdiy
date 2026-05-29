using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 从列表中按序读取 : 控制节点
    {
        public override bool 需要窗口句柄 => false;

        public string 列表变量名 = "";
        public string 输出变量名 = "";
        private int index = 0;
        public override void 初始化()
        {
            index = 0;
            base.初始化();
        }
        从列表中按序读取() : base() { }

        public 从列表中按序读取(string 列表变量名, string 输出变量名) : base()
        {
            this.列表变量名 = 列表变量名;
            this.输出变量名 = 输出变量名;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (!全局.ContainsKey(列表变量名))
            {
                通知工具.吐司通知($"全局变量 '{列表变量名}' 不存在");
                return false;
            }

            var 列表对象 = 全局[列表变量名];

            if (列表对象 is IList 列表)
            {
                if (index >= 列表.Count)
                {
                    index = 0;
                    return false;
                }
                全局[输出变量名] = 列表[index];
                index++;
                return true;
            }
            else
            {
                通知工具.吐司通知($"变量 '{列表变量名}' 不是列表类型");
                return false;
            }
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@从列表中按序读取:\n" +
                   $"列表变量名[{列表变量名}],\n" +
                   $"输出变量名[{输出变量名}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 从列表中按序读取 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@从列表中按序读取:"))
            {
                var 节点 = new 从列表中按序读取();
                Regex regex = new Regex(@"列表变量名\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.列表变量名 = match.Groups[1].Value;
                }
                regex = new Regex(@"输出变量名\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.输出变量名 = match.Groups[1].Value;
                }
                return 节点;
            }
            return null;
        }

        public static 从列表中按序读取 创建节点(IntPtr hWnd)
        {
            string 列表变量名 = 通知工具.输入弹窗("请输入列表全局变量名:", "", "");
            if (string.IsNullOrEmpty(列表变量名))
                return null;
            string 输出变量名 = 通知工具.输入弹窗("请输入要设置的单值变量名:", "", "");
            return new 从列表中按序读取(列表变量名, 输出变量名);
        }
    }
}
