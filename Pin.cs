using System.Device.Gpio;

public class Pin : ControllerBase// Replace _Basic_class with the actual base class
{
    private Dictionary<string, int> _dict = new Dictionary<string, int>
    {
        { "BOARD_TYPE", 12 }
    };

    private Dictionary<string, int> _dict_1 = new Dictionary<string, int>
    {
        { "D0",  17 },
        { "D1",  18 },
        { "D2",  27 },
        { "D3",  22 },
        { "D4",  23 },
        { "D5",  24 },
        { "D6",  25 },
        { "D7",  4 },
        { "D8",  5 },
        { "D9",  6 },
        { "D10", 12 },
        { "D11", 13 },
        { "D12", 19 },
        { "D13", 16 },
        { "D14", 26 },
        { "D15", 20 },
        { "D16", 21 },
        { "SW",  19 },
        { "USER", 19 },
        { "LED", 26 },
        { "BOARD_TYPE", 12 },
        { "RST", 16 },
        { "BLEINT", 13 },
        { "BLERST", 20 },
        { "MCURST", 21 }
    };

    private Dictionary<string, int> _dict_2 = new Dictionary<string, int>
    {
        { "D0",  17 },
        { "D1",  4 }, // Changed
        { "D2",  27 },
        { "D3",  22 },
        { "D4",  23 },
        { "D5",  24 },
        { "D6",  25 }, // Removed
        { "D7",  4 }, // Removed
        { "D8",  5 }, // Removed
        { "D9",  6 },
        { "D10", 12 },
        { "D11", 13 },
        { "D12", 19 },
        { "D13", 16 },
        { "D14", 26 },
        { "D15", 20 },
        { "D16", 21 },
        { "SW",  25 }, // Changed
        { "USER", 25 },
        { "LED", 26 },
        { "BOARD_TYPE", 12 },
        { "RST", 16 },
        { "BLEINT", 13 },
        { "BLERST", 20 },
        { "MCURST", 5 } // Changed
    };

    private int _pin;
    private int _value;
    //private int? _pull;
    private PinMode? _mode;
    private string _board_name;

    public Pin(string pin, PinMode? mode/*, int? pull*/)
    {
        //GPIO.setmode(GPIO.BCM);
        //GPIO.setwarnings(false);

        check_board_type();


        _board_name = pin;
        if (_dict.ContainsKey(pin))
        {
            _pin = _dict[pin];
        }
        else
        {
            throw new ArgumentException($"Pin should be in {_dict.Keys}, not {pin}");
        }



        _mode = mode;
        // _pull = pull;
        _value = 0;
        if (mode.HasValue)
        {
            //if (pull.HasValue)
            //{
            //    GPIO.setup(_pin, mode.Value, pull.Value);
            //}
            EnsureOpenPin(_pin, mode.Value);
        }
        Console.WriteLine("Pin init finished.");
    }

    private void check_board_type()
    {
        int type_pin = _dict["BOARD_TYPE"];
        var pin = EnsureOpenPin(type_pin, PinMode.Input);
        if (pin.Read() == 0)
        {
            Console.WriteLine("using board type 1");
            _dict = _dict_1;
        }
        else
        {
            Console.WriteLine("using board type 2");
            _dict = _dict_2;
        }
    }







    public bool GetValue()
    {
        var pin = EnsureOpenPin(_pin, PinMode.Output);
        var value =pin.Read();
        return value == PinValue.High;
    }
    public void SetValue(bool value)
    {
        var pin = EnsureOpenPin(_pin, PinMode.Output);
        pin.Write(value ? PinValue.High : PinValue.Low);
    }

    public void on()
    {
         SetValue(true);
    }

    public void off()
    {
         SetValue(false);
    }

    public void high()
    {
         on();
    }

    public void low()
    {
         off();
    }

    public PinMode Mode
    {
        get { return _mode ?? PinMode.Input; }
        set
        {
            _mode = value;

            EnsureOpenPin(_pin, _mode.Value);


        }
    }



    //public int? pull(params int[] value)
    //{
    //    return _pull;
    //}

    //public void irq(Action handler, int trigger, int bouncetime = 200)
    //{
    //    mode(IN);
    //    GPIO.add_event_detect(_pin, trigger, handler, bouncetime);
    //}

    public string name()
    {
        return $"GPIO{_pin}";
    }

    public List<string> names()
    {
        return new List<string> { name(), _board_name };
    }

    public class cpu
    {
        public const int GPIO17 = 17;
        public const int GPIO18 = 18;
        public const int GPIO27 = 27;
        public const int GPIO22 = 22;
        public const int GPIO23 = 23;
        public const int GPIO24 = 24;
        public const int GPIO25 = 25;
        public const int GPIO26 = 26;
        public const int GPIO4 = 4;
        public const int GPIO5 = 5;
        public const int GPIO6 = 6;
        public const int GPIO12 = 12;
        public const int GPIO13 = 13;
        public const int GPIO19 = 19;
        public const int GPIO16 = 16;
        public const int GPIO20 = 20;
        public const int GPIO21 = 21;
    }
}

public class ControllerBase
{
    protected static GpioController? StaticGpioController;
    protected GpioController GpioController
    {
        get
        {
            if (StaticGpioController == null)
            {
                StaticGpioController = new GpioController();
            }
            return StaticGpioController;
        }
        set
        {
            StaticGpioController = value;
        }
    }

    protected GpioPin EnsureOpenPin(int pinId, PinMode mode)
    {
        if (this.GpioController.IsPinOpen(pinId))
        {
            if (this.GpioController.GetPinMode(pinId) != mode)
            {
                this.GpioController.SetPinMode(pinId, mode);
            }
            return this.GpioController.OpenPin(pinId);
        }
        else
        {
            return this.GpioController.OpenPin(pinId, mode);
        }
    }
}