using System;
using System.Drawing;

namespace Decagon.EE
{
    public class FastPixel
    {
        public byte[] rgbValues;
        private System.Drawing.Imaging.BitmapData bmpData;
        private IntPtr bmpPtr;

        private bool locked = false;
        private bool _isAlpha = false;
        private Bitmap _bitmap;
        private int _width;
        private int _height;
        public int Width
        {
            get { return _width; }
        }
        public int Height
        {
            get { return _height; }
        }
        public bool IsAlphaBitmap
        {
            get { return _isAlpha; }
        }
        public Bitmap Bitmap
        {
            get { return _bitmap; }
        }

        public FastPixel(Bitmap bitmap)
        {
            if ((bitmap.PixelFormat == (bitmap.PixelFormat | System.Drawing.Imaging.PixelFormat.Indexed)))
            {
                throw new Exception("Cannot lock an Indexed image.");
            }
            _bitmap = bitmap;
            _isAlpha = (Bitmap.PixelFormat == (Bitmap.PixelFormat | System.Drawing.Imaging.PixelFormat.Alpha));
            _width = bitmap.Width;
            _height = bitmap.Height;
        }

        public void Lock()
        {
            if (locked)
            {
                throw new Exception("Bitmap already locked.");
            }

            Rectangle rect = new Rectangle(0, 0, Width, Height);
            bmpData = Bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, Bitmap.PixelFormat);
            bmpPtr = bmpData.Scan0;

            if (IsAlphaBitmap)
            {
                int bytes = (Width * Height) * 4;
                rgbValues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(bmpPtr, rgbValues, 0, rgbValues.Length);
            }
            else
            {
                int bytes = (Width * Height) * 3;
                rgbValues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(bmpPtr, rgbValues, 0, rgbValues.Length);
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
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, bmpPtr, rgbValues.Length);
            // Unlock the bits.
            Bitmap.UnlockBits(bmpData);
            locked = false;
        }

        public void Clear(Color colour)
        {
            if (!locked)
            {
                throw new Exception("Bitmap not locked.");
            }

            if (IsAlphaBitmap)
            {
                for (int index = 0; index <= rgbValues.Length - 1; index += 4)
                {
                    rgbValues[index] = colour.B;
                    rgbValues[index + 1] = colour.G;
                    rgbValues[index + 2] = colour.R;
                    rgbValues[index + 3] = colour.A;
                }
            }
            else
            {
                for (int index = 0; index <= rgbValues.Length - 1; index += 3)
                {
                    rgbValues[index] = colour.B;
                    rgbValues[index + 1] = colour.G;
                    rgbValues[index + 2] = colour.R;
                }
            }
        }
        public void SetPixel(Point location, Color colour)
        {
            SetPixel(location.X, location.Y, colour);
        }
        public void SetPixel(int x, int y, Color colour)
        {
            if (!locked)
            {
                throw new Exception("Bitmap not locked.");
            }

            if (IsAlphaBitmap)
            {
                int index = ((y * Width + x) * 4);
                rgbValues[index] = colour.B;
                rgbValues[index + 1] = colour.G;
                rgbValues[index + 2] = colour.R;
                rgbValues[index + 3] = colour.A;
            }
            else
            {
                int index = ((y * Width + x) * 3);
                rgbValues[index] = colour.B;
                rgbValues[index + 1] = colour.G;
                rgbValues[index + 2] = colour.R;
            }
        }
        public Color GetPixel(Point location)
        {
            return GetPixel(location.X, location.Y);
        }
        public Color GetPixel(int x, int y)
        {
            if (!locked)
            {
                throw new Exception("Bitmap not locked.");

            }

            if (IsAlphaBitmap)
            {
                int index = ((y * Width + x) * 4);
                int b = rgbValues[index];
                int g = rgbValues[index + 1];
                int r = rgbValues[index + 2];
                int a = rgbValues[index + 3];
                return Color.FromArgb(a, r, g, b);
            }
            else
            {
                int index = ((y * Width + x) * 3);
                int b = rgbValues[index];
                int g = rgbValues[index + 1];
                int r = rgbValues[index + 2];
                return Color.FromArgb(r, g, b);
            }
        }
    }
}