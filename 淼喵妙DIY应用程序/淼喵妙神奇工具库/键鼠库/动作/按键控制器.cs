using System.Runtime.InteropServices;
using System.Windows;
using WindowsInput;

namespace 淼喵妙神奇工具库.键鼠库.动作
{
    public static class 按键控制器
    {
        private static InputSimulator inputSimulator = new InputSimulator();
        public static void 单击键(VirtualKeyCode 键) => inputSimulator.Keyboard.KeyPress(键);
        public static void 长按键(VirtualKeyCode 键) => inputSimulator.Keyboard.KeyDown(键);
        public static void 释放键(VirtualKeyCode 键) => inputSimulator.Keyboard.KeyUp(键);
        public static void 输入文本(string 文本) => inputSimulator.Keyboard.TextEntry(文本);
        public static void 快捷键(VirtualKeyCode 主键, VirtualKeyCode 修改键)
        {
            inputSimulator.Keyboard.KeyDown(修改键);
            inputSimulator.Keyboard.KeyPress(主键);
            inputSimulator.Keyboard.KeyUp(修改键);
        }
        public static void 快捷键(VirtualKeyCode 主键, VirtualKeyCode 修改键1, VirtualKeyCode 修改键2)
        {
            inputSimulator.Keyboard.KeyDown(修改键1);
            inputSimulator.Keyboard.KeyDown(修改键2);
            inputSimulator.Keyboard.KeyPress(主键);
            inputSimulator.Keyboard.KeyUp(修改键2);
            inputSimulator.Keyboard.KeyUp(修改键1);
        }
        public static void 快捷键(VirtualKeyCode 主键, VirtualKeyCode 修改键1, VirtualKeyCode 修改键2, VirtualKeyCode 修改键3)
        {
            inputSimulator.Keyboard.KeyDown(修改键1);
            inputSimulator.Keyboard.KeyDown(修改键2);
            inputSimulator.Keyboard.KeyDown(修改键3);
            inputSimulator.Keyboard.KeyPress(主键);
            inputSimulator.Keyboard.KeyUp(修改键3);
            inputSimulator.Keyboard.KeyUp(修改键2);
            inputSimulator.Keyboard.KeyUp(修改键1);
        }
    }
}
