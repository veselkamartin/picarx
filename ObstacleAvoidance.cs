namespace PicarX;

public class ObstacleAvoidance
{
    static int[] _angles = [30, -30];

    public static void AvoidObstacles(PicarX.Picarx px)
    {

        try
        {
            while (true)
            {
                double distance = Math.Round(px.GetDistance(), 2);
                Console.WriteLine("distance: " + distance);

                if (distance >= 60)
                {
                    px.SetDirServoAngle(0);
                    px.Forward(50);
                }
                else if (distance >= 40)
                {
                    px.SetDirServoAngle(0);
                    px.Forward(20);
                }
                else if (distance >= 20)
                {
                    var angle = GetRandomLeftOrRight();
                    px.SetDirServoAngle(angle);
                    px.Forward(20);
                    Thread.Sleep(100);
                    var newDistance = px.GetDistance();
                    if (newDistance < distance)
                    {
                        angle = -angle;
                        px.SetDirServoAngle(angle);
                        Thread.Sleep(100);
                    }
                }
                else
                {
                    var angle = GetRandomLeftOrRight();

                    px.SetDirServoAngle(angle);
                    px.Backward(20);
                    Thread.Sleep(500);
                }
            }
        }
        finally
        {
            px.Stop();
        }

    }

    private static int GetRandomLeftOrRight()
    {
        return _angles[Random.Shared.Next(_angles.Length)];
    }
}
