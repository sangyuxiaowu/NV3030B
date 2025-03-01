using Iot.Device.Graphics;
using System.Device.Gpio;
using System.Device.Pwm.Drivers;
using System.Device.Spi;
using System.Drawing;

namespace Sang.IoT.NV3030B
{
    /// <summary>
    /// NV3030B LCD Display Driver
    /// </summary>
    public partial class NV3030B : GraphicDisplay
    {
        private const int DefaultSPIBufferSize = 0x1000;

        private readonly int _dcPinId;
        private readonly int _resetPinId;
        private readonly int _backlightPin;
        private readonly int _spiBufferSize;
        private readonly bool _shouldDispose;

        private SpiDevice _spiDevice;
        private GpioController _gpioDevice;
        private SoftwarePwmChannel? _backlightChannel;

        private Rgb565[] _screenBuffer;
        private Rgb565[] _previousBuffer;

        private double _fps;
        private DateTimeOffset _lastUpdate;

        /// <summary>
        /// Initializes new instance of NV3030B device that will communicate using SPI bus.
        /// </summary>
        /// <param name="spiDevice">The SPI device used for communication. This Spi device will be displayed along with the NV3030B device.</param>
        /// <param name="dataCommandPin">The id of the GPIO pin used to control the DC line (data/command). This pin must be provided.</param>
        /// <param name="resetPin">The id of the GPIO pin used to control the /RESET line (RST). Can be -1 if not connected</param>
        /// <param name="backlightPin">The pin for turning the backlight on and off, or -1 if not connected.</param>
        /// <param name="backlight_frequency">The frequency of the backlight pin. The default value is 1000.</param>
        /// <param name="spiBufferSize">The size of the SPI buffer. If data larger than the buffer is sent then it is split up into multiple transmissions. The default value is 4096.</param>
        /// <param name="gpioController">The GPIO controller used for communication and controls the the <paramref name="resetPin"/> and the <paramref name="dataCommandPin"/>
        /// If no Gpio controller is passed in then a default one will be created and disposed when NV3030B device is disposed.</param>
        /// <param name="shouldDispose">True to dispose the Gpio Controller when done</param>
        public NV3030B(SpiDevice spiDevice, int dataCommandPin, int resetPin, int backlightPin = -1, int backlight_frequency = 1000,
            int spiBufferSize = DefaultSPIBufferSize, GpioController? gpioController = null, bool shouldDispose = true)
        {
            if (spiBufferSize <= 0)
            {
                throw new ArgumentException("Buffer size must be larger than 0.", nameof(spiBufferSize));
            }

            _spiDevice = spiDevice;
            _dcPinId = dataCommandPin;
            _resetPinId = resetPin;
            _backlightPin = backlightPin;
            _gpioDevice = gpioController ?? new GpioController();
            _shouldDispose = shouldDispose || gpioController is null;
            _fps = 0;
            _lastUpdate = DateTimeOffset.UtcNow;

            _gpioDevice.OpenPin(_dcPinId, PinMode.Output);
            if (_resetPinId >= 0)
            {
                _gpioDevice.OpenPin(_resetPinId, PinMode.Output);
            }

            _spiBufferSize = spiBufferSize;

            if (_backlightPin != -1)
            {
                _gpioDevice.OpenPin(_backlightPin, PinMode.Output);
                _backlightChannel = new SoftwarePwmChannel(backlightPin, backlight_frequency);
                _backlightChannel.Start();
                SetBacklight(100);
            }

            ResetDisplayAsync().Wait();
            InitDisplayParameters();

            _screenBuffer = new Rgb565[ScreenWidth * ScreenHeight];
            _previousBuffer = new Rgb565[ScreenWidth * ScreenHeight];

            // Clear display
            SendFrame(true);
        }

        /// <summary>
        /// Screen width in pixels
        /// </summary>
        public override int ScreenWidth => 240;

        /// <summary>
        /// Screen height in pixels
        /// </summary>
        public override int ScreenHeight => 280;

        /// <summary>
        /// Current FPS value
        /// </summary>
        public double Fps => _fps;

        public override PixelFormat NativePixelFormat => PixelFormat.Format16bppRgb565;

        /// <summary>
        /// Configure memory and orientation parameters
        /// </summary>
        protected virtual void InitDisplayParameters()
        {

            SendCommand(NV3030BCommand.MemoryAccessControl, 0x00);  // 0x36

            SendCommand(NV3030BCommand.ConfigurationRegister, 0x06, 0x08);  // 0xfd

            SendCommand(NV3030BCommand.DisplaySetting1, 0x07, 0x04);  // 0x61

            SendCommand(NV3030BCommand.DisplaySetting2, 0x00, 0x44, 0x45);  // 0x62

            SendCommand(NV3030BCommand.DisplaySetting3, 0x41, 0x07, 0x12, 0x12);  // 0x63

            SendCommand(NV3030BCommand.DisplaySetting4, 0x37);  // 0x64

            SendCommand(NV3030BCommand.PumpSetting1, 0x09, 0x10, 0x21);  // 0x65, Pump1=4.7MHz PUMP1 VSP

            SendCommand(NV3030BCommand.PumpSetting2, 0x09, 0x10, 0x21);  // 0x66, pump=2 AVCL

            SendCommand(NV3030BCommand.PumpSelect, 0x21, 0x40);  // 0x67, pump_sel

            SendCommand(NV3030BCommand.GammaVapVan, 0x90, 0x4c, 0x50, 0x70);  // 0x68, gamma vap/van

            SendCommand(NV3030BCommand.FrameRate, 0x0F, 0x02, 0x01);  // 0xb1

            SendCommand(NV3030BCommand.LayoutControl, 0x01);  // 0xB4, 01:1dot 00:column

            // Porch setting
            SendCommand(NV3030BCommand.PorchSetting, 0x02, 0x02, 0x0a, 0x14);  // 0xB5

            SendCommand(NV3030BCommand.GateControl, 0x04, 0x01, 0x9f, 0x00, 0x02);  // 0xB6

            // Gamma settings
            SendCommand(NV3030BCommand.GammaSelect, 0x11);  // 0xdf, gofc_gamma_en_sel=1

            SendCommand(NV3030BCommand.GammaValue1, 0x03, 0x00, 0x00, 0x30, 0x33, 0x3f);  // 0xE2
            SendCommand(NV3030BCommand.GammaValue2, 0x3f, 0x33, 0x30, 0x00, 0x00, 0x03);  // 0xE5
            SendCommand(NV3030BCommand.GammaValue3, 0x05, 0x67);  // 0xE1
            SendCommand(NV3030BCommand.GammaValue4, 0x67, 0x06);  // 0xE4
            SendCommand(NV3030BCommand.GammaValue5, 0x05, 0x06, 0x0A, 0x0C, 0x0B, 0x0B, 0x13, 0x19);  // 0xE0
            SendCommand(NV3030BCommand.GammaValue6, 0x18, 0x13, 0x0D, 0x09, 0x0B, 0x0B, 0x05, 0x06);  // 0xE3

            // Source settings
            SendCommand(NV3030BCommand.SourceControl1, 0x00, 0xff);  // 0xE6
            SendCommand(NV3030BCommand.SourceControl2, 0x01, 0x04, 0x03, 0x03, 0x00, 0x12);  // 0xE7
            SendCommand(NV3030BCommand.SourceControl3, 0x00, 0x70, 0x00);  // 0xE8

            // Gate control
            SendCommand(NV3030BCommand.GateControl2, 0x52);  // 0xEc

            // Display controls
            SendCommand(NV3030BCommand.DisplayControl1, 0x01, 0x01, 0x02);  // 0xF1
            SendCommand(NV3030BCommand.DisplayControl2, 0x01, 0x30, 0x00, 0x00);  // 0xF6

            // Final configurations
            SendCommand(NV3030BCommand.ConfigurationRegister, 0xfa, 0xfc);  // 0xfd

            // 设置色彩模式为16位RGB565
            SendCommand(NV3030BCommand.ColMod, 0x55);  // 0x3a, 16-bit/pixel

            SendCommand(NV3030BCommand.TearingEffectLine, 0x00);  // 0x35

            SendCommand(NV3030BCommand.NormalDisplay);  // 0x21

            SendCommand(NV3030BCommand.SleepOut);  // 0x11

            SendCommand(NV3030BCommand.DisplayOn);  // 0x29
        }


        /// <summary>
        /// This device supports standard 32 bit formats as input
        /// </summary>
        /// <param name="format">The format to query</param>
        /// <returns>True if it is supported, false if not</returns>
        public override bool CanConvertFromPixelFormat(PixelFormat format)
        {
            return format == PixelFormat.Format32bppXrgb || format == PixelFormat.Format32bppArgb;
        }

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

        /// <summary>
        /// Immediately clears the screen to black.
        /// </summary>
        public override void ClearScreen()
        {
            ClearScreen(true);
        }

        /// <summary>
        /// Reset display
        /// </summary>
        public async Task ResetDisplayAsync()
        {
            if (_resetPinId < 0) return;

            _gpioDevice.Write(_resetPinId, PinValue.High);
            await Task.Delay(10).ConfigureAwait(false);
            _gpioDevice.Write(_resetPinId, PinValue.Low);
            await Task.Delay(10).ConfigureAwait(false);
            _gpioDevice.Write(_resetPinId, PinValue.High);
            await Task.Delay(10).ConfigureAwait(false);
        }


        /// <summary>
        /// Set backlight brightness (0-100)
        /// </summary>
        public void SetBacklight(int brightness)
        {
            if (_backlightChannel is null)
                throw new InvalidOperationException("Backlight pin not configured");

            if (brightness < 0 || brightness > 100)
                throw new ArgumentOutOfRangeException(nameof(brightness), "Brightness must be between 0 and 100");

            double dutyCycle = brightness / 100.0;
            _backlightChannel.DutyCycle = dutyCycle;
        }


        /// <summary>
        /// Set display window for drawing
        /// </summary>
        private void SetWindow(int x0, int y0, int x1, int y1)
        {
            y0 += 20;
            y1 += 20;

            SendCommand(NV3030BCommand.ColumnAddressSet);
            SendData(new byte[] { (byte)(x0 >> 8), (byte)x0, (byte)((x1 - 1) >> 8), (byte)(x1 - 1) });

            SendCommand(NV3030BCommand.PageAddressSet);
            SendData(new byte[] { (byte)(y0 >> 8), (byte)y0, (byte)((y1 - 1) >> 8), (byte)(y1 - 1) });

            SendCommand(NV3030BCommand.MemoryWrite);
        }

        /// <summary>
        /// Send a command to the the display controller along with associated parameters.
        /// </summary>
        /// <param name="command">Command to send.</param>
        /// <param name="commandParameters">parameteters for the command to be sent</param>
        internal void SendCommand(NV3030BCommand command, params byte[] commandParameters)
        {
            SendCommand(command, commandParameters.AsSpan());
        }

        /// <summary>
        /// Send a command to the the display controller along with parameters.
        /// </summary>
        /// <param name="command">Command to send.</param>
        /// <param name="data">Span to send as parameters for the command.</param>
        internal void SendCommand(NV3030BCommand command, Span<byte> data)
        {
            Span<byte> commandSpan = stackalloc byte[]
            {
                (byte)command
            };

            SendSPI(commandSpan, true);

            if (data != null && data.Length > 0)
            {
                SendSPI(data);
            }
        }

        /// <summary>
        /// Send data to the display controller.
        /// </summary>
        /// <param name="data">The data to send to the display controller.</param>
        private void SendData(Span<byte> data)
        {
            SendSPI(data, blnIsCommand: false);
        }

        /// <summary>
        /// Write a block of data to the SPI device
        /// </summary>
        /// <param name="data">The data to be sent to the SPI device</param>
        /// <param name="blnIsCommand">A flag indicating that the data is really a command when true or data when false.</param>
        private void SendSPI(Span<byte> data, bool blnIsCommand = false)
        {
            int index = 0;
            int len;

            // set the DC pin to indicate if the data being sent to the display is DATA or COMMAND bytes.
            _gpioDevice.Write(_dcPinId, blnIsCommand ? PinValue.Low : PinValue.High);

            // write the array of bytes to the display. (in chunks of SPI Buffer Size)
            do
            {
                // calculate the amount of spi data to send in this chunk
                len = Math.Min(data.Length - index, _spiBufferSize);
                // send the slice of data off set by the index and of length len.
                _spiDevice.Write(data.Slice(index, len));
                // add the length just sent to the index
                index += len;
            }
            while (index < data.Length); // repeat until all data sent.
        }

        /// <inheritdoc />
        public override BitmapImage GetBackBufferCompatibleImage()
        {
            return BitmapImage.CreateBitmap(ScreenWidth, ScreenHeight, PixelFormat.Format32bppArgb);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_gpioDevice != null)
                {
                    if (_resetPinId >= 0)
                        _gpioDevice.ClosePin(_resetPinId);
                    if (_backlightPin >= 0)
                    {
                        _backlightChannel?.Stop();
                        _backlightChannel?.Dispose();
                        _gpioDevice.ClosePin(_backlightPin);
                    }
                    if (_dcPinId >= 0)
                        _gpioDevice.ClosePin(_dcPinId);
                    if (_shouldDispose)
                        _gpioDevice.Dispose();
                    _gpioDevice = null;
                }

                _spiDevice?.Dispose();
                _spiDevice = null;
            }
        }
    }
}