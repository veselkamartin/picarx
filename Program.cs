using PicarX;
using System.Device.Gpio;
using System.Device.I2c;

Console.WriteLine("Blinking LED. Press Ctrl+C to end.");

Utils.reset_mcu();


var dir_servo_pin = new Servo(new PWM("P2"));
dir_servo_pin.SetAngle(-20);
Thread.Sleep(3000);
dir_servo_pin.SetAngle(20);
Thread.Sleep(3000);
dir_servo_pin.SetAngle(-10);
Thread.Sleep(3000);
dir_servo_pin.SetAngle(10);
Thread.Sleep(3000);
var motor = new Motor();
motor.Wheel(75, 1);
Thread.Sleep(3000);
motor.Wheel(0, 1);
motor.Wheel(50, 0);
Thread.Sleep(3000);
var cam_pan = new Servo(new PWM("P0"));
var cam_tilt = new Servo(new PWM("P1"));
cam_pan.SetAngle(20);
cam_tilt.SetAngle(-20);
Thread.Sleep(3000);
motor.Wheel(0, 0);
Console.WriteLine("Done");
int pin = 26;

