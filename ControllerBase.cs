using Microsoft.Extensions.Logging;
using System.Device.Gpio;
using System.Device.I2c;

namespace PicarX;

public class ControllerBase
{

	private static GpioController? _gpioController;
	private static bool IsTest { get { return Environment.OSVersion.Platform == PlatformID.Win32NT; } }
	public static GpioController GetGpioController(ILoggerFactory loggerFactory)
	{
		if (_gpioController == null)
		{
			if (IsTest)
			{
				_gpioController = new GpioController(PinNumberingScheme.Logical, new TestGpioDriver(loggerFactory.CreateLogger("TestController")));
			}
			else
			{
				_gpioController = new GpioController();
			}
		}
		return _gpioController;
	}




	public static I2cBus CreateI2cBus(int busId, ILoggerFactory loggerFactory)
	{
		if (IsTest) { return new TestI2cBus(busId, loggerFactory.CreateLogger("TestController")); }

		return I2cBus.Create(busId);
	}



	private class TestI2cBus : I2cBus
	{
		private readonly ILogger _logger;

		public TestI2cBus(int busId, ILogger logger)
		{
			BusId = busId;
			_logger = logger;
		}

		public int BusId { get; }

		public override I2cDevice CreateDevice(int deviceAddress)
		{
			return new TestI2cDevice(BusId, deviceAddress, _logger);
		}

		public override void RemoveDevice(int deviceAddress)
		{
		}

		private class TestI2cDevice : I2cDevice
		{
			private readonly ILogger _logger;

			public TestI2cDevice(int busId, int deviceAddress, ILogger logger)
			{
				BusId = busId;
				DeviceAddress = deviceAddress;
				_logger = logger;
			}

			public override I2cConnectionSettings ConnectionSettings => new I2cConnectionSettings(BusId, DeviceAddress);

			public int BusId { get; }
			public int DeviceAddress { get; }

			public override void Read(Span<byte> buffer)
			{
			}

			public override void Write(ReadOnlySpan<byte> buffer)
			{
				_logger.LogInformation("Write I2C: " + Convert.ToHexString(buffer));
			}

			public override void WriteRead(ReadOnlySpan<byte> writeBuffer, Span<byte> readBuffer)
			{
			}
		}
	}

	private class TestGpioDriver : GpioDriver
	{
		private readonly Dictionary<int, PinState> _pins = new Dictionary<int, PinState>();
		private ILogger _logger;

		public TestGpioDriver(ILogger logger)
		{
			_logger = logger;
		}

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
			_logger.LogInformation($"Setting pin {pinNumber} mode {mode}");

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
			_logger.LogInformation($"Setting pin {pinNumber} to {value}");
		}
	}
}