using System.Device.Gpio;
using System.Device.I2c;
using Microsoft.Extensions.Logging;
using SmartCar.RobotHat;

namespace SmartCar.PicarX;

public class Picarx : IDisposable
{
	private static readonly string CONFIG = "/opt/picar-x/picar-x.conf";

	private static readonly string DEFAULT_LINE_REF = "[1000,1000,1000]";
	private static readonly string DEFAULT_CLIFF_REF = "[500,500,500]";
	private const int DIR_MIN = -30;
	private const int DIR_MAX = 30;
	private const int CAM_PAN_MIN = -90;
	private const int CAM_PAN_MAX = 90;
	private const int CAM_TILT_MIN = -35;
	private const int CAM_TILT_MAX = 65;

	private const double TIMEOUT = 0.02;

	private FileDB config_file;
	private Servo cam_pan;
	private Servo cam_tilt;
	private Servo dir_servo_pin;
	//private Grayscale_Module grayscale;
	private readonly Ultrasonic _ultrasonic;
	private List<double> line_reference;
	private List<double> cliff_reference;
	private double dir_cali_val;
	private double cam_pan_cali_val;
	private double cam_tilt_cali_val;
	private double dir_current_angle;
	readonly RobotHat.RobotHat _robotHat;
	private readonly ILogger<Picarx> _logger;
	private bool _isDisposed;

	public Picarx(
		ILoggerFactory loggerFactory,
		GpioController? controller = null, bool shouldDisposeController = false,
		I2cBus? bus = null, bool shouldDisposeBus = false,
		List<string>? servo_pins = null,
		List<string>? grayscale_pins = null,
		List<string>? ultrasonic_pins = null,
		string? config = null)
	{
		_logger = loggerFactory.CreateLogger<Picarx>();
		_robotHat = new RobotHat.RobotHat(
			loggerFactory.CreateLogger<RobotHat.RobotHat>(),
			loggerFactory.CreateLogger<Pwm>(),
			loggerFactory.CreateLogger<Motor>(),
			controller, shouldDisposeController, bus, shouldDisposeBus);

		servo_pins = servo_pins ?? new List<string> { "P0", "P1", "P2" };
		grayscale_pins = grayscale_pins ?? new List<string> { "A0", "A1", "A2" };
		ultrasonic_pins = ultrasonic_pins ?? new List<string> { "D2", "D3" };
		config = config ?? CONFIG;

		// --------- config_file ---------
		config_file = new FileDB(config);

		// --------- servos init ---------
		var servoLogger = loggerFactory.CreateLogger<Servo>();
		cam_pan = new Servo(_robotHat.GetPwm(servo_pins[0]), servoLogger);
		cam_tilt = new Servo(_robotHat.GetPwm(servo_pins[1]), servoLogger);
		dir_servo_pin = new Servo(_robotHat.GetPwm(servo_pins[2]), servoLogger);

		// get calibration values
		dir_cali_val = double.Parse(config_file.Get("picarx_dir_servo", "0"));
		cam_pan_cali_val = double.Parse(config_file.Get("picarx_cam_pan_servo", "0"));
		cam_tilt_cali_val = double.Parse(config_file.Get("picarx_cam_tilt_servo", "0"));

		// set servos to init angle
		SetDirServoAngle(0);
		SetCamPanAngle(0);
		SetCamTiltAngle(0);

		// get calibration values
		var cali_dir_value = config_file.Get("picarx_dir_motor", "[1, 1]")
			.Trim('[', ']')
			.Split(',')
			.Select(int.Parse)
			.ToList();
		_robotHat.Motor.Calibration.Direction = new() { { MotorEnum.Left, cali_dir_value[0] }, { MotorEnum.Right, cali_dir_value[1] } };
		var cali_speed_value = new List<int> { 0, 0 };
		_robotHat.Motor.Calibration.Speed = new() { { MotorEnum.Left, cali_speed_value[0] }, { MotorEnum.Right, cali_speed_value[1] } };

		// --------- grayscale module init ---------
		//var adcs = grayscale_pins.Select(pin => new ADC(pin)).ToArray();
		//grayscale = new Grayscale_Module(adcs[0], adcs[1], adcs[2], reference: null);

		// get reference
		line_reference = config_file.Get("line_reference", DEFAULT_LINE_REF)
			.Trim('[', ']')
			.Split(',')
			.Select(double.Parse)
			.ToList();
		cliff_reference = config_file.Get("cliff_reference", DEFAULT_CLIFF_REF)
			.Trim('[', ']')
			.Split(',')
			.Select(double.Parse)
			.ToList();

		// transfer reference
		//grayscale.SetReference(line_reference);

		// --------- ultrasonic init ---------
		var trig = ultrasonic_pins[0];
		var echo = ultrasonic_pins[1];
		_ultrasonic = new Ultrasonic(_robotHat.GetPin(trig), _robotHat.GetPin(echo), loggerFactory.CreateLogger<Ultrasonic>());
	}

	public void MotorSpeedCalibration(int value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		if (value < 0)
		{
			_robotHat.Motor.Calibration.Speed[MotorEnum.Left] = 0;
			_robotHat.Motor.Calibration.Speed[MotorEnum.Right] = Math.Abs(value);
		}
		else
		{
			_robotHat.Motor.Calibration.Speed[MotorEnum.Left] = Math.Abs(value);
			_robotHat.Motor.Calibration.Speed[MotorEnum.Right] = 0;
		}
	}

	public void MotorDirectionCalibrate(MotorEnum motor, int value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		motor -= 1;
		if (value == 1)
		{
			_robotHat.Motor.Calibration.Direction[motor] = 1;
		}
		else if (value == -1)
		{
			_robotHat.Motor.Calibration.Direction[motor] = -1;
		}
		config_file.Set("picarx_dir_motor", string.Join(", ", _robotHat.Motor.Calibration.Direction));
	}

	public void DirServoCalibrate(double value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		dir_cali_val = value;
		//config_file.Set("picarx_dir_servo", value.ToString());
		dir_servo_pin.SetAngle(value);
	}

	public void SetDirServoAngle(double value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		_logger.LogInformation("Direction {value}", value);

		dir_current_angle = Math.Clamp(value, DIR_MIN, DIR_MAX);
		double angle_value = dir_current_angle + dir_cali_val;
		dir_servo_pin.SetAngle(angle_value);
	}

	public void CamPanServoCalibrate(double value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		cam_pan_cali_val = value;
		//config_file.Set("picarx_cam_pan_servo", value.ToString());
		cam_pan.SetAngle(value);
	}

	public void CamTiltServoCalibrate(double value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		cam_tilt_cali_val = value;
		config_file.Set("picarx_cam_tilt_servo", value.ToString());
		cam_tilt.SetAngle(value);
	}

	public void SetCamPanAngle(double value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		value = Math.Clamp(value, CAM_PAN_MIN, CAM_PAN_MAX);
		_logger.LogInformation($"Setting Cam pan angle to {value}");
		cam_pan.SetAngle(-1 * (value + -1 * cam_pan_cali_val));
	}

	public void SetCamTiltAngle(double value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		value = Math.Clamp(value, CAM_TILT_MIN, CAM_TILT_MAX);
		_logger.LogInformation($"Setting Cam tilt angle to {value}");
		cam_tilt.SetAngle(-1 * (value + -1 * cam_tilt_cali_val));
	}

	public void SetPower(int speed)
	{
		if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

		_robotHat.Motor.SetMotorSpeed(MotorEnum.Left, speed);
		_robotHat.Motor.SetMotorSpeed(MotorEnum.Right, speed);
	}

	public void Backward(int speed)
	{
		if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

		_logger.LogInformation($"Backward {speed}");

		double current_angle = dir_current_angle;
		if (current_angle != 0)
		{
			double abs_current_angle = Math.Abs(current_angle);
			if (abs_current_angle > DIR_MAX) abs_current_angle = DIR_MAX;
			double power_scale = (100 - abs_current_angle) / 100.0;
			if (current_angle / abs_current_angle > 0)
			{
				_robotHat.Motor.SetMotorSpeed(MotorEnum.Left, -1 * speed);
				_robotHat.Motor.SetMotorSpeed(MotorEnum.Right, (int)(speed * power_scale));
			}
			else
			{
				_robotHat.Motor.SetMotorSpeed(MotorEnum.Left, (int)(-1 * speed * power_scale));
				_robotHat.Motor.SetMotorSpeed(MotorEnum.Right, speed);
			}
		}
		else
		{
			_robotHat.Motor.SetMotorSpeed(MotorEnum.Left, -1 * speed);
			_robotHat.Motor.SetMotorSpeed(MotorEnum.Right, speed);
		}
	}

	public void Forward(int speed)
	{
		if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

		_logger.LogInformation($"Forward {speed}");

		double current_angle = dir_current_angle;
		if (current_angle != 0)
		{
			double abs_current_angle = Math.Abs(current_angle);
			if (abs_current_angle > DIR_MAX) abs_current_angle = DIR_MAX;
			double power_scale = (100 - abs_current_angle) / 100.0;
			if (current_angle / abs_current_angle > 0)
			{
				_robotHat.Motor.SetMotorSpeed(MotorEnum.Left, (int)(speed * power_scale));
				_robotHat.Motor.SetMotorSpeed(MotorEnum.Right, -speed);
			}
			else
			{
				_robotHat.Motor.SetMotorSpeed(MotorEnum.Left, speed);
				_robotHat.Motor.SetMotorSpeed(MotorEnum.Right, (int)(-1 * speed * power_scale));
			}
		}
		else
		{
			_robotHat.Motor.SetMotorSpeed(MotorEnum.Left, speed);
			_robotHat.Motor.SetMotorSpeed(MotorEnum.Right, -1 * speed);
		}
	}
	public async Task Turn(int angle)
	{
		SetDirServoAngle(angle < 0 ? -30 : 30);
		Forward(80);
		await Task.Delay(Math.Abs(angle) * 10);
		Stop();
	}
	public async Task DirectForward(int distanceInCm)
	{
		SetDirServoAngle(0);
		Forward(80);
		await Task.Delay(distanceInCm * 10);
		Stop();
	}
	public async Task DirectBack(int distanceInCm)
	{
		SetDirServoAngle(0);
		Backward(80);
		await Task.Delay(distanceInCm * 10);
		Stop();
	}

	public void Stop()
	{
		if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

		_logger.LogInformation("Stop");
		_robotHat.Motor.Stop();
	}

	public double GetDistance()
	{
		return _ultrasonic.Read();
	}

	//public void SetGrayscaleReference(List<double> value)
	//{
	//    if (value.Count == 3)
	//    {
	//        line_reference = value;
	//        grayscale.SetReference(line_reference);
	//        config_file.Set("line_reference", string.Join(", ", line_reference));
	//    }
	//    else
	//    {
	//        throw new ArgumentException("Grayscale reference must be a list of 3 values.");
	//    }
	//}

	//public List<double> GetGrayscaleData()
	//{
	//    return new List<double>(grayscale.Read());
	//}

	//public bool GetLineStatus(List<double> gm_val_list)
	//{
	//    return grayscale.ReadStatus(gm_val_list);
	//}

	//public void SetLineReference(List<double> value)
	//{
	//    SetGrayscaleReference(value);
	//}

	public bool GetCliffStatus(List<double> gm_val_list)
	{
		if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

		for (int i = 0; i < 3; i++)
		{
			if (gm_val_list[i] <= cliff_reference[i])
			{
				return true;
			}
		}
		return false;
	}

	public void SetCliffReference(List<double> value)
	{
		if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

		if (value.Count == 3)
		{
			cliff_reference = value;
			config_file.Set("cliff_reference", string.Join(", ", cliff_reference));
		}
		else
		{
			throw new ArgumentException("Cliff reference must be a list of 3 values.");
		}
	}
	/// <inheritdoc/>
	public void Dispose()
	{
		_robotHat.Dispose();
		_isDisposed = true;
	}
}