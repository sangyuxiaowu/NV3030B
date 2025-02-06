using System;
using System.Device.Gpio;

namespace Sang.IoT.NV3030B
{
    internal enum NV3030BCommand : byte
    {
        MemoryAccessControl = 0x36,
        ConfigurationRegister = 0xfd,
        DisplaySetting1 = 0x61,
        DisplaySetting2 = 0x62,
        DisplaySetting3 = 0x63,
        DisplaySetting4 = 0x64,
        PumpSetting1 = 0x65,
        PumpSetting2 = 0x66,
        PumpSelect = 0x67,
        GammaVapVan = 0x68,
        FrameRate = 0xb1,
        LayoutControl = 0xB4,
        PorchSetting = 0xB5,
        GateControl = 0xB6,
        GammaSelect = 0xdf,
        GammaValue1 = 0xE2,
        GammaValue2 = 0xE5,
        GammaValue3 = 0xE1,
        GammaValue4 = 0xE4,
        GammaValue5 = 0xE0,
        GammaValue6 = 0xE3,
        SourceControl1 = 0xE6,
        SourceControl2 = 0xE7,
        SourceControl3 = 0xE8,
        GateControl2 = 0xEc,
        DisplayControl1 = 0xF1,
        DisplayControl2 = 0xF6,
        ColMod = 0x3a,
        TearingEffectLine = 0x35,
        NormalDisplay = 0x21,
        SleepOut = 0x11,
        DisplayOn = 0x29,
        ColumnAddressSet = 0x2A,
        PageAddressSet = 0x2B,
        MemoryWrite = 0x2C
    }
}
