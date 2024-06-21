public class Servo  
{
    private const ushort MAX_PW = 2500;
    private const ushort MIN_PW = 500;
    private static readonly int _freq = 50;

    private PWM _pwm;

    public Servo(PWM pwm)
    {
		_pwm = pwm;
        _pwm.SetPeriod(4095);
        var prescaler = (byte)(PWM.CLOCK / _freq / _pwm.GetPeriod());
        _pwm.SetPrescaler(prescaler);
        // SetAngle(90); // Uncomment if needed
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

        double HighLevelTime = Map(angle, -90, 90, MIN_PW, MAX_PW);
        _Debug($"High_level_time: {HighLevelTime}");
        double pwr = HighLevelTime / 20000;
        _Debug($"pulse width rate: {pwr}");
        var value = (ushort)(pwr * _pwm.GetPeriod());
        _Debug($"pulse width value: {value}");
        _pwm.SetPulseWidth(value);
    }

    // pwm_value ranges MIN_PW 500 to MAX_PW 2500 degrees
    public void SetPwm(ushort pwmValue)
    {
        if (pwmValue > MAX_PW)
        {
            pwmValue = MAX_PW;
        }
        if (pwmValue < MIN_PW)
        {
            pwmValue = MIN_PW;
        }

        _pwm.SetPulseWidth(pwmValue);
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