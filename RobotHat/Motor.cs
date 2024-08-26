using Microsoft.Extensions.Logging;
using System.Device.Gpio;

namespace SmartCar.RobotHat;

public class Motor
{
	private const ushort PERIOD = 4095;
	private const byte PRESCALER = 10;

	private readonly Dictionary<MotorEnum, Pwm> _motorSpeedPins;
	private readonly Dictionary<MotorEnum, GpioPin> _motorDirectionPins;
	private readonly ILogger<Motor> _logger;

	public class MotorCalibration
	{
		public Dictionary<MotorEnum, int> Direction { get; set; } = new() { { MotorEnum.Left, 0 }, { MotorEnum.Right, 0 } };
		public Dictionary<MotorEnum, int> Speed { get; set; } = new() { { MotorEnum.Left, 0 }, { MotorEnum.Right, 0 } };
	}
	public MotorCalibration Calibration { get; set; } = new MotorCalibration();

	public Motor(
		ILogger<Motor> logger,
		Pwm leftRearPwmPin,
		Pwm rightRearPwmPin,
		GpioPin leftRearDirPin,
		GpioPin rightRearDirPin)
	{
		_motorSpeedPins = new() { { MotorEnum.Left, leftRearPwmPin }, { MotorEnum.Right, rightRearPwmPin } };
		_motorDirectionPins = new() { { MotorEnum.Left, leftRearDirPin }, { MotorEnum.Right, rightRearDirPin } };

		// Initialize PWM pins
		foreach (var pin in _motorSpeedPins.Values)
		{
			pin.SetPeriod(PERIOD);
			pin.SetPrescaler(PRESCALER);
		}
		_logger = logger;
	}

	// Control motor direction and speed
	// motor 0 or 1,
	// dir 0 or 1
	// speed 0 ~ 100
	public void SetMotorSpeed(MotorEnum motor, int speed)
	{
		speed = Math.Clamp(speed, -100, 100);
		_logger.LogInformation($"Setting motor {motor} speed to {speed}");
		int direction = speed >= 0 ? 1 * Calibration.Direction[motor] : -1 * Calibration.Direction[motor];
		speed = Math.Abs(speed);
		if (speed != 0) speed = speed / 2 + 50;
		speed -= Calibration.Speed[motor];

		if (direction < 0)
		{
			_motorDirectionPins[motor].Write(PinValue.High);
			_motorSpeedPins[motor].SetPulseWidthPercent(speed);
		}
		else
		{
			_motorDirectionPins[motor].Write(PinValue.Low);
			_motorSpeedPins[motor].SetPulseWidthPercent(speed);
		}
	}
	public void Stop()
	{
		_logger.LogInformation($"Stop");

		//Do twice to make sure
		_motorSpeedPins[MotorEnum.Left].SetPulseWidthPercent(0);
		_motorSpeedPins[MotorEnum.Right].SetPulseWidthPercent(0);
		Thread.Sleep(2);
		_motorSpeedPins[MotorEnum.Left].SetPulseWidthPercent(0);
		_motorSpeedPins[MotorEnum.Right].SetPulseWidthPercent(0);
	}
}
public enum MotorEnum
{
	Right,
	Left
}