using System.Device.I2c;

public class PWM : ControllerBase
{
    private const byte REG_CHN = 0x20;
    private const byte REG_FRE = 0x30;
    private const byte REG_PSC = 0x40;
    private const byte REG_ARR = 0x44;

    private const int ADDR1 = 0x14;
    private const int ADDR2 = 0x15;
    private static readonly int CLOCK = 72000000;

    private readonly byte _addr;
    private readonly byte _channel;
    private readonly byte _timer;
    private ushort _pulseWidth;
    private int _freq;
    private byte _prescaler;
    private int _pulseWidthPercent;
    private I2cBus _bus;
    private I2cDevice _device;

    private static List<ushort> timer = new List<ushort> { 0, 0, 0, 0 };

    public PWM(string channel)
    {

        _bus = I2cBus.Create(1); // Initialize your I2C bus here
        _device = _bus.CreateDevice(ADDR1);
        _addr = ADDR1;


        if (channel.StartsWith("P"))
        {
            _channel = byte.Parse(channel.Substring(1));
            if (_channel > 14)
                throw new ArgumentException("channel must be in range of 0-14");
        }
        else
        {
            throw new ArgumentException($"PWM channel should be between [P0, P11], not {channel}");
        }

        _timer = (byte)(_channel / 4);

        try
        {
            _device.WriteByte(0x2C);
            _device.WriteByte(0);
            _device.WriteByte(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            _device.Dispose();
            _device = _bus.CreateDevice(ADDR2);
            _addr = ADDR2;
        }

        Debug($"PWM address: {_addr:X2}");
        _freq = 50;
        SetFrequency(50);
    }

    private void I2CWrite(byte reg, ushort value)
    {
        byte valueH = (byte)(value >> 8);
        byte valueL = (byte)(value & 0xff);
        Debug($"i2c write: [0x{_addr:X2}, 0x{reg:X2}, 0x{valueH:X2}, 0x{valueL:X2}]");
        _device.Write([reg, valueH, valueL]);
    }

    public int GetFrequency()
    {
        return _freq;
    }

    public void SetFrequency(int freq)
    {
        _freq = freq;
        var resultAp = new List<(byte psc, ushort arr)>();
        List<double> resultAcy = new List<double>();
        byte st = (byte)(Math.Sqrt(CLOCK / _freq) - 5);
        if (st <= 0) st = 1;

        for (byte psc = st; psc < st + 10; psc++)
        {
            ushort arr = (ushort)((double)CLOCK / _freq / psc);
            resultAp.Add((psc, arr));
            resultAcy.Add(Math.Abs(_freq - (double)CLOCK / psc / arr));
        }

        int i = resultAcy.IndexOf(resultAcy.Min());
        var selectedPsc = resultAp[i].psc;
        var selectedArr = resultAp[i].arr;
        Debug($"prescaler: {selectedPsc}, period: {selectedArr}");
        SetPrescaler(selectedPsc);
        SetPeriod(selectedArr);
    }

    public int GetPrescaler()
    {
        return _prescaler;
    }

    public void SetPrescaler(byte prescaler)
    {
        _prescaler = (byte)(prescaler - 1);
        var reg = (byte)(REG_PSC + _timer);
        Debug($"Set prescaler to: {_prescaler}");
        I2CWrite(reg, _prescaler);
    }

    public int GetPeriod()
    {
        return timer[_timer];
    }

    public void SetPeriod(ushort arr)
    {
        timer[_timer] = (ushort)(arr - 1);
        var reg = (byte)(REG_ARR + _timer);
        Debug($"Set arr to: {timer[_timer]}");
        I2CWrite(reg, timer[_timer]);
    }

    public ushort GetPulseWidth()
    {
        return _pulseWidth;
    }

    public void SetPulseWidth(ushort pulseWidth)
    {
        _pulseWidth = pulseWidth;
        var reg = (byte)(REG_CHN + _channel);
        I2CWrite(reg, _pulseWidth);
    }

    public int GetPulseWidthPercent()
    {
        return _pulseWidthPercent;
    }

    public void SetPulseWidthPercent(int pulseWidthPercent)
    {
        _pulseWidthPercent = pulseWidthPercent;
        double temp = _pulseWidthPercent / 100.0;
        var pulseWidth = (ushort)(temp * timer[_timer]);
        SetPulseWidth(pulseWidth);
    }

    private void Debug(string message)
    {
        Console.WriteLine(message);
    }



    public static void Test()
    {
        PWM p = new PWM("P0");
        p.SetPeriod(1000);
        p.SetPrescaler(10);
        while (true)
        {
            for (ushort i = 0; i <= 4095; i += 10)
            {
                p.SetPulseWidth(i);
                Console.WriteLine(i);
                Thread.Sleep(1 / 4095);
            }
            Thread.Sleep(1000);
            for (ushort i = 4095; i >= 0; i -= 10)
            {
                p.SetPulseWidth(i);
                Console.WriteLine(i);
                Thread.Sleep(1 / 4095);
            }
            Thread.Sleep(1000);
        }
    }


}
