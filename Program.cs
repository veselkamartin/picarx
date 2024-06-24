
class Program
{
    static string manual = @"
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
";

    static void ShowInfo()
    {
        Console.Clear();  // clear terminal window
        Console.WriteLine(manual);
    }

    static void Main(string[] args)
    {
        //ControllerBase.SetTest();

        int pan_angle = 0;
        int tilt_angle = 0;
        Picarx px = new Picarx();

        ShowInfo();
        try
        {
            while (true)
            {
                var key = Console.ReadKey();
                var lowerKey = char.ToLower(key.KeyChar);

                if ("wsadikjl".Contains(lowerKey))
                {
                    if ('w' == lowerKey)
                    {
                        px.SetDirServoAngle(0);
                        px.Forward(80);
                    }
                    else if ('s' == lowerKey)
                    {
                        px.SetDirServoAngle(0);
                        px.Backward(80);
                    }
                    else if ('a' == lowerKey)
                    {
                        px.SetDirServoAngle(-30);
                        px.Forward(80);
                    }
                    else if ('d' == lowerKey)
                    {
                        px.SetDirServoAngle(30);
                        px.Forward(80);
                    }
                    else if ('i' == lowerKey)
                    {
                        tilt_angle += 5;
                        if (tilt_angle > 30)
                            tilt_angle = 30;
                    }
                    else if ('k' == lowerKey)
                    {
                        tilt_angle -= 5;
                        if (tilt_angle < -30)
                            tilt_angle = -30;
                    }
                    else if ('l' == lowerKey)
                    {
                        pan_angle += 5;
                        if (pan_angle > 30)
                            pan_angle = 30;
                    }
                    else if ('j' == lowerKey)
                    {
                        pan_angle -= 5;
                        if (pan_angle < -30)
                            pan_angle = -30;
                    }

                    px.SetCamTiltAngle(tilt_angle);
                    px.SetCamPanAngle(pan_angle);
                    Console.WriteLine();
                    Thread.Sleep(500);
                    //ShowInfo();
                    px.Forward(0);
                    Console.WriteLine();
                }
                else if (key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control)
                {
                    Console.WriteLine("\n Quit");
                    break;
                }
                else {
                    Console.WriteLine($"Unknown key {key}");
                }
            }
        }
        finally
        {
            px.SetCamTiltAngle(0);
            px.SetCamPanAngle(0);
            px.SetDirServoAngle(0);
            px.Stop();
            Thread.Sleep(200);
        }
    }
}
