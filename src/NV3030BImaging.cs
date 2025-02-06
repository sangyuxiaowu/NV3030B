using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Iot.Device.Graphics;

namespace Sang.IoT.NV3030B
{
    public partial class NV3030B
    {
        /// <summary>
        /// Send a bitmap to the NV3030B display
        /// </summary>
        /// <param name="bm">The bitmap to send</param>
        /// <param name="sourcePoint">Source point in bitmap</param>
        /// <param name="destinationRect">Destination rectangle on display</param>
        /// <param name="update">True to immediately update screen</param>
        public void DrawBitmap(BitmapImage bm, Point sourcePoint, Rectangle destinationRect, bool update)
        {
            if (bm is null)
            {
                throw new ArgumentNullException(nameof(bm));
            }

            FillBackBufferFromImage(bm, sourcePoint, destinationRect);

            if (update)
            {
                SendFrame(false);
            }
        }

        /// <inheritdoc />
        public override void DrawBitmap(BitmapImage bm)
        {
            int width = Math.Min(ScreenWidth, bm.Width);
            int height = Math.Min(ScreenHeight, bm.Height);
            
            DrawBitmap(bm, new Point(0, 0), new Rectangle(0, 0, width, height), true);
        }

        private void FillBackBufferFromImage(BitmapImage image, Point sourcePoint, Rectangle destinationRect)
        {
            if (image is null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            // 调整目标区域
            Converters.AdjustImageDestination(image, ref sourcePoint, ref destinationRect);

            // 调整Y坐标偏移，与Python代码保持一致
            destinationRect.Y += 20;
            
            // 并行处理转换图像数据
            Parallel.For(0, destinationRect.Height, y =>
            {
                var row = _screenBuffer.AsSpan(y * ScreenWidth, ScreenWidth);
                for (int x = 0; x < destinationRect.Width; x++)
                {
                    int xSource = sourcePoint.X + x;
                    int ySource = sourcePoint.Y + y;
                    if (xSource < image.Width && ySource < image.Height)
                    {
                        row[x] = Rgb565.FromRgba32(image[xSource, ySource]);
                    }
                }
            });
        }

        /// <summary>
        /// 直接发送像素数据到显示器指定区域
        /// </summary>
        public void SendBitmapPixelData(Span<byte> pixelData, Rectangle destinationRect)
        {
            SetWindow(destinationRect.X, destinationRect.Y, 
                    (destinationRect.Right - 1), (destinationRect.Bottom - 1));
            SendData(pixelData.ToArray());
            UpdateFps();
        }

    }
}
