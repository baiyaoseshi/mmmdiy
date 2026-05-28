using Newtonsoft.Json;
using System;

namespace 淼喵妙神奇工具库.感知库.串联
{
    [Serializable]
    public class 串联条件数据
    {
        public string 变量名 { get; set; } = "";

        public static 串联条件数据 从串联条件创建(串联条件 条件)
        {
            if (条件 == null) return null;
            return new 串联条件数据 { 变量名 = 条件.变量名 };
        }

        public 串联条件 还原为串联条件()
        {
            return new 串联条件(变量名);
        }
    }
}
