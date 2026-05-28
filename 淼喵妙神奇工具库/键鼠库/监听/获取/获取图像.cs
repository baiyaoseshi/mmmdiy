using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace 淼喵妙神奇工具库.键鼠库.监听.获取
{
    public class 获取图像 : 获取交互基类
    {
        private Rectangle _resultRect;
        private bool _isDragging;
        private Point _dragStart, _currentPos;
        private readonly IntPtr _hWnd;

        public 获取图像(IntPtr hWnd = default)
        {
            _hWnd = hWnd;
            _targetWindow = hWnd;
        }

        public Mat 获取图像信息()
        {
            启动获取();
            等待完成();
            if (!_获取成功 || _resultRect.Width <= 0 || _resultRect.Height <= 0)
            {
                Dispose();
                return null;
            }
            Bitmap bmp = new Bitmap(_resultRect.Width, _resultRect.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(_resultRect.X, _resultRect.Y, 0, 0, _resultRect.Size);
            }
                
            Dispose();
            return bmp.ToMat();
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