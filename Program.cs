using PicarX;
using System.Device.Gpio;
using System.Device.I2c;

Console.WriteLine("Blinking LED. Press Ctrl+C to end.");
int pin = 26;

using var controller = new GpioController();
controller.OpenPin(pin, PinMode.Output);
bool ledOn = true;
//var motor = new Motor();
Console.WriteLine("Reset");
Utils.reset_mcu();
Console.WriteLine("Creating bus");
var _bus = I2cBus.Create(1); // Initialize your I2C bus here
Console.WriteLine("Creating device");
var _device = _bus.CreateDevice(0x14);
var GPI12=controller.OpenPin(12, PinMode.Input);
var boardType = GPI12.Read();
Console.WriteLine($"BoardType: {boardType}");

var GPIO5=controller.OpenPin(5, PinMode.Output);
var GPIO2 = controller.OpenPin(2, PinMode.Output);
var GPI23 = controller.OpenPin(23, PinMode.Output);
var GPI24 = controller.OpenPin(24, PinMode.Output);

//2024 - 06-16 23:11:37,239 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,247 [INFO]  Pin init finished.
//2024-06-16 23:11:37,248 [INFO]  
//GPIO5.Write(0);
//Thread.Sleep(100);
////2024-06-16 23:11:37,258 [INFO]  
//GPIO5.Write(1);
//Thread.Sleep(100);
//2024-06-16 23:11:37,478 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,506 [DEBUG] Conneceted i2c device: ['0x14']
//address: 0x14
//bus: 1
//2024-06-16 23:11:37,508 [DEBUG] prescaler: 1200, period: 1200
//2024-06-16 23:11:37,508 [DEBUG] Set prescaler to: 1200
//2024-06-16 23:11:37,509 [DEBUG] 
_device.WriteWord(0x40, 0xAF04);
//2024-06-16 23:11:37,510 [DEBUG] Set arr to: 1200
//2024-06-16 23:11:37,511 [DEBUG] 
_device.WriteWord(0x44, 0xB004);
//2024-06-16 23:11:37,512 [DEBUG] Set arr to: 4095
//2024-06-16 23:11:37,512 [DEBUG] 
_device.WriteWord(0x44, 0xFF0F);
//2024-06-16 23:11:37,513 [DEBUG] Set prescaler to: 352
//2024-06-16 23:11:37,514 [DEBUG] 
_device.WriteWord(0x40, 0x5F01);
//2024-06-16 23:11:37,515 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,538 [DEBUG] Conneceted i2c device: ['0x14']
//address: 0x14
//bus: 1
//2024-06-16 23:11:37,539 [DEBUG] prescaler: 1200, period: 1200
//2024-06-16 23:11:37,540 [DEBUG] Set prescaler to: 1200
//2024-06-16 23:11:37,541 [DEBUG] 
_device.WriteWord(0x40, 0xAF04);
//2024-06-16 23:11:37,542 [DEBUG] Set arr to: 1200
//2024-06-16 23:11:37,543 [DEBUG] 
_device.WriteWord(0x44, 0xB004);
//2024-06-16 23:11:37,544 [DEBUG] Set arr to: 4095
//2024-06-16 23:11:37,544 [DEBUG] 
_device.WriteWord(0x44, 0xFF0F);
//2024-06-16 23:11:37,545 [DEBUG] Set prescaler to: 352
//2024-06-16 23:11:37,546 [DEBUG] 
_device.WriteWord(0x40, 0x5F01);
//2024-06-16 23:11:37,547 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,570 [DEBUG] Conneceted i2c device: ['0x14']
//address: 0x14
//bus: 1
//2024-06-16 23:11:37,572 [DEBUG] prescaler: 1200, period: 1200
//2024-06-16 23:11:37,573 [DEBUG] Set prescaler to: 1200
//2024-06-16 23:11:37,573 [DEBUG] 
_device.WriteWord(0x40, 0xAF04);
//2024-06-16 23:11:37,574 [DEBUG] Set arr to: 1200
//2024-06-16 23:11:37,575 [DEBUG] 
_device.WriteWord(0x44, 0xB004);
//2024-06-16 23:11:37,576 [DEBUG] Set arr to: 4095
//2024-06-16 23:11:37,577 [DEBUG] 
_device.WriteWord(0x44, 0xFF0F);
//2024-06-16 23:11:37,578 [DEBUG] Set prescaler to: 352
//2024-06-16 23:11:37,579 [DEBUG] 
_device.WriteWord(0x40, 0x5F01);
//2024-06-16 23:11:37,580 [DEBUG] Set angle to: 1.2
//2024-06-16 23:11:37,581 [DEBUG] Pulse width: 1513.3333333333335
//2024-06-16 23:11:37,582 [DEBUG] pulse width rate: 0.07566666666666667
//2024-06-16 23:11:37,582 [DEBUG] pulse width value: 309
//2024-06-16 23:11:37,583 [DEBUG] 
_device.WriteWord(0x22, 0x3501);
//2024-06-16 23:11:37,584 [DEBUG] Set angle to: 0.0
//2024-06-16 23:11:37,585 [DEBUG] Pulse width: 1500.0
//2024-06-16 23:11:37,586 [DEBUG] pulse width rate: 0.075
//2024-06-16 23:11:37,586 [DEBUG] pulse width value: 307
//2024-06-16 23:11:37,587 [DEBUG] 
_device.WriteWord(0x20, 0x3301);
//2024-06-16 23:11:37,588 [DEBUG] Set angle to: 10.4
//2024-06-16 23:11:37,588 [DEBUG] Pulse width: 1615.5555555555557
//2024-06-16 23:11:37,589 [DEBUG] pulse width rate: 0.08077777777777778
//2024-06-16 23:11:37,590 [DEBUG] pulse width value: 330
//2024-06-16 23:11:37,590 [DEBUG] 
_device.WriteWord(0x21, 0x4A01);
//2024-06-16 23:11:37,592 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,593 [INFO]  Pin init finished.
//2024-06-16 23:11:37,594 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,594 [INFO]  Pin init finished.
//2024-06-16 23:11:37,595 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,617 [DEBUG] Conneceted i2c device: ['0x14']
//address: 0x14
//bus: 1
//2024-06-16 23:11:37,618 [DEBUG] prescaler: 1200, period: 1200
//2024-06-16 23:11:37,618 [DEBUG] Set prescaler to: 1200
//2024-06-16 23:11:37,618 [DEBUG] 
_device.WriteWord(0x43, 0xAF04);
//2024-06-16 23:11:37,619 [DEBUG] Set arr to: 1200
//2024-06-16 23:11:37,619 [DEBUG] 
_device.WriteWord(0x47, 0xB004);
//2024-06-16 23:11:37,620 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,642 [DEBUG] Conneceted i2c device: ['0x14']
//address: 0x14
//bus: 1
//2024-06-16 23:11:37,643 [DEBUG] prescaler: 1200, period: 1200
//2024-06-16 23:11:37,643 [DEBUG] Set prescaler to: 1200
//2024-06-16 23:11:37,643 [DEBUG] 
_device.WriteWord(0x43, 0xAF04);
//2024-06-16 23:11:37,644 [DEBUG] Set arr to: 1200
//2024-06-16 23:11:37,644 [DEBUG] 
_device.WriteWord(0x47, 0xB004);
//2024-06-16 23:11:37,645 [DEBUG] Set arr to: 4095
//2024-06-16 23:11:37,646 [DEBUG] 
_device.WriteWord(0x47, 0xFF0F);
//2024-06-16 23:11:37,646 [DEBUG] Set prescaler to: 10
//2024-06-16 23:11:37,646 [DEBUG] 
_device.WriteWord(0x43, 0x0900);
//2024-06-16 23:11:37,647 [DEBUG] Set arr to: 4095
//2024-06-16 23:11:37,647 [DEBUG] 
_device.WriteWord(0x47, 0xFF0F);
//2024-06-16 23:11:37,648 [DEBUG] Set prescaler to: 10
//2024-06-16 23:11:37,648 [DEBUG] 
_device.WriteWord(0x43, 0x0900);
//2024-06-16 23:11:37,649 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,671 [DEBUG] Conneceted i2c device: ['0x14']
//address: 0x14
//bus: 1
//2024-06-16 23:11:37,672 [DEBUG] ADC device address: 0x14
//2024-06-16 23:11:37,672 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,694 [DEBUG] Conneceted i2c device: ['0x14']
//address: 0x14
//bus: 1
//2024-06-16 23:11:37,695 [DEBUG] ADC device address: 0x14
//2024-06-16 23:11:37,695 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,718 [DEBUG] Conneceted i2c device: ['0x14']
//address: 0x14
//bus: 1
//2024-06-16 23:11:37,719 [DEBUG] ADC device address: 0x14
//2024-06-16 23:11:37,720 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,721 [INFO]  Pin init finished.
//2024-06-16 23:11:37,721 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,722 [INFO]  Pin init finished.
//2024-06-16 23:11:37,722 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,723 [INFO]  Pin init finished.
//2024-06-16 23:11:37,723 [DEBUG] Set logging level to [debug]
//2024-06-16 23:11:37,723 [INFO]  Pin init finished.
//2024-06-16 23:11:37,724 [INFO]  
Thread.Sleep(1000);
GPI23.Write(0 );
//2024-06-16 23:11:37,724 [DEBUG] 
_device.WriteWord(0x2C, 0x650A);
//2024-06-16 23:11:37,725 [INFO]  
GPI24.Write(1 );

//2024-06-16 23:11:37,725 [DEBUG] 
_device.WriteWord(0x2D, 0x650A);
Thread.Sleep(4000);
//2024-06-16 23:11:38,727 [DEBUG] 
_device.WriteWord(0x2C, 0x0000);
//2024-06-16 23:11:38,728 [DEBUG] 
_device.WriteWord(0x2D, 0x0000);
//2024-06-16 23:11:38,730 [DEBUG] 
_device.WriteWord(0x2C, 0x0000);
//2024-06-16 23:11:38,731 [DEBUG] 
_device.WriteWord(0x2D, 0x0000);
//2024-06-16 23:11:38,734 [DEBUG] 
_device.WriteWord(0x2C, 0x0000);
//2024-06-16 23:11:38,734 [DEBUG] 
_device.WriteWord(0x2D, 0x0000);
//2024-06-16 23:11:38,737 [DEBUG] 
_device.WriteWord(0x2C, 0x0000);
//2024-06-16 23:11:38,738 [DEBUG] 
_device.WriteWord(0x2D, 0x0000);

//while (true)
//{
//    controller.Write(pin, ledOn ? PinValue.High : PinValue.Low);
//    Thread.Sleep(1000);
//    ledOn = !ledOn;
//    Console.WriteLine("Device test 1");
//    _device.Write([0x21, 0x4F, 0x01]);
//    Thread.Sleep(1000);
//    Console.WriteLine("Device test 2");
//    _device.Write([0x21, 0x1F, 0x01]);
//    Thread.Sleep(1000);
//    Console.WriteLine("Device test 3");
//    _device.Write([0x21, 0x01, 0x4F]);
//    Thread.Sleep(1000);
//    Console.WriteLine("Device test 4");
//    _device.Write([0x21, 0x01, 0x1F]);
//    Thread.Sleep(1000);
//    Console.WriteLine("Device test 5");
//    _device.Write([0x4F, 0x01, 0x21]);
//    Thread.Sleep(1000);
//    Console.WriteLine("Device test 6");
//    _device.Write([0x1F, 0x01, 0x21]);
//    //motor.Wheel(100);
//    //Thread.Sleep(1000);
//    //motor.Wheel(0);
//    //Thread.Sleep(1000);
//    //motor.Wheel(-100);
//    //Thread.Sleep(1000);
//    //motor.Wheel(-50);
//    //Thread.Sleep(1000);
//    //motor.Wheel(0);
//    //Thread.Sleep(1000);


//}