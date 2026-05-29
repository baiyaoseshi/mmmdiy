using Newtonsoft.Json;
using System;

namespace 淼喵妙神奇工具库.感知库.串联
{
    [Serializable]
    public class 条件条目数据
    {
        public string 类型 { get; set; } = "";
        public 串联图片数据? 图片数据 { get; set; }
        public 串联条件数据? 变量数据 { get; set; }
        public bool 反转 { get; set; }

        public static 条件条目数据 从条件创建((串联抽象类, bool) 条件)
        {
            var 条目 = new 条件条目数据 { 反转 = 条件.Item2 };
            if (条件.Item1 is 串联图片 sp)
            {
                条目.类型 = "图片";
                条目.图片数据 = 串联图片数据.从串联图创建(sp);
            }
            else if (条件.Item1 is 串联条件 st)
            {
                条目.类型 = "变量";
                条目.变量数据 = 串联条件数据.从串联条件创建(st);
            }
            return 条目;
        }

        public (串联抽象类, bool) 还原为条件()
        {
            串联抽象类? 条件 = null;
            if (类型 == "图片" && 图片数据 != null)
                条件 = 图片数据.还原为串联图();
            else if (类型 == "变量" && 变量数据 != null)
                条件 = 变量数据.还原为串联条件();
            return (条件!, 反转);
        }
    }
}
