using System;
using System.Drawing;
using 淼喵妙神奇工具库.感知库;

namespace 淼喵妙神奇工具库.键鼠库.监听.获取
{
    public class 获取点 : 获取交互基类
    {
        private Point _resultPoint;
        private readonly IntPtr _hWnd;

        public 获取点(IntPtr hWnd = default)
        {
            _hWnd = hWnd;
            _targetWindow = hWnd;
        }

        public Point 获取点信息()
        {
            启动获取();
            等待完成();
            if (!_获取成功)
            {
                Dispose();
                return Point.Empty;
            }
            var point = _resultPoint;
            var rect = Rectangle.Empty;
            if (_hWnd != IntPtr.Zero)
                rect = 窗口处理器.获取窗口框(_hWnd);

            if (rect != Rectangle.Empty)
                point = new Point(point.X - rect.X, point.Y - rect.Y);
            Dispose();
            return point;
        }

        protected override void 处理鼠标移动(Point pos) => 绘制原色彩圆形(pos, 10);
        protected override void 处理左键按下() { }

        protected override void 处理左键释放()
        {
            Point pos;
            GetCursorPos(out pos);
            _resultPoint = pos;
            完成();
        }
    }
}