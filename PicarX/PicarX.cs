using System.Device.Gpio;
using System.Device.I2c;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SmartCar.RobotHat;

namespace SmartCar.PicarX;

public class Picarx : IDisposable
{
	private static readonly string CONFIG = "/opt/picar-x/picar-x.conf";

	private const string DEFAULT_LINE_REF = "[1000,1000,1000]";
	private const string DEFAULT_CLIFF_REF = "[500,500,500]";
	private const int DIR_MIN = -30;
	private const int DIR_MAX = 30;
	private const int CAM_PAN_MIN = -90;
	private const int CAM_PAN_MAX = 90;
	private const int CAM_TILT_MIN = -35;
	private const int CAM_TILT_MAX = 65;

	private const double TIMEOUT = 0.02;

	private FileDB config_file;
	private readonly Servo _camPanServo;
	private readonly Servo _camTiltServo;
	private readonly Servo _dirServo;
	//private Grayscale_Module grayscale;
	private readonly Ultrasonic _ultrasonic;
	private List<double> line_reference;
	private List<double> cliff_reference;
	private double _dir_cali_val;
	private double _cam_pan_current;
	private double _cam_pan_cali_val;
	private double _cam_tilt_current;
	private double _cam_tilt_cali_val;
	private double _dir_current_angle;
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
		_camPanServo = new Servo(_robotHat.GetPwm(servo_pins[0]), servoLogger);
		_camTiltServo = new Servo(_robotHat.GetPwm(servo_pins[1]), servoLogger);
		_dirServo = new Servo(_robotHat.GetPwm(servo_pins[2]), servoLogger);

		// get calibration values
		_dir_cali_val = double.Parse(config_file.Get("picarx_dir_servo", "0"));
		_cam_pan_cali_val = double.Parse(config_file.Get("picarx_cam_pan_servo", "0"));
		_cam_tilt_cali_val = double.Parse(config_file.Get("picarx_cam_tilt_servo", "0"));

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

		_dir_cali_val = value;
		//config_file.Set("picarx_dir_servo", value.ToString());
		_dirServo.SetAngle(value);
	}

	public void SetDirServoAngle(double value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		_logger.LogInformation("Direction {value}", value);

		_dir_current_angle = Math.Clamp(value, DIR_MIN, DIR_MAX);
		double angle_value = _dir_current_angle + _dir_cali_val;
		_dirServo.SetAngle(angle_value);
	}
	public double CurrentDirServoAngle => _dir_current_angle;
	public void CamPanServoCalibrate(double value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		_cam_pan_cali_val = value;
		//config_file.Set("picarx_cam_pan_servo", value.ToString());
		_camPanServo.SetAngle(value);
	}

	public void CamTiltServoCalibrate(double value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		_cam_tilt_cali_val = value;
		config_file.Set("picarx_cam_tilt_servo", value.ToString());
		_camTiltServo.SetAngle(value);
	}

	public void SetCamPanAngle(double value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		value = Math.Clamp(value, CAM_PAN_MIN, CAM_PAN_MAX);
		_cam_pan_current = value;
		_logger.LogInformation($"Setting Cam pan angle to {value}");
		_camPanServo.SetAngle(-1 * (value + -1 * _cam_pan_cali_val));
	}

	public void SetCamTiltAngle(double value)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		value = Math.Clamp(value, CAM_TILT_MIN, CAM_TILT_MAX);
		_cam_tilt_current = value;
		_logger.LogInformation($"Setting Cam tilt angle to {value}");
		_camTiltServo.SetAngle(-1 * (value + -1 * _cam_tilt_cali_val));
	}

	public void SetPower(int speed)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		_robotHat.Motor.SetMotorSpeed(MotorEnum.Left, speed);
		_robotHat.Motor.SetMotorSpeed(MotorEnum.Right, speed);
	}

	public void Backward(int speed)
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		_logger.LogInformation($"Backward {speed}");

		double current_angle = _dir_current_angle;
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
		ObjectDisposedException.ThrowIf(_isDisposed, this);

		_logger.LogInformation($"Forward {speed}");

		double current_angle = _dir_current_angle;
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
	public async Task Turn(int angle, CancellationToken ct)
	{
		int dirServoAngle = angle < 0 ? -30 : 30;
		if (_dir_current_angle != dirServoAngle)
		{
			SetDirServoAngle(dirServoAngle);
			await Task.Delay(300);
		}
		Forward(80);
		var timeInMs = Math.Abs(angle) * 12.6 + 64;
		await Task.Delay((int)timeInMs, ct);
		Stop();
	}
	public async Task<bool> DirectForward(int distanceInCm, CancellationToken ct)
	{
		SetDirServoAngle(0);
		Forward(80);
		int runTime = DistanceToTime(distanceInCm);
		var stopWatch = Stopwatch.StartNew();
		bool completed = true;
		while (stopWatch.ElapsedMilliseconds < runTime && !ct.IsCancellationRequested)
		{
			var remaining = runTime - stopWatch.ElapsedMilliseconds;
			//if remaining time is less thant 20ms, we do not measure distance bacause mesurement can take longer than that
			if (remaining > 20)
			{
				var distance = GetDistance();

				if (distance < 10.0)
				{
					completed = false;
					break;
				}
				await Task.Delay(10, ct);
			}
		}
		Stop();
		return completed;
	}
	private int DistanceToTime(int distanceInCm)
	{
		//Measured:
		//Distance[cm] Time[ms]
		//    15         350   
		//    78        1550  

		// (1550-350)/(78-15)=64
		// 350-15*65=19

		return 64 + 19 * distanceInCm;
	}

	public async Task DirectBack(int distanceInCm, CancellationToken ct)
	{
		SetDirServoAngle(0);
		Backward(80);
		await Task.Delay(DistanceToTime(distanceInCm), ct);
		Stop();
	}

	public void Stop()
	{
		ObjectDisposedException.ThrowIf(_isDisposed, this);

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
		ObjectDisposedException.ThrowIf(_isDisposed, this);

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
		ObjectDisposedException.ThrowIf(_isDisposed, this);

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