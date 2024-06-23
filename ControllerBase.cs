using System.Device.Gpio;
using System.Device.I2c;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;

public class ControllerBase
{

    private static IGpioController? _gpioController;
    private static bool _isTest = false;
    protected IGpioController GpioController
    {
        get
        {
            if (_gpioController == null)
            {
                _gpioController = new GpioController2();
            }
            return _gpioController;
        }
        set
        {
            _gpioController = value;
        }
    }

    protected bool ReadPin(int pinId)
    {
        if (_isTest) { return false; }
        var pin = EnsureOpenPin(pinId, PinMode.Input);
        var value = pin.Read();
        return value == PinValue.High;
    }
    protected void WritePin(int pinId, bool value)
    {
        if (_isTest) { Console.WriteLine($"Set pin {pinId} to {value}"); return; }
        var pin = EnsureOpenPin(pinId, PinMode.Output);
        pin.Write(value ? PinValue.High : PinValue.Low);
    }
    protected void SetPinMode(int pinId, PinMode mode)
    {
        if (_isTest) { Console.WriteLine($"Set pin {pinId} mode {mode}"); return; }
        EnsureOpenPin(pinId, mode);
    }

    private GpioPin EnsureOpenPin(int pinId, PinMode mode)
    {
        if (GpioController.IsPinOpen(pinId))
        {
            if (GpioController.GetPinMode(pinId) != mode)
            {
                GpioController.SetPinMode(pinId, mode);
            }
            return GpioController.OpenPin(pinId);
        }
        else
        {
            return GpioController.OpenPin(pinId, mode);
        }
    }

    protected I2cBus CreateI2cBus(int busId)
    {
        if (_isTest) { return new TestI2cBus(busId); }

        return I2cBus.Create(busId);
    }

    public static void SetTest()
    {
        _isTest = true;
    }

    private class TestI2cBus : I2cBus
    {
        public TestI2cBus(int busId)
        {
            BusId = busId;
        }

        public int BusId { get; }

        public override I2cDevice CreateDevice(int deviceAddress)
        {
            return new TestI2cDevice(BusId, deviceAddress);
        }

        public override void RemoveDevice(int deviceAddress)
        {
        }

        private class TestI2cDevice : I2cDevice
        {
            public TestI2cDevice(int busId, int deviceAddress)
            {
                BusId = busId;
                DeviceAddress = deviceAddress;
            }

            public override I2cConnectionSettings ConnectionSettings => new I2cConnectionSettings(BusId, DeviceAddress);

            public int BusId { get; }
            public int DeviceAddress { get; }

            public override void Read(Span<byte> buffer)
            {
            }

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                Console.WriteLine("Write I2C: " + Convert.ToHexString(buffer));
            }

            public override void WriteRead(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
            {
            }
        }
    }
}

