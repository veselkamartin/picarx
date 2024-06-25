using System.Device.Gpio;
using System.Device.I2c;

namespace PicarX.RobotHat;

public class RobotHat : IDisposable
{
	private const int BOARD_TYPE_PIN = 12;
	private readonly bool _shouldDisposeController;
	private GpioController _controller;
	private readonly bool _shouldDisposeBus;
	private I2cBus _bus;
	private readonly I2cDevice _device;
	bool _isDisposed;

	public const int I2C_DEVICE_ADDR1 = 0x14;
	public const int I2C_DEVICE_ADDR2 = 0x15;


	private Dictionary<string, int> _dict;
	private Dictionary<string, GpioPin> _pins = new();


	private Dictionary<string, int> _dict_1 = new Dictionary<string, int>
		{
			{ "D0",  17 },
			{ "D1",  18 },
			{ "D2",  27 },
			{ "D3",  22 },
			{ "D4",  23 },
			{ "D5",  24 },
			{ "D6",  25 },
			{ "D7",  4 },
			{ "D8",  5 },
			{ "D9",  6 },
			{ "D10", 12 },
			{ "D11", 13 },
			{ "D12", 19 },
			{ "D13", 16 },
			{ "D14", 26 },
			{ "D15", 20 },
			{ "D16", 21 },
			{ "SW",  19 },
			{ "USER", 19 },
			{ "LED", 26 },
			{ "BOARD_TYPE", BOARD_TYPE_PIN },
			{ "RST", 16 },
			{ "BLEINT", 13 },
			{ "BLERST", 20 },
			{ "MCURST", 21 }
		};

	private Dictionary<string, int> _dict_2 = new Dictionary<string, int>
		{
			{ "D0",  17 },
			{ "D1",  4 }, // Changed
            { "D2",  27 },
			{ "D3",  22 },
			{ "D4",  23 },
			{ "D5",  24 },
			{ "D6",  25 }, // Removed
            { "D7",  4 }, // Removed
            { "D8",  5 }, // Removed
            { "D9",  6 },
			{ "D10", 12 },
			{ "D11", 13 },
			{ "D12", 19 },
			{ "D13", 16 },
			{ "D14", 26 },
			{ "D15", 20 },
			{ "D16", 21 },
			{ "SW",  25 }, // Changed
            { "USER", 25 },
			{ "LED", 26 },
			{ "BOARD_TYPE", BOARD_TYPE_PIN },
			{ "RST", 16 },
			{ "BLEINT", 13 },
			{ "BLERST", 20 },
			{ "MCURST", 5 } // Changed
        };
	GpioPin D0 { get { return GetPin("D0"); } }
	GpioPin D1 { get { return GetPin("D1"); } }
	GpioPin D2 { get { return GetPin("D2"); } }
	GpioPin D3 { get { return GetPin("D3"); } }
	GpioPin D4 { get { return GetPin("D4", PinMode.Output); } }
	GpioPin D5 { get { return GetPin("D5", PinMode.Output); } }
	GpioPin D6 { get { return GetPin("D6"); } }
	GpioPin D7 { get { return GetPin("D7"); } }
	GpioPin D8 { get { return GetPin("D8"); } }
	GpioPin D9 { get { return GetPin("D9"); } }
	GpioPin D10 { get { return GetPin("D10"); } }
	GpioPin D11 { get { return GetPin("D11"); } }
	GpioPin D12 { get { return GetPin("D12"); } }
	GpioPin D13 { get { return GetPin("D13"); } }
	GpioPin D14 { get { return GetPin("D14"); } }
	GpioPin D15 { get { return GetPin("D15"); } }
	GpioPin D16 { get { return GetPin("D16"); } }
	GpioPin SW { get { return GetPin("SW"); } }
	GpioPin USER { get { return GetPin("USER"); } }
	GpioPin LED { get { return GetPin("LED", PinMode.Output); } }
	GpioPin BOARD_TYPE { get; }
	GpioPin RST { get { return GetPin("RST"); } }
	GpioPin BLEINT { get { return GetPin("BLEINT"); } }
	GpioPin BLERST { get { return GetPin("BLERST"); } }
	GpioPin MCURST { get { return GetPin("MCURST", PinMode.Output); } }

	public Motor Motor { get; }

	public RobotHat(
			GpioController? controller = null, bool shouldDisposeController = false,
			I2cBus? bus = null, bool shouldDisposeBus = false)
	{
		_shouldDisposeController = shouldDisposeController || controller == null;
		_controller = controller ?? new GpioController();

		_shouldDisposeBus = shouldDisposeBus || bus is null;
		_bus = bus ?? I2cBus.Create(1);
		_device = _bus.CreateDevice(I2C_DEVICE_ADDR1);


		BOARD_TYPE = _controller.OpenPin(BOARD_TYPE_PIN, PinMode.Input);
		_pins.Add("BOARD_TYPE", BOARD_TYPE);

		var pin = BOARD_TYPE.Read() == PinValue.High;
		if (!pin)
		{
			Console.WriteLine("using board type 1");
			_dict = _dict_1;
		}
		else
		{
			Console.WriteLine("using board type 2");
			_dict = _dict_2;
		}

		ResetMcu();

		var leftRearPwmPin = GetPwm("P13");
		var rightRearPwmPin = GetPwm("P12");
		var leftRearDirPin = D4;
		var rightRearDirPin = D5;

		Motor = new Motor(leftRearPwmPin, rightRearPwmPin, leftRearDirPin, rightRearDirPin);
	}

	private void ResetMcu()
	{
		MCURST.Write(PinValue.Low);
		Thread.Sleep(10);
		MCURST.Write(PinValue.High);
		Thread.Sleep(200);
	}

	public GpioPin GetPin(string pinName, PinMode? pinMode = null)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		if (_pins.TryGetValue(pinName, out var pin))
		{
			return pin;
		}
		if (!_dict.TryGetValue(pinName, out int pinNumber))
		{
			throw new ArgumentException($"Pin should be in {_dict.Keys}, not {pinName}");
		}

		if (pinMode.HasValue)
		{
			pin = _controller.OpenPin(pinNumber, pinMode.Value);
		}
		else
		{
			pin = _controller.OpenPin(pinNumber);
		}
		_pins.Add(pinName, pin);
		return pin;
	}
	public Pwm GetPwm(string pwmName)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		return new Pwm(_device, pwmName);
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		foreach (var pin in _pins)
		{
			var pinNumber = _dict[pin.Key];
			_controller.ClosePin(pinNumber);
		}
		_pins.Clear();
		if (_shouldDisposeController)
		{
			_controller?.Dispose();
		}
		_controller = null!;

		_bus.RemoveDevice(_device.ConnectionSettings.DeviceAddress);
		if (_shouldDisposeBus)
		{
			_bus?.Dispose();
		}
		_bus = null!;
		_isDisposed = true;
	}
}
