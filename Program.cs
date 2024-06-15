﻿using PicarX;
using System.Device.Gpio;
using System.Device.I2c;
using System.Threading.Channels;

Console.WriteLine("Blinking LED. Press Ctrl+C to end.");
int pin = 26;

using var controller = new GpioController();
controller.OpenPin(pin, PinMode.Output);
bool ledOn = true;
//var motor = new Motor();

Console.WriteLine("Creating bus");
var _bus = I2cBus.Create(1); // Initialize your I2C bus here
Console.WriteLine("Creating device");
var _device = _bus.CreateDevice(0x14);


while (true)
{
    controller.Write(pin, ledOn ? PinValue.High : PinValue.Low);
    Thread.Sleep(1000);
    ledOn = !ledOn;
    Console.WriteLine("Device test 1");
    _device.Write([0x21, 0x4F, 0x01]);
    Thread.Sleep(1000);
    Console.WriteLine("Device test 2");
    _device.Write([0x21, 0x1F, 0x01]);
    Thread.Sleep(1000);
    Console.WriteLine("Device test 3");
    _device.Write([0x21, 0x01, 0x4F]);
    Thread.Sleep(1000);
    Console.WriteLine("Device test 4");
    _device.Write([0x21, 0x01, 0x1F]);
    Thread.Sleep(1000);
    Console.WriteLine("Device test 5");
    _device.Write([0x4F, 0x01, 0x21]);
    Thread.Sleep(1000);
    Console.WriteLine("Device test 6");
    _device.Write([0x1F, 0x01, 0x21]);
    //motor.Wheel(100);
    //Thread.Sleep(1000);
    //motor.Wheel(0);
    //Thread.Sleep(1000);
    //motor.Wheel(-100);
    //Thread.Sleep(1000);
    //motor.Wheel(-50);
    //Thread.Sleep(1000);
    //motor.Wheel(0);
    //Thread.Sleep(1000);


}