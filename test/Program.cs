using Iot.Device.Graphics;
using Iot.Device.Graphics.SkiaSharpAdapter;
using Sang.IoT.NV3030B;
using System.Device.Spi;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Drawing;
using SkiaSharp;

class Program
{
    const int pinID_DC = 42;
    const int pinID_Reset = 33;
    const int pinID_BL = 40;

    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Initializing display...");
            
            SkiaSharpAdapter.Register();
            SpiDevice displaySPI = SpiDevice.Create(new SpiConnectionSettings(0, 0) 
            { 
                Mode = SpiMode.Mode0, 
                DataBitLength = 8, 
                ClockFrequency = 40_000_000 
            });

            var gpioController = new GpioController(PinNumberingScheme.Logical, new SysFsDriver());
            using var display = new NV3030B(displaySPI, pinID_DC, pinID_Reset, pinID_BL, gpioController: gpioController);

            Console.WriteLine("Testing basic graphics...");

            // 测试基本图形
            await TestBasicGraphics(display);

            // 测试图片显示
            await TestSkiaImage(display);

            Console.WriteLine("All tests completed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private static async Task TestBasicGraphics(NV3030B display)
    {
        // 创建SkiaSharp图像
        using var surface = SKSurface.Create(new SKImageInfo(display.ScreenWidth, display.ScreenHeight));
        var canvas = surface.Canvas;

        // 测试基本的纯红色填充
        canvas.Clear(SKColors.Red);
        using (var image = surface.Snapshot())
        using (var data = image.Encode())
        using (var stream = new MemoryStream(data.ToArray()))
        {
            var bitmapImage = BitmapImage.CreateFromStream(stream);
            display.DrawBitmap(bitmapImage);
        }
        await Task.Delay(2000);
    }

    private static async Task TestSkiaImage(NV3030B display)
    {
        Console.WriteLine("Testing image display");
        
        try
        {
            // 使用SkiaSharp加载图片
            using var bitmap = SKBitmap.Decode("LCD_1inch5.jpg");
            if (bitmap != null)
            {
                Console.WriteLine($"Image loaded: {bitmap.Width}x{bitmap.Height}");
                
                // 调整图片大小以适应屏幕
                var imageInfo = new SKImageInfo(display.ScreenWidth, display.ScreenHeight);
                using var scaled = bitmap.Resize(imageInfo, SKFilterQuality.High);
                using var image = SKImage.FromBitmap(scaled);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = new MemoryStream(data.ToArray());
                
                var bitmapImage = BitmapImage.CreateFromStream(stream);
                display.DrawBitmap(bitmapImage);
                
                Console.WriteLine("Image drawn to display");
                await Task.Delay(2000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load or display image: {ex.Message}");
        }
    }
}