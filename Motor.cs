using System.Device.Gpio;

namespace PicarX;

public class Motor
{
    private const ushort PERIOD = 4095;
    private const byte PRESCALER = 10;

    private readonly Dictionary<MotorEnum, PWM> motorSpeedPins;
    private readonly Dictionary<MotorEnum, GpioPin> motorDirectionPins;

    public class MotorCalibration
    {
        public Dictionary<MotorEnum, int> Direction { get; set; } = new();
        public Dictionary<MotorEnum, int> Speed { get; set; } = new();
    }
    public MotorCalibration Calibration { get; set; } = new MotorCalibration();

    public Motor(
        PWM leftRearPwmPin,
        PWM rightRearPwmPin,
        GpioPin leftRearDirPin,
        GpioPin rightRearDirPin)
    {
        motorSpeedPins = new() { { MotorEnum.Left, leftRearPwmPin }, { MotorEnum.Right, rightRearPwmPin } };
        motorDirectionPins = new() { { MotorEnum.Left, leftRearDirPin }, { MotorEnum.Right, rightRearDirPin } };

        // Initialize PWM pins
        foreach (var pin in motorSpeedPins.Values)
        {
            pin.SetPeriod(PERIOD);
            pin.SetPrescaler(PRESCALER);
        }
    }

    // Control motor direction and speed
    // motor 0 or 1,
    // dir 0 or 1
    // speed 0 ~ 100
    public void SetMotorSpeed(MotorEnum motor, int speed)
    {
        speed = Math.Clamp(speed, -100, 100);
        Console.WriteLine($"Setting motor {motor} speed to {speed}");
        motor -= 1;
        int direction = speed >= 0 ? 1 * Calibration.Direction[motor] : -1 * Calibration.Direction[motor];
        speed = Math.Abs(speed);
        if (speed != 0) speed = speed / 2 + 50;
        speed -= Calibration.Speed[motor];

        if (direction < 0)
        {
            motorDirectionPins[motor].Write(PinValue.High);
            motorSpeedPins[motor].SetPulseWidthPercent(speed);
        }
        else
        {
            motorDirectionPins[motor].Write(PinValue.Low);
            motorSpeedPins[motor].SetPulseWidthPercent(speed);
        }
    }
    public void Stop()
    {
        //Do twice to make sure
        motorSpeedPins[MotorEnum.Left].SetPulseWidthPercent(0);
        motorSpeedPins[MotorEnum.Right].SetPulseWidthPercent(0);
        Thread.Sleep(2);
        motorSpeedPins[MotorEnum.Left].SetPulseWidthPercent(0);
        motorSpeedPins[MotorEnum.Right].SetPulseWidthPercent(0);
    }
}
public enum MotorEnum
{
    Right,
    Left
}