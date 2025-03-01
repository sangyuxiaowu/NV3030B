// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Iot.Device.Graphics;
using System.Drawing;
using System.Runtime.InteropServices;


namespace Sang.IoT.NV3030B
{
    public partial class NV3030B
    {
        /// <summary>
        /// Fill rectangle to the specified color
        /// </summary>
        /// <param name="color">The color to fill the rectangle with.</param>
        /// <param name="x">The x co-ordinate of the point to start the rectangle at in pixels.</param>
        /// <param name="y">The y co-ordinate of the point to start the rectangle at in pixels.</param>
        /// <param name="w">The width of the rectangle in pixels.</param>
        /// <param name="h">The height of the rectangle in pixels.</param>
        public void FillRect(Color color, int x, int y, int w, int h)
        {
            FillRect(color, x, y, w, h, false);
        }

        /// <summary>
        /// Fill rectangle to the specified color
        /// </summary>
        /// <param name="color">The color to fill the rectangle with.</param>
        /// <param name="x">The x co-ordinate of the point to start the rectangle at in pixels.</param>
        /// <param name="y">The y co-ordinate of the point to start the rectangle at in pixels.</param>
        /// <param name="w">The width of the rectangle in pixels.</param>
        /// <param name="h">The height of the rectangle in pixels.</param>
        /// <param name="doRefresh">True to immediately update the screen, false to only update the back buffer</param>
        private void FillRect(Color color, int x, int y, int w, int h, bool doRefresh)
        {
            Span<byte> colourBytes = stackalloc byte[2]; // create a short span that holds the colour data to be sent to the display

            // set the colourbyte array to represent the fill colour
            var c = Rgb565.FromRgba32(color);

            // set the pixels in the array representing the raw data to be sent to the display
            // to the fill color
            for (int j = y; j < y + h; j++)
            {
                for (int i = x; i < x + w; i++)
                {
                    _screenBuffer[i + j * ScreenWidth] = c;
                }
            }

            if (doRefresh)
            {
                SendFrame(false);
            }
        }

        /// <summary>
        /// Clears the screen to a specific color
        /// </summary>
        /// <param name="color">The color to clear the screen to</param>
        /// <param name="doRefresh">Immediately force an update of the screen. If false, only the backbuffer is cleared.</param>
        public void ClearScreen(Color color, bool doRefresh)
        {
            FillRect(color, 0, 0, ScreenWidth, ScreenHeight, doRefresh);
        }

        /// <summary>
        /// Clears the screen to black
        /// </summary>
        /// <param name="doRefresh">Immediately force an update of the screen. If false, only the backbuffer is cleared.</param>
        public void ClearScreen(bool doRefresh)
        {
            FillRect(Color.FromArgb(0, 0, 0), 0, 0, ScreenWidth, ScreenHeight, doRefresh);
        }
    }
}
