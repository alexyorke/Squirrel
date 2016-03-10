using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Decagon.EE
{
    public class FastPixel
    {
        private BitmapData bmpData;
        private IntPtr bmpPtr;

        private bool locked;
        private byte[] rgbValues;

        public FastPixel(Bitmap bitmap)
        {
            if (bitmap.PixelFormat == (bitmap.PixelFormat | PixelFormat.Indexed))
            {
                throw new Exception("Cannot lock an Indexed image.");
            }
            Bitmap = bitmap;
            IsAlphaBitmap = Bitmap.PixelFormat == (Bitmap.PixelFormat | PixelFormat.Alpha);
            Width = bitmap.Width;
            Height = bitmap.Height;
        }

        private int Width { get; }

        private int Height { get; }

        private bool IsAlphaBitmap { get; }

        private Bitmap Bitmap { get; }

        public void Lock()
        {
            if (locked)
            {
                throw new Exception("Bitmap already locked.");
            }

            var rect = new Rectangle(0, 0, Width, Height);
            bmpData = Bitmap.LockBits(rect, ImageLockMode.ReadWrite, Bitmap.PixelFormat);
            bmpPtr = bmpData.Scan0;

            if (IsAlphaBitmap)
            {
                var bytes = Width*Height*4;
                rgbValues = new byte[bytes];
                Marshal.Copy(bmpPtr, rgbValues, 0, rgbValues.Length);
            }
            else
            {
                var bytes = Width*Height*3;
                rgbValues = new byte[bytes];
                Marshal.Copy(bmpPtr, rgbValues, 0, rgbValues.Length);
            }

            locked = true;
        }

        public void Unlock(bool setPixels)
        {
            if (!locked)
            {
                throw new Exception("Bitmap not locked.");
            }
            // Copy the RGB values back to the bitmap
            if (setPixels)
                Marshal.Copy(rgbValues, 0, bmpPtr, rgbValues.Length);
            // Unlock the bits.
            Bitmap.UnlockBits(bmpData);
            locked = false;
        }

        public void SetPixel(int x, int y, byte[] colour)
        {
            if (!locked)
            {
                throw new Exception("Bitmap not locked.");
            }

            if (IsAlphaBitmap)
            {
                var index = (y*Width + x)*4;
                rgbValues[index] = colour[0];
                rgbValues[index + 1] = colour[1];
                rgbValues[index + 2] = colour[2];
                rgbValues[index + 3] = colour[3];
            }
            else
            {
                var index = (y*Width + x)*3;
                rgbValues[index] = colour[0];
                rgbValues[index + 1] = colour[1];
                rgbValues[index + 2] = colour[2];
            }
        }

        public Color GetPixel(int x, int y)
        {
            if (!locked)
            {
                throw new Exception("Bitmap not locked.");
            }

            if (IsAlphaBitmap)
            {
                var index = (y*Width + x)*4;
                int b = rgbValues[index];
                int g = rgbValues[index + 1];
                int r = rgbValues[index + 2];
                int a = rgbValues[index + 3];
                return Color.FromArgb(a, r, g, b);
            }
            else
            {
                var index = (y*Width + x)*3;
                int b = rgbValues[index];
                int g = rgbValues[index + 1];
                int r = rgbValues[index + 2];
                return Color.FromArgb(r, g, b);
            }
        }
    }
}