using Microsoft.Extensions.Logging;
using System.Device.Gpio;
using System.Diagnostics;

namespace PicarX.PicarX;

public class Ultrasonic
{
	private const double SOUND_SPEED = 343.3; // ms
	private readonly GpioPin _trig;
	private readonly GpioPin _echo;
	private readonly ILogger<Ultrasonic> _logger;
	private readonly TimeSpan _timeout;
	public Ultrasonic(GpioPin trig, GpioPin echo, ILogger<Ultrasonic> logger, TimeSpan? timeout = null)
	{
		if (trig == null || echo == null)
			throw new ArgumentException("trig and echo must be Pin objects");

		_timeout = timeout ?? TimeSpan.FromMilliseconds(20);
		_trig = trig;
		_echo = echo;
		_logger = logger;
		_trig.SetPinMode(PinMode.Output);
		_echo.SetPinMode(PinMode.InputPullDown);
	}

	private double ReadDistance()
	{
		var durationStopwatch = new Stopwatch();
		var durationStartStopwatch = new Stopwatch();
		var fromStartStopwatch = Stopwatch.StartNew();
		//Console.WriteLine($"Echo: {_echo.Read()}");
		_trig.Write(PinValue.Low);
		Thread.Sleep(1);
		_trig.Write(PinValue.High);
		WaitMicroseconds(10);
		_trig.Write(PinValue.Low);
		//Console.WriteLine($"Echo: {_echo.Read()}");
		durationStartStopwatch.Start();


		while (_echo.Read() == PinValue.Low)
		{
			if (fromStartStopwatch.Elapsed > _timeout)
			{
				_logger.LogInformation("Waiting for low timed out");
				return -1;
			}
		}
		durationStartStopwatch.Stop();

		durationStopwatch.Start();
		while (_echo.Read() == PinValue.High)
		{
			if (fromStartStopwatch.Elapsed > _timeout)
			{
				_logger.LogInformation($"Waiting for high timed out, start {durationStartStopwatch.Elapsed.TotalMilliseconds}");
				return -1;
			}
		}
		durationStopwatch.Stop();
		var duration = durationStopwatch.Elapsed.TotalMilliseconds;
		_logger.LogInformation($"Duration: {duration}, start {durationStartStopwatch.Elapsed.TotalMilliseconds}");
		var distance = Math.Round(duration * SOUND_SPEED / 2 / 10, 2);
		return distance;
	}

	public double Read(int times = 10)
	{
		_logger.LogInformation($"Distance reading {times} times");
		for (int i = 0; i < times; i++)
		{
			var distance = ReadDistance();
			if (distance != -1)
			{
				_logger.LogInformation("Distance {distance}", distance);

				return distance;
			}
		}
		_logger.LogInformation($"Distance cannot be read");
		return -1;
	}
	private static void WaitMicroseconds(long microseconds)
	{
		if (microseconds <= 0) return;

		var stopwatch = Stopwatch.StartNew();
		while (stopwatch.Elapsed.TotalMicroseconds < microseconds)
		{
			// Spin-wait until the desired time has elapsed
		}
		stopwatch.Stop();
	}
}
