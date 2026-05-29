using System;
using System.IO;
using System.Diagnostics;
using 淼喵妙神奇工具库.输出库;

namespace 淼喵妙神奇工具库.键鼠库.动作.流程节点
{
    public static class 脚本扩展工具
    {
        public static string 获取推荐脚本目录()
        {
            var 目录 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "淼喵妙脚本DIY", "scripts");
            Directory.CreateDirectory(目录);
            return 目录;
        }

        public static string 解析脚本路径(string 脚本路径)
        {
            if (Path.IsPathFullyQualified(脚本路径))
                return 脚本路径;
            return Path.GetFullPath(Path.Combine(获取推荐脚本目录(), 脚本路径));
        }

        public static void 用关联程序打开(string 文件路径)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = 文件路径,
                    UseShellExecute = true
                });
            }
            catch
            {
                var 编辑器路径 = 通知工具.输入弹窗("未找到关联程序，请手动输入编辑器程序路径:", "选择编辑器", "");
                if (!string.IsNullOrEmpty(编辑器路径))
                {
                    Process.Start(编辑器路径, $"\"{文件路径}\"");
                }
            }
        }

        public static void 创建CSharp模板(string 文件路径)
        {
            var 内容 = @"// C# 脚本模板 - 用于「C#脚本」节点
// 全局变量通过 vars 字典传入，使用 dynamic 类型访问
// 示例：dynamic 图片 = vars[""截图""];
// 可用的引用：System.Drawing, System.Linq, System.Collections.Generic
// 返回值将被写入节点的输出变量

// 在此编写你的逻辑：
string result = ""Hello from C# script"";

// 返回结果（将被写入输出变量）
return result;
";
            File.WriteAllText(文件路径, 内容, System.Text.Encoding.UTF8);
        }

        public static void 创建Python模板(string 文件路径)
        {
            var 内容 = @"# Python 脚本模板 - 用于「外部脚本」节点
# 全局变量通过 stdin 以 JSON 格式传入
# 示例：{""变量名1"": ""值1"", ""变量名2"": 123}
# 返回值通过 stdout 以 JSON 格式输出

import sys
import json

# 读取输入变量
input_data = json.loads(sys.stdin.read())

# 在此编写你的逻辑：
result = ""Hello from Python script""

# 输出结果（将被写入节点的输出变量）
print(json.dumps(result))
";
            File.WriteAllText(文件路径, 内容, System.Text.Encoding.UTF8);
        }

        public static void 创建NodeJs模板(string 文件路径)
        {
            var 内容 = @"// Node.js 脚本模板 - 用于「外部脚本」节点
// 全局变量通过 stdin 以 JSON 格式传入
// 返回值通过 stdout 以 JSON 格式输出

const readline = require('readline');

let inputData = '';

process.stdin.on('data', (chunk) => {
    inputData += chunk;
});

process.stdin.on('end', () => {
    const vars = JSON.parse(inputData);
    
    // 在此编写你的逻辑：
    const result = ""Hello from Node.js script"";
    
    // 输出结果（将被写入节点的输出变量）
    console.log(JSON.stringify(result));
});
";
            File.WriteAllText(文件路径, 内容, System.Text.Encoding.UTF8);
        }

        public static string 获取目录提示文本()
        {
            return $"推荐脚本存放目录：{获取推荐脚本目录()}\n建议使用相对路径引用脚本文件。";
        }
    }
}
