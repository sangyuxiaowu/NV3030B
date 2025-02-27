
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Spi;
using Sang.IoT.NV3030B;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

class Program
{
    const int pinID_DC = 25;
    const int pinID_Reset = 27;
    const int pinID_BL = 18;

    static async Task Main(string[] args)
    {
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
            using var display = new NV3030B(displaySPI, pinID_DC, pinID_Reset, pinID_BL, gpioController: gpioController);

            Console.WriteLine("Testing basic graphics...");

            // 测试基本图形
            // await TestBasicGraphics(display);

            // 测试图片显示
            await TestSkiaImage(display);

            Console.WriteLine("All tests completed.");

            Task.Delay(5000).Wait();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    
    private static async Task TestSkiaImage(NV3030B display)
    {
        Console.WriteLine("Testing image display");
        
        try
        {
           using (Image<Bgra32> image2inch4 = Image.Load<Bgra32>("LCD_1inch5.jpg"))
            {
                using Image<Bgr24> converted2inch4Image = image2inch4.CloneAs<Bgr24>();
                display.ShowImage(converted2inch4Image);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load or display image: {ex.Message}");
        }
    }
}