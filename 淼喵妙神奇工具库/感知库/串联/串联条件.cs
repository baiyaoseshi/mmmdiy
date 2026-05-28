using System;
using System.Collections.Generic;

namespace 淼喵妙神奇工具库.感知库.串联
{
    public class 串联条件 : 串联抽象类
    {
        public string 变量名 { get; private set; }
        public 串联抽象类? 附加 { get; private set; }

        public 串联条件(string 变量名, 串联抽象类? 附加 = null)
        {
            this.变量名 = 变量名;
            this.附加 = 附加;
        }

        public override bool 判断(IntPtr hWnd, Dictionary<string, object> 全局)
        {
            if (全局.TryGetValue(变量名, out var value) && value is bool b && b)
            {
                if (附加 != null)
                    return 附加.判断(hWnd, 全局);
                return true;
            }
            return false;
        }
    }
}
