﻿
using Iot.Device.Graphics;
using Iot.Device.Graphics.SkiaSharpAdapter;
using Sang.IoT.NV3030B;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Spi;

class Program
{
#if LUCK_FOX
    const int pinID_DC = 32;
    const int pinID_Reset = 33;
    const int pinID_BL = 40;
#else
    const int pinID_DC = 25;
    const int pinID_Reset = 27;
    const int pinID_BL = 18;
#endif
    static async Task Main(string[] args)
    {
        SkiaSharpAdapter.Register();

        try
        {
            Console.WriteLine("Initializing display...");

            SpiDevice displaySPI = SpiDevice.Create(new SpiConnectionSettings(0, 0)
            {
                Mode = SpiMode.Mode0,
                DataBitLength = 8,
                ClockFrequency = 40_000_000
            });
#if LUCK_FOX
            // LUCK_FOX 需要使用 SysFsDriver 驱动 GPIO
            var gpioController = new GpioController(PinNumberingScheme.Logical, new SysFsDriver());
            using var display = new NV3030B(displaySPI, pinID_DC, pinID_Reset, pinID_BL, gpioController: gpioController);
#else
            using var display = new NV3030B(displaySPI, pinID_DC, pinID_Reset, pinID_BL);
#endif

            // 颜色测试
            Console.WriteLine("Testing basic colors...");
            display.ClearScreen(System.Drawing.Color.Red, true);
            Task.Delay(200).Wait();
            display.ClearScreen(System.Drawing.Color.Green, true);
            Task.Delay(200).Wait();
            display.ClearScreen(System.Drawing.Color.Blue, true);
            Task.Delay(200).Wait();
            display.ClearScreen(System.Drawing.Color.White, true);
            Task.Delay(200).Wait();
            display.ClearScreen(System.Drawing.Color.Black, true);
            Task.Delay(200).Wait();
            display.ClearScreen(System.Drawing.Color.Yellow, true);
            Task.Delay(200).Wait();
            display.ClearScreen(System.Drawing.Color.Cyan, true);
            Task.Delay(200).Wait();
            display.ClearScreen(System.Drawing.Color.Magenta, true);
            Task.Delay(200).Wait();
            display.ClearScreen(System.Drawing.Color.Gray, true);
            Task.Delay(200).Wait();

            Console.WriteLine("Testing basic graphics...");

            // 测试基本图形
            display.ClearScreen(System.Drawing.Color.Red, true);
            Console.WriteLine("Testing fill rectangle...");
            display.FillRect(System.Drawing.Color.Blue, 0, 0, 100, 100);
            display.FillRect(System.Drawing.Color.Green, 100, 0, 100, 100);
            display.SendFrame(false);

            Task.Delay(5000).Wait();

            Console.WriteLine("Testing clear screen...");
            display.ClearScreen(true);
            // fps
            Console.WriteLine("fps: " + display.Fps);

            Console.WriteLine("Testing image display...");

            // 测试图片显示
            if (args != null && args.Length > 0)
            {
                await TestImage(display, args[0]);
            }
            else
            {
                await TestImage(display);
            }
            Task.Delay(5000).Wait();

            // 亮度
            Console.WriteLine("Testing backlight...");
            display.SetBacklight(20);
            Task.Delay(500).Wait();
            display.SetBacklight(50);
            Task.Delay(500).Wait();
            display.SetBacklight(100);

            Task.Delay(5000).Wait();

            // 局部刷新
            Console.WriteLine("Testing partial update...");
            display.FillRect(System.Drawing.Color.Azure, 100, 0, 100, 100);
            display.SendFrame(false);

            Console.WriteLine("All tests completed.");

            Task.Delay(2000).Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }


    private static async Task TestImage(NV3030B display, string file = "LCD_1inch5.jpg")
    {

        try
        {
            using var image = BitmapImage.CreateFromFile(file);
            display.DrawBitmap(image);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load or display image: {ex.Message}");
        }
    }
}