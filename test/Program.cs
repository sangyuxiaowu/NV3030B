
using Iot.Device.Graphics;
using Iot.Device.Graphics.SkiaSharpAdapter;
using Sang.IoT.NV3030B;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Spi;

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
            display.ClearScreen(System.Drawing.Color.Red, true);
            Task.Delay(10000).Wait();
            display.FillRect(System.Drawing.Color.Blue, 0, 0, 100, 100);
            display.FillRect(System.Drawing.Color.Green, 100, 0, 100, 100);

            display.SendFrame(false);

            Task.Delay(10000).Wait();

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

            //Task.Delay(10000).Wait();
            //
            //Console.WriteLine("Testing bin display...");
            //
            //await TestBin(display);
            //
            //Task.Delay(10000).Wait();
            //
            //Console.WriteLine("Testing ImageSharp display...");
            //
            //await TestImageSharp(display);
            //
            //Console.WriteLine("All tests completed.");

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

        try
        {
            // 读取bin文件
            using var fs = new FileStream("pix.bin", FileMode.Open);
            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, buffer.Length);
            display.SendBitmapPixelData(buffer, new System.Drawing.Rectangle(0, 0, 240, 280));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load or display image: {ex.Message}");
        }
    }

    private static async Task TestImageSharp(NV3030B display)
    {

        try
        {
            using var image = SixLabors.ImageSharp.Image.Load<Bgra32>("LCD_1inch5.jpg");
            using Image<Bgr24> converted = image.CloneAs<Bgr24>();
            var pix = new byte[display.ScreenHeight * display.ScreenWidth * 2];
            for (int y = 0; y < display.ScreenHeight; y++)
            {
                for (int x = 0; x < display.ScreenWidth; x++)
                {
                    var color = image[x, y];
                    pix[(y * display.ScreenWidth + x) * 2] = (byte)((color.R & 0xF8) | (color.G >> 5));
                    pix[(y * display.ScreenWidth + x) * 2 + 1] = (byte)(((color.G << 3) & 0xE0) | (color.B >> 3));
                }
            }
            display.SendBitmapPixelData(pix, new System.Drawing.Rectangle(0, 0, display.ScreenWidth, display.ScreenHeight));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load or display image: {ex.Message}");
        }
    }

}