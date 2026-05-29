using System;
using System.Drawing;
using 淼喵妙神奇工具库.感知库;

namespace 淼喵妙神奇工具库.键鼠库.监听.获取
{
    public class 获取框 : 获取交互基类
    {
        private Rectangle _resultRect;
        private bool _isDragging;
        private Point _dragStart, _currentPos;
        private readonly IntPtr _hWnd;

        public 获取框(IntPtr hWnd = default)
        {
            _hWnd = hWnd;
            _targetWindow = hWnd;
        }

        public Rectangle 获取框信息()
        {
            启动获取();
            等待完成();

            if (!_获取成功 || _resultRect.IsEmpty)
            {
                Dispose();
                return Rectangle.Empty;
            }

            var rect = Rectangle.Empty;
            if (_hWnd != IntPtr.Zero && _resultRect != Rectangle.Empty)
                rect = 窗口处理器.获取窗口框(_hWnd);

            if (rect != Rectangle.Empty)
                _resultRect = new Rectangle(
                _resultRect.X - rect.X,
                _resultRect.Y - rect.Y,
                _resultRect.Width,
                _resultRect.Height);
            Dispose();
            return _resultRect;
        }

        protected override void 处理鼠标移动(Point pos)
        {
            _currentPos = pos;
            if (_isDragging)
            {
                var rect = new Rectangle(
                    Math.Min(_dragStart.X, pos.X),
                    Math.Min(_dragStart.Y, pos.Y),
                    Math.Abs(_dragStart.X - pos.X),
                    Math.Abs(_dragStart.Y - pos.Y));
                绘制原色彩矩形(rect);
            }
            else
            {
                绘制原色彩圆形(pos, 10);
            }
        }

        protected override void 处理左键按下()
        {
            _isDragging = true;
            _dragStart = _currentPos;
        }

        protected override void 处理左键释放()
        {
            if (_isDragging)
            {
                _isDragging = false;
                var x = Math.Min(_dragStart.X, _currentPos.X);
                var y = Math.Min(_dragStart.Y, _currentPos.Y);
                var w = Math.Abs(_dragStart.X - _currentPos.X);
                var h = Math.Abs(_dragStart.Y - _currentPos.Y);
                _resultRect = new Rectangle(x, y, w, h);
                完成();
            }
        }
    }
}