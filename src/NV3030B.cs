using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Graphics;

namespace Sang.IoT.NV3030B
{
    /// <summary>
    /// NV3030B LCD Display Driver
    /// </summary>
    public partial class NV3030B : GraphicDisplay
    {
        private const int DefaultSPIBufferSize = 0x1000;
        private const byte LcdPortraitConfig = 0x00;

        private readonly int _dcPinId;
        private readonly int _resetPinId;
        private readonly int _backlightPin;
        private readonly int _spiBufferSize;
        private readonly bool _shouldDispose;

        private SpiDevice _spiDevice;
        private GpioController _gpioDevice;
        private GpioDriver _pwm;

        private Rgb565[] _screenBuffer;
        private Rgb565[] _previousBuffer;

        private double _fps;
        private DateTimeOffset _lastUpdate;

        /// <summary>
        /// Initializes new instance of NV3030B device that will communicate using SPI bus.
        /// </summary>
        /// <param name="spiDevice">The SPI device used for communication.</param>
        /// <param name="dataCommandPin">The GPIO pin used for DC (data/command).</param>
        /// <param name="resetPin">The GPIO pin used for RST (reset).</param>
        /// <param name="backlightPin">The pin for backlight PWM control.</param>
        /// <param name="spiBufferSize">Size of SPI buffer.</param>
        /// <param name="gpioController">GPIO controller instance.</param>
        /// <param name="shouldDispose">True to dispose GPIO controller when done.</param>
        public NV3030B(SpiDevice spiDevice, int dataCommandPin, int resetPin, int backlightPin = -1, 
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
                SetBacklight(100);
            }

            ResetDisplayAsync().Wait();
            Initialize();

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

        /// <inheritdoc />
        public override PixelFormat NativePixelFormat => PixelFormat.Format16bppRgb565;

        /// <summary>
        /// Current FPS value
        /// </summary>
        public double Fps => _fps;

        private void Initialize()
        {
            // 完全按照Python代码的初始化序列
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
        /// Set display window for drawing
        /// </summary>
        private void SetWindow(int x0, int y0, int x1, int y1)
        {
            y0 += 20;
            y1 += 20;
            
            SendCommand(NV3030BCommand.ColumnAddressSet);
            SendData(new byte[] { (byte)(x0 >> 8), (byte)x0, (byte)(x1 >> 8), (byte)x1 });
            
            SendCommand(NV3030BCommand.PageAddressSet);
            SendData(new byte[] { (byte)(y0 >> 8), (byte)y0, (byte)(y1 >> 8), (byte)y1 });
            
            SendCommand(NV3030BCommand.MemoryWrite);
        }
        
        /// <summary>
        /// Send command to display
        /// </summary>
        private void SendCommand(NV3030BCommand cmd, params byte[] data)
        {
            Console.WriteLine($"CMD: 0x{(byte)cmd:X2}");
            _gpioDevice.Write(_dcPinId, PinValue.Low);
            _spiDevice.Write(new[] { (byte)cmd });
            
            if (data.Length > 0)
            {
                _gpioDevice.Write(_dcPinId, PinValue.High);
                _spiDevice.Write(data);
                Console.WriteLine($"Data: {BitConverter.ToString(data)}");
            }
        }

        /// <summary>
        /// Send data to display
        /// </summary>
        private void SendData(byte[] data)
        {
            if (data.Length > 32)
            {
                Console.WriteLine($"Data length: {data.Length} bytes");
                Console.WriteLine($"First 32 bytes: {BitConverter.ToString(data, 0, Math.Min(32, data.Length))}");
            }
            else
            {
                Console.WriteLine($"Data: {BitConverter.ToString(data)}");
            }
            _gpioDevice.Write(_dcPinId, PinValue.High);
            _spiDevice.Write(data);
        }

        /// <summary>
        /// Reset display
        /// </summary>
        public async Task ResetDisplayAsync()
        {
            if (_resetPinId < 0) return;

            _gpioDevice.Write(_resetPinId, PinValue.High);
            await Task.Delay(10);
            _gpioDevice.Write(_resetPinId, PinValue.Low);
            await Task.Delay(10);
            _gpioDevice.Write(_resetPinId, PinValue.High);
            await Task.Delay(120);
        }

        /// <summary>
        /// Set backlight brightness (0-100)
        /// </summary>
        public void SetBacklight(int brightness)
        {
            if (_backlightPin == -1)
                throw new InvalidOperationException("Backlight pin not configured");

            brightness = Math.Clamp(brightness, 0, 100);
            _gpioDevice.Write(_backlightPin, brightness > 0 ? PinValue.High : PinValue.Low);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_gpioDevice != null)
                {
                    if (_resetPinId >= 0)
                        _gpioDevice.ClosePin(_resetPinId);
                    if (_backlightPin >= 0)
                        _gpioDevice.ClosePin(_backlightPin);
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

        // Implementation of GraphicDisplay abstract methods
        public override void ClearScreen()
        {
            Array.Clear(_screenBuffer, 0, _screenBuffer.Length);
            SendFrame(true);
        }

        public override bool CanConvertFromPixelFormat(PixelFormat format)
        {
            return format == PixelFormat.Format32bppArgb || 
                   format == PixelFormat.Format32bppXrgb ||
                   format == PixelFormat.Format16bppRgb565;
        }

        public override BitmapImage GetBackBufferCompatibleImage()
        {
            return BitmapImage.CreateBitmap(ScreenWidth, ScreenHeight, PixelFormat.Format32bppArgb);
        }

        /// <summary>
        /// Send frame buffer to display
        /// </summary>
        public void SendFrame(bool forceFull)
        {
            // 移除forceFull判断，强制每次都发送完整帧
            SetWindow(0, 0, ScreenWidth - 1, ScreenHeight - 1);
            _gpioDevice.Write(_dcPinId, PinValue.High);

            var buffer = new byte[_screenBuffer.Length * 2];
            for (int i = 0; i < _screenBuffer.Length; i++)
            {
                var value = _screenBuffer[i].PackedValue;
                // 修改字节顺序与Python代码保持一致
                buffer[i * 2] = (byte)(value & 0xFF);        // 低字节
                buffer[i * 2 + 1] = (byte)((value >> 8) & 0xFF);     // 高字节
            }

            // 使用固定大小块发送数据
            const int chunkSize = 4096;  // 与Python代码使用相同的块大小
            for (int i = 0; i < buffer.Length; i += chunkSize)
            {
                int length = Math.Min(chunkSize, buffer.Length - i);
                var chunk = buffer.AsSpan(i, length);
                _spiDevice.Write(chunk);
                
                // 打印第一块数据用于调试
                if (i == 0)
                {
                    Console.WriteLine($"First chunk data: {BitConverter.ToString(chunk.ToArray())}");
                }
            }

            Array.Copy(_screenBuffer, _previousBuffer, _screenBuffer.Length);
            UpdateFps();
        }

        private void UpdateFps()
        {
            var now = DateTimeOffset.UtcNow;
            var ts = now - _lastUpdate;
            if (ts <= TimeSpan.FromMilliseconds(1))
            {
                ts = TimeSpan.FromMilliseconds(1);
            }
            _fps = 1.0 / ts.TotalSeconds;
            _lastUpdate = now;
        }
    }
}