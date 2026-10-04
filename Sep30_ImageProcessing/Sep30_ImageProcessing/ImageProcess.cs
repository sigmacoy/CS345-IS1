using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace Sep30_ImageProcessing
{
    public static class ImageProcess
    {
        public static unsafe void CopyImage(WriteableBitmap src, WriteableBitmap dest)
        {
            using (var srcLock = src.Lock())
            using (var destLock = dest.Lock())
            {
                int size = srcLock.RowBytes * src.PixelSize.Height;
                Buffer.MemoryCopy((void*)srcLock.Address, (void*)destLock.Address, size, size);
            }
        }
        
        public static unsafe void FlipHorizontal(WriteableBitmap src, WriteableBitmap dest)
        {
            int width = src.PixelSize.Width;
            int height = src.PixelSize.Height;
            using (var srcLock = src.Lock())
            using (var destLock = dest.Lock())
            {
                byte* srcPtr = (byte*)srcLock.Address;
                byte* destPtr = (byte*)destLock.Address;
                int bpp = 4; // Bgra8888
                int rowBytes = srcLock.RowBytes;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int srcIndex = y * rowBytes + x * bpp;
                        int destIndex = y * rowBytes + (width - 1 - x) * bpp;
                        destPtr[destIndex] = srcPtr[srcIndex];
                        destPtr[destIndex + 1] = srcPtr[srcIndex + 1];
                        destPtr[destIndex + 2] = srcPtr[srcIndex + 2];
                        destPtr[destIndex + 3] = srcPtr[srcIndex + 3];
                    }
                }
            }
        }
        
        public static unsafe void FlipVertical(WriteableBitmap src, WriteableBitmap dest)
        {
            int width = src.PixelSize.Width;
            int height = src.PixelSize.Height;
            using (var srcLock = src.Lock())
            using (var destLock = dest.Lock())
            {
                byte* srcPtr = (byte*)srcLock.Address;
                byte* destPtr = (byte*)destLock.Address;
                int bpp = 4;
                int rowBytes = srcLock.RowBytes;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int srcIndex = y * rowBytes + x * bpp;
                        int destIndex = (height - 1 - y) * rowBytes + x * bpp;
                        destPtr[destIndex] = srcPtr[srcIndex];
                        destPtr[destIndex + 1] = srcPtr[srcIndex + 1];
                        destPtr[destIndex + 2] = srcPtr[srcIndex + 2];
                        destPtr[destIndex + 3] = srcPtr[srcIndex + 3];
                    }
                }
            }
        }

        public static unsafe void Greyscale(WriteableBitmap src, WriteableBitmap dest)
        {
            int width = src.PixelSize.Width;
            int height = src.PixelSize.Height;
            using (var srcLock = src.Lock())
            using (var destLock = dest.Lock())
            {
                byte* srcPtr = (byte*)srcLock.Address;
                byte* destPtr = (byte*)destLock.Address;
                int bpp = 4;
                int rowBytes = srcLock.RowBytes;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * rowBytes + x * bpp;
                        byte b = srcPtr[index];
                        byte g = srcPtr[index + 1];
                        byte r = srcPtr[index + 2];
                        
                        byte gray = (byte)((r + g + b) / 3);
                        
                        destPtr[index] = gray;
                        destPtr[index + 1] = gray;
                        destPtr[index + 2] = gray;
                        destPtr[index + 3] = srcPtr[index + 3];
                    }
                }
            }
        }
        
        public static unsafe void Invert(WriteableBitmap src, WriteableBitmap dest)
        {
            int width = src.PixelSize.Width;
            int height = src.PixelSize.Height;
            using (var srcLock = src.Lock())
            using (var destLock = dest.Lock())
            {
                byte* srcPtr = (byte*)srcLock.Address;
                byte* destPtr = (byte*)destLock.Address;
                int bpp = 4;
                int rowBytes = srcLock.RowBytes;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * rowBytes + x * bpp;
                        destPtr[index] = (byte)(255 - srcPtr[index]);         // B
                        destPtr[index + 1] = (byte)(255 - srcPtr[index + 1]); // G
                        destPtr[index + 2] = (byte)(255 - srcPtr[index + 2]); // R
                        destPtr[index + 3] = srcPtr[index + 3];               // A
                    }
                }
            }
        }
        
        public static unsafe void Brightness(WriteableBitmap src, WriteableBitmap dest, int value)
        {
            int width = src.PixelSize.Width;
            int height = src.PixelSize.Height;
            using (var srcLock = src.Lock())
            using (var destLock = dest.Lock())
            {
                byte* srcPtr = (byte*)srcLock.Address;
                byte* destPtr = (byte*)destLock.Address;
                int bpp = 4;
                int rowBytes = srcLock.RowBytes;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * rowBytes + x * bpp;
                        int b = srcPtr[index] + value;
                        int g = srcPtr[index + 1] + value;
                        int r = srcPtr[index + 2] + value;
                        
                        destPtr[index] = (byte)Math.Clamp(b, 0, 255);
                        destPtr[index + 1] = (byte)Math.Clamp(g, 0, 255);
                        destPtr[index + 2] = (byte)Math.Clamp(r, 0, 255);
                        destPtr[index + 3] = srcPtr[index + 3];
                    }
                }
            }
        }
        
        public static unsafe void Contrast(WriteableBitmap src, WriteableBitmap dest, int nContrast)
        {
            double contrast = (100.0 + nContrast) / 100.0;
            contrast *= contrast;

            int width = src.PixelSize.Width;
            int height = src.PixelSize.Height;
            using (var srcLock = src.Lock())
            using (var destLock = dest.Lock())
            {
                byte* srcPtr = (byte*)srcLock.Address;
                byte* destPtr = (byte*)destLock.Address;
                int bpp = 4;
                int rowBytes = srcLock.RowBytes;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * rowBytes + x * bpp;
                        double b = srcPtr[index] / 255.0;
                        double g = srcPtr[index + 1] / 255.0;
                        double r = srcPtr[index + 2] / 255.0;

                        b = (((b - 0.5) * contrast) + 0.5) * 255.0;
                        g = (((g - 0.5) * contrast) + 0.5) * 255.0;
                        r = (((r - 0.5) * contrast) + 0.5) * 255.0;

                        destPtr[index] = (byte)Math.Clamp((int)b, 0, 255);
                        destPtr[index + 1] = (byte)Math.Clamp((int)g, 0, 255);
                        destPtr[index + 2] = (byte)Math.Clamp((int)r, 0, 255);
                        destPtr[index + 3] = srcPtr[index + 3];
                    }
                }
            }
        }
        
        public static unsafe void Rotate(WriteableBitmap src, WriteableBitmap dest, int value)
        {
            int width = src.PixelSize.Width;
            int height = src.PixelSize.Height;
            float angleRadians = (float)(value * Math.PI / 180.0);
            int xCenter = width / 2;
            int yCenter = height / 2;
            float cosA = (float)Math.Cos(angleRadians);
            float sinA = (float)Math.Sin(angleRadians);

            using (var srcLock = src.Lock())
            using (var destLock = dest.Lock())
            {
                byte* srcPtr = (byte*)srcLock.Address;
                byte* destPtr = (byte*)destLock.Address;
                int bpp = 4;
                int rowBytes = srcLock.RowBytes;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int dIndex = y * rowBytes + x * bpp;
                        destPtr[dIndex] = 0;
                        destPtr[dIndex + 1] = 0;
                        destPtr[dIndex + 2] = 0;
                        destPtr[dIndex + 3] = 0;
                    }
                }

                for (int xp = 0; xp < width; xp++)
                {
                    for (int yp = 0; yp < height; yp++)
                    {
                        int x0 = xp - xCenter;
                        int y0 = yp - yCenter;
                        
                        int xs = (int)(x0 * cosA + y0 * sinA) + xCenter;
                        int ys = (int)(-x0 * sinA + y0 * cosA) + yCenter;

                        int dIndex = yp * rowBytes + xp * bpp;
                        if (xs >= 0 && xs < width && ys >= 0 && ys < height)
                        {
                            int sIndex = ys * rowBytes + xs * bpp;
                            destPtr[dIndex] = srcPtr[sIndex];
                            destPtr[dIndex + 1] = srcPtr[sIndex + 1];
                            destPtr[dIndex + 2] = srcPtr[sIndex + 2];
                            destPtr[dIndex + 3] = srcPtr[sIndex + 3];
                        }
                    }
                }
            }
        }
    }
}
