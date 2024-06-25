namespace PicarX;

public class Motor
{
	private const ushort PERIOD = 4095;
	private const byte PRESCALER = 10;

    private const int ADDR1 = 0x14;
    private const int ADDR2 = 0x15;


    private PWM leftRearPwmPin;
	private PWM rightRearPwmPin;
	private Pin leftRearDirPin;
	private Pin rightRearDirPin;
	private PWM[] motorSpeedPins;

	public Motor()
	{
		var bus = ControllerBase.CreateI2cBus(1);
        const int ADDR1 = 0x14;
        const int ADDR2 = 0x15;
        var device = bus.CreateDevice(ADDR1);


        leftRearPwmPin = new PWM(device,"P13");
		rightRearPwmPin = new PWM(device, "P12");
		leftRearDirPin = new Pin("D4", System.Device.Gpio.PinMode.Output);
		rightRearDirPin = new Pin("D5", System.Device.Gpio.PinMode.Output);

		motorSpeedPins = new PWM[] { leftRearPwmPin, rightRearPwmPin };

		// Initialize PWM pins
		foreach (var pin in motorSpeedPins)
		{
			pin.SetPeriod(PERIOD);
			pin.SetPrescaler(PRESCALER);
		}
	}

	// Control motor direction and speed
	// motor 0 or 1,
	// dir 0 or 1
	// speed 0 ~ 100
	public void Wheel(int speed, int motor = -1)
	{
		var dir = speed > 0;
		speed = Math.Abs(speed);

		if (speed != 0)
		{
			speed = speed / 2 + 50;
		}

		if (motor == 0)
		{
			leftRearDirPin.SetValue(dir);
			leftRearPwmPin.SetPulseWidthPercent(speed);
		}
		else if (motor == 1)
		{
			rightRearDirPin.SetValue(dir);
			rightRearPwmPin.SetPulseWidthPercent(speed);
		}
		else if (motor == -1)
		{
			leftRearDirPin.SetValue(dir);
			leftRearPwmPin.SetPulseWidthPercent(speed);
			rightRearDirPin.SetValue(dir);
			rightRearPwmPin.SetPulseWidthPercent(speed);
		}
	}
}