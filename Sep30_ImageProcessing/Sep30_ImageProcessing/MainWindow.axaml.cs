using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using OpenCvSharp;
using System;
using System.IO;

namespace Sep30_ImageProcessing
{
    public partial class MainWindow : Avalonia.Controls.Window
    {
        private WriteableBitmap? _loadedImage;
        private WriteableBitmap? _processedImage;
        
        private VideoCapture? _capture;
        private DispatcherTimer? _timer;
        private bool _isVideoGreyscale = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private WriteableBitmap CreateBlankWriteableBitmap(WriteableBitmap src)
        {
            return new WriteableBitmap(src.PixelSize, src.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
        }

        public static unsafe WriteableBitmap MatToWriteableBitmap(Mat mat)
        {
            var wb = new WriteableBitmap(new PixelSize(mat.Width, mat.Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
            using (var lockWb = wb.Lock())
            {
                using var bgraMat = new Mat();
                Cv2.CvtColor(mat, bgraMat, ColorConversionCodes.BGR2BGRA);
                long size = bgraMat.Total() * bgraMat.ElemSize();
                Buffer.MemoryCopy((void*)bgraMat.Data, (void*)lockWb.Address, lockWb.RowBytes * lockWb.Size.Height, size);
            }
            return wb;
        }

        private async void OpenFile_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Image",
                AllowMultiple = false
            });

            if (files.Count > 0)
            {
                await using var stream = await files[0].OpenReadAsync();
                var tempBitmap = new Avalonia.Media.Imaging.Bitmap(stream);
                
                // Convert to WriteableBitmap (Bgra8888)
                _loadedImage = new WriteableBitmap(tempBitmap.PixelSize, tempBitmap.Dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
                using (var frameBuffer = _loadedImage.Lock())
                {
                    tempBitmap.CopyPixels(new Avalonia.PixelRect(0, 0, tempBitmap.PixelSize.Width, tempBitmap.PixelSize.Height), frameBuffer.Address, frameBuffer.RowBytes * frameBuffer.Size.Height, frameBuffer.RowBytes);
                }
                
                ImageLoaded.Source = _loadedImage;
                UpdateProcessedImage();
            }
        }

        private async void SaveFile_Click(object? sender, RoutedEventArgs e)
        {
            if (_processedImage == null) return;
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Processed Image",
                DefaultExtension = "png"
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                _processedImage.Save(stream);
            }
        }

        private void PixelCopy_Click(object? sender, RoutedEventArgs e)
        {
            if (_loadedImage == null) return;
            _processedImage = CreateBlankWriteableBitmap(_loadedImage);
            ImageProcess.CopyImage(_loadedImage, _processedImage);
            ImageProcessed.Source = _processedImage;
        }

        private void Greyscaling_Click(object? sender, RoutedEventArgs e)
        {
            if (_loadedImage == null) return;
            _processedImage = CreateBlankWriteableBitmap(_loadedImage);
            ImageProcess.Greyscale(_loadedImage, _processedImage);
            ImageProcessed.Source = _processedImage;
        }

        private void Inversion_Click(object? sender, RoutedEventArgs e)
        {
            if (_loadedImage == null) return;
            _processedImage = CreateBlankWriteableBitmap(_loadedImage);
            ImageProcess.Invert(_loadedImage, _processedImage);
            ImageProcessed.Source = _processedImage;
        }

        private void MirrorHoriz_Click(object? sender, RoutedEventArgs e)
        {
            if (_loadedImage == null) return;
            _processedImage = CreateBlankWriteableBitmap(_loadedImage);
            ImageProcess.FlipHorizontal(_loadedImage, _processedImage);
            ImageProcessed.Source = _processedImage;
        }

        private void MirrorVert_Click(object? sender, RoutedEventArgs e)
        {
            if (_loadedImage == null) return;
            _processedImage = CreateBlankWriteableBitmap(_loadedImage);
            ImageProcess.FlipVertical(_loadedImage, _processedImage);
            ImageProcessed.Source = _processedImage;
        }

        private void UpdateProcessedImage()
        {
            if (_loadedImage == null) return;
            
            var temp1 = CreateBlankWriteableBitmap(_loadedImage);
            var temp2 = CreateBlankWriteableBitmap(_loadedImage);
            var temp3 = CreateBlankWriteableBitmap(_loadedImage);

            ImageProcess.Contrast(_loadedImage, temp1, (int)ContrastSlider.Value);
            ImageProcess.Brightness(temp1, temp2, (int)BrightnessSlider.Value);
            ImageProcess.Rotate(temp2, temp3, (int)RotationSlider.Value);

            _processedImage = temp3;
            ImageProcessed.Source = _processedImage;
        }

        private void OnCamera_Click(object? sender, RoutedEventArgs e)
        {
            DeviceErrorText.IsVisible = false;
            DeviceSelectorPanel.IsVisible = true;
        }

        private void DeviceOk_Click(object? sender, RoutedEventArgs e)
        {
            if (StartCamera(DeviceComboBox.SelectedIndex))
            {
                DeviceSelectorPanel.IsVisible = false;
            }
        }

        private void DeviceApply_Click(object? sender, RoutedEventArgs e)
        {
            StartCamera(DeviceComboBox.SelectedIndex);
        }

        private void DeviceCancel_Click(object? sender, RoutedEventArgs e)
        {
            DeviceSelectorPanel.IsVisible = false;
        }

        private bool StartCamera(int index)
        {
            DeviceErrorText.IsVisible = false;
            try
            {
                _timer?.Stop();
                _capture?.Release();
                
                _capture = new VideoCapture(index, VideoCaptureAPIs.AVFOUNDATION);
                if (!_capture.IsOpened())
                {
                    _capture = new VideoCapture(index, VideoCaptureAPIs.ANY);
                }

                if (!_capture.IsOpened())
                {
                    DeviceErrorText.Text = $"Failed to open camera index {index}. It may not exist or is in use.";
                    DeviceErrorText.IsVisible = true;
                    return false;
                }
                
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(33)
                };
                _timer.Tick += Timer_Tick;
                _timer.Start();
                return true;
            }
            catch (Exception ex)
            {
                DeviceErrorText.Text = "Camera error: " + ex.Message;
                DeviceErrorText.IsVisible = true;
                return false;
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_capture != null && _capture.IsOpened())
            {
                using var frame = new Mat();
                _capture.Read(frame);
                if (!frame.Empty())
                {
                    _loadedImage = MatToWriteableBitmap(frame);
                    ImageLoaded.Source = _loadedImage;

                    if (_isVideoGreyscale)
                    {
                        _processedImage = CreateBlankWriteableBitmap(_loadedImage);
                        ImageProcess.Greyscale(_loadedImage, _processedImage);
                        ImageProcessed.Source = _processedImage;
                    }
                    else 
                    {
                        UpdateProcessedImage();
                    }
                }
            }
        }

        private void OffCamera_Click(object? sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            _capture?.Release();
            _capture = null;
            ImageLoaded.Source = null;
            ImageProcessed.Source = null;
        }

        private void VideoGreyscale_Click(object? sender, RoutedEventArgs e)
        {
            _isVideoGreyscale = !_isVideoGreyscale;
        }

        private void Slider_ValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
        {
            UpdateProcessedImage();
        }
    }
}