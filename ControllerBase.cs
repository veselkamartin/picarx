using System.Device.Gpio;
using System.Device.I2c;

public class ControllerBase
{

    private static GpioController? _gpioController;
    private static bool IsTest { get { return Environment.OSVersion.Platform == PlatformID.Win32NT; } }
    public static GpioController GetGpioController()
    {
        if (_gpioController == null)
        {
            if (IsTest)
            {
                _gpioController = new GpioController(PinNumberingScheme.Logical, new TestGpioDriver());
            }
            else
            {
                _gpioController = new GpioController();
            }
        }
        return _gpioController;
    }

    protected bool ReadPin(int pinId)
    {
        var pin = EnsureOpenPin(pinId, PinMode.Input);
        var value = pin.Read();
        return value == PinValue.High;
    }
    protected void WritePin(int pinId, bool value)
    {
        var pin = EnsureOpenPin(pinId, PinMode.Output);
        pin.Write(value ? PinValue.High : PinValue.Low);
    }
    protected void SetPinMode(int pinId, PinMode mode)
    {
        EnsureOpenPin(pinId, mode);
    }

    private GpioPin EnsureOpenPin(int pinId, PinMode mode)
    {
        var GpioController = GetGpioController();
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

    public static I2cBus CreateI2cBus(int busId)
    {
        if (IsTest) { return new TestI2cBus(busId); }

        return I2cBus.Create(busId);
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

    private class TestGpioDriver : GpioDriver
    {
        private readonly Dictionary<int, PinState> _pins = new Dictionary<int, PinState>();

        class PinState
        {
            public PinMode Mode { get; set; } = PinMode.Input;
            public bool IsOpen { get; set; }
        }
        PinState GetPinState(int pinNumber)
        {
            if (!_pins.TryGetValue(pinNumber, out var pinState))
            {
                pinState = new PinState();
                _pins.Add(pinNumber, pinState);
            }
            return pinState;
        }

        protected override int PinCount => throw new NotImplementedException();

        protected override void AddCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
        }

        protected override void ClosePin(int pinNumber)
        {
            GetPinState(pinNumber).IsOpen = false;
        }

        protected override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
        {
            return pinNumber;
        }

        protected override PinMode GetPinMode(int pinNumber)
        {
            return GetPinState(pinNumber).Mode;
        }

        protected override bool IsPinModeSupported(int pinNumber, PinMode mode)
        {
            return true;
        }

        protected override void OpenPin(int pinNumber)
        {
            GetPinState(pinNumber).IsOpen = true;
        }

        protected override PinValue Read(int pinNumber)
        {
            if (GetPinState(pinNumber).Mode == PinMode.Output) throw new Exception($"Pin {pinNumber} is output");
            if (!GetPinState(pinNumber).IsOpen) throw new Exception($"Pin {pinNumber} is closed");
            return PinValue.Low;
        }

        protected override void RemoveCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback)
        {
        }

        protected override void SetPinMode(int pinNumber, PinMode mode)
        {
            Console.WriteLine($"Setting pin {pinNumber} mode {mode}");

            GetPinState(pinNumber).Mode = mode;
        }

        protected override WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override void Write(int pinNumber, PinValue value)
        {
            if (GetPinState(pinNumber).Mode != PinMode.Output) throw new Exception($"Pin {pinNumber} is input");
            if (!GetPinState(pinNumber).IsOpen) throw new Exception($"Pin {pinNumber} is closed");
            Console.WriteLine($"Setting pin {pinNumber} to {value}");
        }
    }
}

