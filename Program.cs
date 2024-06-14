using PicarX;
using System.Device.Gpio;

Console.WriteLine("Blinking LED. Press Ctrl+C to end.");
int pin = 26;

using var controller = new GpioController();
controller.OpenPin(pin, PinMode.Output);
bool ledOn = true;
var motor = new Motor();
while (true)
{
    controller.Write(pin, ledOn ? PinValue.High : PinValue.Low);
    Thread.Sleep(1000);
    ledOn = !ledOn;

    motor.Wheel(100);
    Thread.Sleep(1000);
    motor.Wheel(0);
    Thread.Sleep(1000);
    motor.Wheel(-100);
    Thread.Sleep(1000);
    motor.Wheel(-50);
    Thread.Sleep(1000);
    motor.Wheel(0);
    Thread.Sleep(1000);

}