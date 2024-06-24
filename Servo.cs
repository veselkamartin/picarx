public class Servo  
{
    private const ushort MAX_PW = 2500;
    private const ushort MIN_PW = 500;
    public static readonly int FREQ = 50;
    public const ushort PERIOD = 4095;
    private PWM _pwm;

    public Servo(PWM pwm)
    {
		_pwm = pwm;
        _pwm.SetPeriod(PERIOD);
        var prescaler = (ushort)(PWM.CLOCK / FREQ /PERIOD);
        _pwm.SetPrescaler(prescaler);
    }

    // angle ranges -90 to 90 degrees
    public void SetAngle(double angle)
    {
        if (angle < -90)
        {
            angle = -90;
        }
        if (angle > 90)
        {
            angle = 90;
        }

        _Debug($"Servo {_pwm.Channel} set angle to: {angle}");
        double pulseWidthTime = Map(angle, -90, 90, MIN_PW, MAX_PW);
        //_Debug($"Pulse width: {pulseWidthTime}");
        SetPulseWidthTime(pulseWidthTime);
    }

    // pwm_value ranges MIN_PW 500 to MAX_PW 2500 degrees
    public void SetPulseWidthTime(double pulseWidthTime)
    {
        if (pulseWidthTime > MAX_PW)
        {
            pulseWidthTime = MAX_PW;
        }
        if (pulseWidthTime < MIN_PW)
        {
            pulseWidthTime = MIN_PW;
        }

        var pwr = pulseWidthTime / 20000;
        //_Debug($"pulse width rate: {pwr}");
        var value = (ushort)(pwr * PERIOD);
        //_Debug($"pulse width value: {value}");
        _pwm.SetPulseWidth(value);
    }

    private double Map(double x, double in_min, double in_max, double out_min, double out_max)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

    private void _Debug(string message)
    {
        Console.WriteLine(message); // Replace with actual debug method
    }
}