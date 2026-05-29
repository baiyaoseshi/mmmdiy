using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace 淼喵妙用户界面.Dialogs
{
    public partial class ImageViewerDialog : Window
    {
        private List<Bitmap> _images;
        private int _currentIndex;

        public ImageViewerDialog(string title, List<Bitmap> images)
        {
            InitializeComponent();
            Title = title;
            _images = images;
            _currentIndex = 0;
            ShowImage(0);
        }

        private void ShowImage(int index)
        {
            _currentIndex = index;
            var bmp = _images[index];

            ImageDisplay.Source = BitmapToBitmapSource(bmp);
            IndexLabel.Text = $"{index + 1} / {_images.Count}";
            SizeLabel.Text = $"{bmp.Width} × {bmp.Height}";

            PrevButton.IsEnabled = index > 0;
            NextButton.IsEnabled = index < _images.Count - 1;
        }

        private static BitmapSource BitmapToBitmapSource(Bitmap bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, ImageFormat.Png);
            memory.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
                ShowImage(_currentIndex - 1);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _images.Count - 1)
                ShowImage(_currentIndex + 1);
        }
    }
}
