namespace PicarX
{
    public static class I2cDeviceExtensions
    {
        public static void WriteWord(this System.Device.I2c.I2cDevice device, byte regAddress, ushort data)
        {
            byte valueH = (byte)(data >> 8);
            byte valueL = (byte)(data & 0xff);
            Console.WriteLine($"i2c write: [0x{device.ConnectionSettings.DeviceAddress:X2}, 0x{regAddress:X2}, 0x{valueH:X2}, 0x{valueL:X2}]");
            device.Write([regAddress, valueH, valueL]);

        }
    }
}
