using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public class 智能执行任务 : 控制节点
    {
        public string 目标描述 = "";
        public string 脚本搜索路径 = "";

        智能执行任务() : base() { }

        public 智能执行任务(string 目标描述, string 脚本搜索路径 = "") : base()
        {
            this.目标描述 = 目标描述;
            this.脚本搜索路径 = 脚本搜索路径;
        }

        public override bool 节点动作(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (string.IsNullOrEmpty(目标描述))
            {
                return false;
            }

            try
            {
                string 匹配的脚本 = 获取全局端AI匹配的脚本(目标描述);

                if (!string.IsNullOrEmpty(匹配的脚本) && File.Exists(匹配的脚本))
                {
                    string 脚本内容 = File.ReadAllText(匹配的脚本);
                    var 子脚本 = new 自动任务脚本(hWnd, 脚本内容);
                    return 子脚本.执行();
                }

                Debug.WriteLine($"[智能执行任务] AI 调用失败或配置不全，无法匹配脚本。目标: {目标描述}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[智能执行任务] 执行出错: {ex.Message}");
                return false;
            }
        }

        private string 获取全局端AI匹配的脚本(string 目标)
        {
            var 脚本列表 = 获取所有可用脚本();
            var config = AI配置管理器.获取全局配置();

            if (!AI配置管理器.配置是否有效(config) || 脚本列表.Count == 0)
            {
                Debug.WriteLine("[智能执行任务] AI 配置无效或无可用脚本");
                return null;
            }

            try
            {
                string 脚本信息 = "";
                foreach (var 脚本路径 in 脚本列表)
                {
                    var 脚本信息项 = 解析脚本信息(脚本路径);
                    脚本信息 += $"脚本名: {脚本信息项.Item1}, 备注: {脚本信息项.Item2}\n";
                }

                string 提示词 = $"根据目标描述选择最合适的脚本执行。\n\n目标: {目标}\n\n可用脚本:\n{脚本信息}\n\n请返回你认为最合适的脚本文件名(不含扩展名)。";

                var 消息历史 = new List<AIChatMessage>
                {
                    new AIChatMessage { 角色 = "系统", 内容 = "你是一个智能脚本规划助手，根据用户目标选择最合适的脚本执行。只返回脚本文件名，不要返回其他内容。" },
                    new AIChatMessage { 角色 = "用户", 内容 = 提示词 }
                };

                string 响应 = Task.Run(() => AI配置管理器.调用AI(config, 消息历史, "")).Result;

                if (!string.IsNullOrEmpty(响应))
                {
                    foreach (var 脚本路径 in 脚本列表)
                    {
                        string 脚本名 = Path.GetFileNameWithoutExtension(脚本路径);
                        if (响应.Contains(脚本名) || 脚本名.Contains(响应.Trim()))
                        {
                            return 脚本路径;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[智能执行任务] AI 调用失败: {ex.Message}");
            }
            return null;
        }

        private List<string> 获取所有可用脚本()
        {
            List<string> 脚本列表 = new List<string>();

            if (!string.IsNullOrEmpty(脚本搜索路径) && Directory.Exists(脚本搜索路径))
            {
                脚本列表.AddRange(Directory.GetFiles(脚本搜索路径, "*.script", SearchOption.AllDirectories));
            }

            string 用户文档路径 = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string 默认脚本路径 = Path.Combine(用户文档路径, "脚本");
            if (Directory.Exists(默认脚本路径))
            {
                脚本列表.AddRange(Directory.GetFiles(默认脚本路径, "*.script", SearchOption.AllDirectories));
            }

            return 脚本列表.Distinct().ToList();
        }

        private (string, string) 解析脚本信息(string 脚本路径)
        {
            try
            {
                string 内容 = File.ReadAllText(脚本路径);
                string 脚本名 = Path.GetFileNameWithoutExtension(脚本路径);
                string 备注 = "";

                Regex 脚本备注Regex = new Regex(@"进程名\[([^\]]*)\]窗口标题\[([^\]]*)\](.*)");
                Match 脚本备注匹配 = 脚本备注Regex.Match(内容);
                if (脚本备注匹配.Success)
                {
                    备注 = 脚本备注匹配.Groups[3].Value.Trim();
                }

                if (string.IsNullOrEmpty(备注))
                {
                    Regex 节点备注Regex = new Regex(@"节点备注\[([^\]]*)\]");
                    Match 节点备注匹配 = 节点备注Regex.Match(内容);
                    if (节点备注匹配.Success)
                    {
                        备注 = 节点备注匹配.Groups[1].Value;
                    }
                }

                return (脚本名, 备注);
            }
            catch
            {
                return (Path.GetFileNameWithoutExtension(脚本路径), "");
            }
        }

        public override string 保存为字符串(自动任务脚本 脚本)
        {
            return "@智能规划任务:\n" +
                   $"目标描述[{目标描述}],\n" +
                   $"脚本搜索路径[{脚本搜索路径}],\n" +
                   base.保存为字符串(脚本);
        }

        public static 智能执行任务 解析(string 字符串, IntPtr hWnd)
        {
            if (字符串.StartsWith("@智能规划任务:"))
            {
                var 节点 = new 智能执行任务();
                Regex regex = new Regex(@"目标描述\[([^\]]+)\]");
                Match match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.目标描述 = match.Groups[1].Value;
                }
                regex = new Regex(@"脚本搜索路径\[([^\]]+)\]");
                match = regex.Match(字符串);
                if (match.Success)
                {
                    节点.脚本搜索路径 = match.Groups[1].Value;
                }
                return 节点;
            }
            return null;
        }

        public static 智能执行任务 创建节点(IntPtr hWnd)
        {
            string 目标 = 通知工具.输入弹窗("请输入智能规划的目标:", "智能规划任务", "");
            if (!string.IsNullOrEmpty(目标))
            {
                string 路径 = 通知工具.输入弹窗("请输入脚本搜索路径(可选):", "智能规划任务", "");
                return new 智能执行任务(目标, string.IsNullOrEmpty(路径) ? "" : 路径);
            }
            return null;
        }
    }
}