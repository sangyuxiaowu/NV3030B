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

    const int testAwait = 2000;
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
            await TestBasicColors(display);

            // 测试基本图形
            await TestBasicGraphics(display);

            // 测试图片显示
            if (args != null && args.Length > 0)
            {
                await TestImage(display, args[0]);
            }
            else
            {
                await TestImage(display);
            }
            await Task.Delay(testAwait);

            // 亮度测试
            await TestBacklight(display);

            // 局部刷新测试
            await TestPartialUpdate(display);

            Console.WriteLine("All tests completed.");

            await Task.Delay(testAwait);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static async Task TestBasicColors(NV3030B display)
    {
        Console.WriteLine("Testing basic colors...");
        display.ClearScreen(System.Drawing.Color.Red, true);
        await Task.Delay(200);
        display.ClearScreen(System.Drawing.Color.Green, true);
        await Task.Delay(200);
        display.ClearScreen(System.Drawing.Color.Blue, true);
        await Task.Delay(200);
        display.ClearScreen(System.Drawing.Color.White, true);
        await Task.Delay(200);
        display.ClearScreen(System.Drawing.Color.Black, true);
        await Task.Delay(200);
        display.ClearScreen(System.Drawing.Color.Yellow, true);
        await Task.Delay(200);
        display.ClearScreen(System.Drawing.Color.Cyan, true);
        await Task.Delay(200);
        display.ClearScreen(System.Drawing.Color.Magenta, true);
        await Task.Delay(200);
        display.ClearScreen(System.Drawing.Color.Gray, true);
        await Task.Delay(200);
    }

    private static async Task TestBasicGraphics(NV3030B display)
    {
        Console.WriteLine("Testing basic graphics...");
        display.ClearScreen(System.Drawing.Color.Red, true);
        Console.WriteLine("Testing fill rectangle...");
        display.FillRect(System.Drawing.Color.Blue, 0, 0, 100, 100);
        display.FillRect(System.Drawing.Color.Green, 100, 0, 100, 100);
        display.SendFrame(false);
        await Task.Delay(testAwait);
        Console.WriteLine("Testing clear screen...");
        display.ClearScreen(true);
    }

    private static async Task TestBacklight(NV3030B display)
    {
        Console.WriteLine("Testing backlight...");
        display.SetBacklight(20);
        await Task.Delay(500);
        display.SetBacklight(50);
        await Task.Delay(500);
        display.SetBacklight(100);
        await Task.Delay(testAwait);
    }

    private static async Task TestPartialUpdate(NV3030B display)
    {
        Console.WriteLine("Testing partial update...");
        using var image = BitmapImage.CreateFromFile("LCD_1inch5.jpg");
        display.DrawBitmap(image, new System.Drawing.Point(0, 100), new System.Drawing.Rectangle(0, 0, 100, 100), true);
        await Task.Delay(testAwait);
    }

    private static async Task TestImage(NV3030B display, string file = "LCD_1inch5.jpg")
    {
        Console.WriteLine("Testing image display...");
        try
        {
            using var image = BitmapImage.CreateFromFile(file);
            display.DrawBitmap(image);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load or display image: {ex.Message}");
        }
        await Task.Delay(testAwait);
    }
}