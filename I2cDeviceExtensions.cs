﻿namespace PicarX;

public static class I2cDeviceExtensions
{
	public static void WriteWord(this System.Device.I2c.I2cDevice device, byte regAddress, int data)
	{
		if (data < 0) throw new ArgumentOutOfRangeException("data", "Word data cannot be negative");
		if (data > ushort.MaxValue) throw new ArgumentOutOfRangeException("data", $"Word data cannot more than {ushort.MaxValue}");
		var word = (ushort)data;
		WriteWord(device, regAddress, word);
	}
	public static void WriteWord(this System.Device.I2c.I2cDevice device, byte regAddress, ushort data)
	{
		byte valueH = (byte)(data >> 8);
		byte valueL = (byte)(data & 0xff);
		//Console.WriteLine($"i2c write to 0x{device.ConnectionSettings.DeviceAddress:X2}: [0x{regAddress:X2}, 0x{valueH:X2}, 0x{valueL:X2}]");
		device.Write([regAddress, valueH, valueL]);
	}
}
