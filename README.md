# Sang.IoT.NV3030B

NV3030B LCD Controller

## Introduction

This is a library for the NV3030B LCD controller. It is used in the Waveshare 1.5inch LCD Module.

This library is based on the [Ili934x](https://github.com/dotnet/iot/tree/main/src/devices/Ili934x?wt.mc_id=DT-MVP-5005195) library.

## Installation

You can install this library:

```bash
dotnet add package Sang.IoT.NV3030B
```

## Usage

```csharp
using Iot.Device.Graphics;
using Iot.Device.Graphics.SkiaSharpAdapter;
using Sang.IoT.NV3030B;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.Spi;

// Setup the GPIO
const int pinID_DC = 25;
const int pinID_Reset = 27;
const int pinID_BL = 18;

SkiaSharpAdapter.Register();

// Setup the SPI
SpiDevice displaySPI = SpiDevice.Create(new SpiConnectionSettings(0, 0)
{
    Mode = SpiMode.Mode0,
    DataBitLength = 8,
    ClockFrequency = 40_000_000
});

// Create the display
using var display = new NV3030B(displaySPI, pinID_DC, pinID_Reset, pinID_BL);

// Show a bitmap
using var image = BitmapImage.CreateFromFile("image.jpg");
display.DrawBitmap(image);
```

## Hardware

The NV3030B is a LCD controller used in the Waveshare 1.5inch LCD Module. It is connected to the Raspberry Pi using SPI.

## References

- [Waveshare 1.5inch LCD Module](https://www.waveshare.com/wiki/1.5inch_LCD_Module)
- [Ili934x](https://github.com/dotnet/iot/tree/main/src/devices/Ili934x?wt.mc_id=DT-MVP-5005195)