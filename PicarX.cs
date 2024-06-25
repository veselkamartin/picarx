using System.Device.Gpio;
using System.Device.I2c;
using PicarX;

public class Picarx : IDisposable
{
    private static readonly string CONFIG = "/opt/picar-x/picar-x.conf";

    private static readonly string DEFAULT_LINE_REF = "[1000,1000,1000]";
    private static readonly string DEFAULT_CLIFF_REF = "[500,500,500]";
    private readonly bool _shouldDisposeBus;
    private const int DIR_MIN = -30;
    private const int DIR_MAX = 30;
    private const int CAM_PAN_MIN = -90;
    private const int CAM_PAN_MAX = 90;
    private const int CAM_TILT_MIN = -35;
    private const int CAM_TILT_MAX = 65;

    private const int PERIOD = 4095;
    private const int PRESCALER = 10;
    private const double TIMEOUT = 0.02;

    private FileDB config_file;
    private Servo cam_pan;
    private Servo cam_tilt;
    private Servo dir_servo_pin;
    private GpioPin left_rear_dir_pin;
    private GpioPin right_rear_dir_pin;
    private PWM left_rear_pwm_pin;
    private PWM right_rear_pwm_pin;
    private List<GpioPin> motor_direction_pins;
    private List<PWM> motor_speed_pins;
    //private Grayscale_Module grayscale;
    //private Ultrasonic ultrasonic;
    private List<double> line_reference;
    private List<double> cliff_reference;
    private double dir_cali_val;
    private double cam_pan_cali_val;
    private double cam_tilt_cali_val;
    private List<int> cali_dir_value;
    private List<int> cali_speed_value;
    private double dir_current_angle;
    RobotHat _robotHat;
    private I2cBus _bus;
    private I2cDevice _device;
    private bool _isDisposed;
    private const int ADDR1 = 0x14;
    private const int ADDR2 = 0x15;

    public Picarx(
        GpioController? controller = null, bool shouldDisposeController = false,
        I2cBus? bus = null, bool shouldDisposeBus = false,
        List<string>? servo_pins = null,
        List<string>? motor_pins = null,
        List<string>? grayscale_pins = null,
        List<string>? ultrasonic_pins = null,
        string? config = null)
    {
        _robotHat = new RobotHat(controller, shouldDisposeController);
        _shouldDisposeBus = shouldDisposeBus || bus is null;
        _bus = bus ?? I2cBus.Create(1);
        _device = _bus.CreateDevice(ADDR1);

        servo_pins = servo_pins ?? new List<string> { "P0", "P1", "P2" };
        motor_pins = motor_pins ?? new List<string> { "D4", "D5", "P12", "P13" };
        grayscale_pins = grayscale_pins ?? new List<string> { "A0", "A1", "A2" };
        ultrasonic_pins = ultrasonic_pins ?? new List<string> { "D2", "D3" };
        config = config ?? CONFIG;

        // reset robot_hat
        Utils.ResetMcu();
        Thread.Sleep(200);

        // --------- config_file ---------
        config_file = new FileDB(config);

        // --------- servos init ---------
        cam_pan = new Servo(new PWM(_device, servo_pins[0]));
        cam_tilt = new Servo(new PWM(_device, servo_pins[1]));
        dir_servo_pin = new Servo(new PWM(_device, servo_pins[2]));

        // get calibration values
        dir_cali_val = double.Parse(config_file.Get("picarx_dir_servo", "0"));
        cam_pan_cali_val = double.Parse(config_file.Get("picarx_cam_pan_servo", "0"));
        cam_tilt_cali_val = double.Parse(config_file.Get("picarx_cam_tilt_servo", "0"));

        // set servos to init angle
        dir_servo_pin.SetAngle(dir_cali_val);
        cam_pan.SetAngle(cam_pan_cali_val);
        cam_tilt.SetAngle(cam_tilt_cali_val);

        // --------- motors init ---------
        left_rear_dir_pin = _robotHat.GetPin(motor_pins[0], PinMode.Output);
        right_rear_dir_pin = _robotHat.GetPin(motor_pins[1], PinMode.Output);
        left_rear_pwm_pin = new PWM(_device, motor_pins[2]);
        right_rear_pwm_pin = new PWM(_device, motor_pins[3]);
        motor_direction_pins = new List<GpioPin> { left_rear_dir_pin, right_rear_dir_pin };
        motor_speed_pins = new List<PWM> { left_rear_pwm_pin, right_rear_pwm_pin };

        // get calibration values
        cali_dir_value = config_file.Get("picarx_dir_motor", "[1, 1]")
            .Trim('[', ']')
            .Split(',')
            .Select(int.Parse)
            .ToList();
        cali_speed_value = new List<int> { 0, 0 };
        dir_current_angle = 0;

        // init pwm
        foreach (var pin in motor_speed_pins)
        {
            pin.SetPeriod(PERIOD);
            pin.SetPrescaler(PRESCALER);
        }

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
        //ultrasonic = new Ultrasonic(new Pin(trig), new Pin(echo, PinMode.Input, PinPull.PullDown));
    }

    public void SetMotorSpeed(int motor, int speed)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));
        speed = Constrain(speed, -100, 100);
        Console.WriteLine($"Setting motor {motor} speed to {speed}");
        motor -= 1;
        int direction = speed >= 0 ? 1 * cali_dir_value[motor] : -1 * cali_dir_value[motor];
        speed = Math.Abs(speed);
        if (speed != 0) speed = speed / 2 + 50;
        speed -= cali_speed_value[motor];

        if (direction < 0)
        {
            motor_direction_pins[motor].Write(PinValue.High);
            motor_speed_pins[motor].SetPulseWidthPercent(speed);
        }
        else
        {
            motor_direction_pins[motor].Write(PinValue.Low);
            motor_speed_pins[motor].SetPulseWidthPercent(speed);
        }
    }

    public void MotorSpeedCalibration(int value)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

        if (value < 0)
        {
            cali_speed_value[0] = 0;
            cali_speed_value[1] = Math.Abs(value);
        }
        else
        {
            cali_speed_value[0] = Math.Abs(value);
            cali_speed_value[1] = 0;
        }
    }

    public void MotorDirectionCalibrate(int motor, int value)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

        motor -= 1;
        if (value == 1)
        {
            cali_dir_value[motor] = 1;
        }
        else if (value == -1)
        {
            cali_dir_value[motor] = -1;
        }
        //config_file.Set("picarx_dir_motor", string.Join(", ", cali_dir_value));
    }

    public void DirServoCalibrate(double value)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

        dir_cali_val = value;
        //config_file.Set("picarx_dir_servo", value.ToString());
        dir_servo_pin.SetAngle(value);
    }

    public void SetDirServoAngle(double value)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

        dir_current_angle = Constrain(value, DIR_MIN, DIR_MAX);
        double angle_value = dir_current_angle + dir_cali_val;
        dir_servo_pin.SetAngle(angle_value);
    }

    public void CamPanServoCalibrate(double value)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

        cam_pan_cali_val = value;
        //config_file.Set("picarx_cam_pan_servo", value.ToString());
        cam_pan.SetAngle(value);
    }

    public void CamTiltServoCalibrate(double value)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

        cam_tilt_cali_val = value;
        config_file.Set("picarx_cam_tilt_servo", value.ToString());
        cam_tilt.SetAngle(value);
    }

    public void SetCamPanAngle(double value)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

        value = Constrain(value, CAM_PAN_MIN, CAM_PAN_MAX);
        Console.WriteLine($"Setting Cam pan angle to {value}");
        cam_pan.SetAngle(-1 * (value + -1 * cam_pan_cali_val));
    }

    public void SetCamTiltAngle(double value)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

        value = Constrain(value, CAM_TILT_MIN, CAM_TILT_MAX);
        Console.WriteLine($"Setting Cam tilt angle to {value}");
        cam_tilt.SetAngle(-1 * (value + -1 * cam_tilt_cali_val));
    }

    public void SetPower(int speed)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

        SetMotorSpeed(1, speed);
        SetMotorSpeed(2, speed);
    }

    public void Backward(int speed)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

        double current_angle = dir_current_angle;
        if (current_angle != 0)
        {
            double abs_current_angle = Math.Abs(current_angle);
            if (abs_current_angle > DIR_MAX) abs_current_angle = DIR_MAX;
            double power_scale = (100 - abs_current_angle) / 100.0;
            if ((current_angle / abs_current_angle) > 0)
            {
                SetMotorSpeed(1, -1 * speed);
                SetMotorSpeed(2, (int)(speed * power_scale));
            }
            else
            {
                SetMotorSpeed(1, (int)(-1 * speed * power_scale));
                SetMotorSpeed(2, speed);
            }
        }
        else
        {
            SetMotorSpeed(1, -1 * speed);
            SetMotorSpeed(2, speed);
        }
    }

    public void Forward(int speed)
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

        double current_angle = dir_current_angle;
        if (current_angle != 0)
        {
            double abs_current_angle = Math.Abs(current_angle);
            if (abs_current_angle > DIR_MAX) abs_current_angle = DIR_MAX;
            double power_scale = (100 - abs_current_angle) / 100.0;
            if ((current_angle / abs_current_angle) > 0)
            {
                SetMotorSpeed(1, (int)(speed * power_scale));
                SetMotorSpeed(2, -speed);
            }
            else
            {
                SetMotorSpeed(1, speed);
                SetMotorSpeed(2, (int)(-1 * speed * power_scale));
            }
        }
        else
        {
            SetMotorSpeed(1, speed);
            SetMotorSpeed(2, -1 * speed);
        }
    }

    public void Stop()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

        for (int i = 0; i < 2; i++)
        {
            motor_speed_pins[0].SetPulseWidthPercent(0);
            motor_speed_pins[1].SetPulseWidthPercent(0);
            Thread.Sleep(2);
        }
    }

    //public double GetDistance()
    //{
    //    return ultrasonic.Read();
    //}

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
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

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
        if (_isDisposed) throw new ObjectDisposedException(nameof(Picarx));

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

        if (_shouldDisposeBus)
        {
            _bus?.Dispose();
        }

        _isDisposed = true;
    }

    private int Constrain(int x, int min, int max)
    {
        return Math.Max(min, Math.Min(max, x));
    }

    private double Constrain(double x, double min, double max)
    {
        return Math.Max(min, Math.Min(max, x));
    }
}
