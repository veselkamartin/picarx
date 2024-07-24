namespace PicarX;

public class KeyboardControl
{
	private readonly PicarX.Picarx _px;

	public KeyboardControl(PicarX.Picarx px)
	{
		this._px = px;
	}

	public void Run()
	{
		int pan_angle = 0;
		int tilt_angle = 0;


		ShowInfo();
		try
		{
			while (true)
			{
				var key = Console.ReadKey();
				var lowerKey = char.ToLower(key.KeyChar);

				if ("wsadikjlq".Contains(lowerKey))
				{
					if ('w' == lowerKey)
					{
						_px.SetDirServoAngle(0);
						_px.Forward(80);
						Thread.Sleep(500);
						_px.Stop();
					}
					else if ('s' == lowerKey)
					{
						_px.SetDirServoAngle(0);
						_px.Backward(80); 
						Thread.Sleep(500);
						_px.Stop();
					}
					else if ('a' == lowerKey)
					{
						_px.SetDirServoAngle(-30);
						_px.Forward(80);
						Thread.Sleep(500);
						_px.Stop();
					}
					else if ('d' == lowerKey)
					{
						_px.SetDirServoAngle(30);
						_px.Forward(80);
						Thread.Sleep(500);
						_px.Stop();
					}
					else if ('i' == lowerKey)
					{
						tilt_angle += 5;
						if (tilt_angle > 30)
							tilt_angle = 30;
						_px.SetCamTiltAngle(tilt_angle);
					}
					else if ('k' == lowerKey)
					{
						tilt_angle -= 5;
						if (tilt_angle < -30)
							tilt_angle = -30;
						_px.SetCamTiltAngle(tilt_angle);
					}
					else if ('l' == lowerKey)
					{
						pan_angle += 5;
						if (pan_angle > 30)
							pan_angle = 30;
						_px.SetCamPanAngle(pan_angle);
					}
					else if ('j' == lowerKey)
					{
						pan_angle -= 5;
						if (pan_angle < -30)
							pan_angle = -30;
						_px.SetCamPanAngle(pan_angle);
					}
					else if ('q' == lowerKey)
					{
						ObstacleAvoidance.AvoidObstacles(_px);
					}

					Console.WriteLine();
					var distance = _px.GetDistance();
					Console.WriteLine($"Distance: {distance}");
				}
				else if (key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control)
				{
					Console.WriteLine("\n Quit");
					break;
				}
				else
				{
					Console.WriteLine($"Unknown key {key}");
				}
			}
		}
		finally
		{
			_px.SetCamTiltAngle(0);
			_px.SetCamPanAngle(0);
			_px.SetDirServoAngle(0);
			_px.Stop();
			Thread.Sleep(200);
		}
	}
	static void ShowInfo()
	{
		Console.WriteLine("""
		Press keys on keyboard to control PiCar-X!
		    w: Forward
		    a: Turn left
		    s: Backward
		    d: Turn right
		    i: Head up
		    k: Head down
		    j: Turn head left
		    l: Turn head right
		    ctrl+c: Press twice to exit the program
		""");
	}
}