
using Iot.Device.Graphics;
using Iot.Device.Graphics.SkiaSharpAdapter;
using Sang.IoT.NV3030B;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Spi;
using System.Drawing;

class Program
{
    const int pinID_DC = 25;
    const int pinID_Reset = 27;
    const int pinID_BL = 18;

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

            var gpioController = new GpioController(PinNumberingScheme.Logical, new SysFsDriver());
            using var display = new NV3030B(displaySPI, pinID_DC, pinID_Reset, pinID_BL);

            Console.WriteLine("Testing basic graphics...");

            // 测试基本图形
            display.ClearScreen(Color.Red, true);
            Task.Delay(10000).Wait();
            display.FillRect(Color.Blue, 0, 0, 100, 100);
            display.FillRect(Color.Green, 100, 0, 100, 100);

            display.SendFrame(false);

            Task.Delay(10000).Wait();

            Console.WriteLine("Testing image display...");

            // 测试图片显示
            if(args != null && args.Length > 0)
            {
                await TestImage(display, args[0]);
            }
            else
            {
                await TestImage(display);
            }

            Task.Delay(10000).Wait();

            Console.WriteLine("Testing bin display...");

            await TestBin(display);

            Console.WriteLine("All tests completed.");

            Task.Delay(50000).Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }


    private static async Task TestImage(NV3030B display, string file = "LCD_1inch5.jpg")
    {
        Console.WriteLine("Testing image display");

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
    private static async Task TestBin(NV3030B display)
    {
        Console.WriteLine("Testing image display");

        try
        {
            // 读取bin文件
            using var fs = new FileStream("pix.bin", FileMode.Open);
            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            display.SendBitmapPixelData(buffer, new System.Drawing.Rectangle(0,0,240,280));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load or display image: {ex.Message}");
        }
    }

}