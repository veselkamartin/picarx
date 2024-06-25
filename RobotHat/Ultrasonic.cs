using System.Device.Gpio;

namespace PicarX.RobotHat;

public class Ultrasonic
{
	private const double SOUND_SPEED = 343.3; // ms
	private readonly GpioPin _trig;
	private readonly GpioPin _echo;
	private readonly TimeSpan _timeout;
	public Ultrasonic(GpioPin trig, GpioPin echo, TimeSpan? timeout = null)
	{
		if (trig == null || echo == null)
			throw new ArgumentException("trig and echo must be Pin objects");

		_timeout = timeout ?? TimeSpan.FromMicroseconds(20);
		_trig = trig;
		_echo = echo;

		_trig.SetPinMode(PinMode.Output);
		_echo.SetPinMode(PinMode.InputPullDown);
	}

	private double ReadDistance()
	{
		var durationStopwatch = new System.Diagnostics.Stopwatch();
		_trig.Write(PinValue.Low);
		Thread.Sleep(1);
		_trig.Write(PinValue.High);
		Thread.Sleep(1);
		_trig.Write(PinValue.Low);

		var timeoutStart = DateTime.UtcNow;

		while (_echo.Read() == PinValue.Low)
		{
			var timeFromStart = DateTime.UtcNow - timeoutStart;
			if (timeFromStart > _timeout)
			{
				return -1;
			}
		}
		durationStopwatch.Start();
		while (_echo.Read() == PinValue.High)
		{
			var timeFromStart = DateTime.UtcNow - timeoutStart;
			if (timeFromStart > _timeout)
			{
				return -1;
			}
		}
		durationStopwatch.Stop();
		var duration = durationStopwatch.Elapsed.TotalMilliseconds;
		var distance = Math.Round(duration * SOUND_SPEED / 2 * 100, 2);
		return distance;
	}

	public double Read(int times = 10)
	{
		for (int i = 0; i < times; i++)
		{
			var distance = ReadDistance();
			if (distance != -1)
			{
				return distance;
			}
		}
		return -1;
	}
}
