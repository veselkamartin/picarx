using PicarX;
using System.Device.I2c;

public class PWM : ControllerBase
{
    private const byte REG_CHN = 0x20;
    //private const byte REG_FRE = 0x30;
    private const byte REG_PSC = 0x40;
    private const byte REG_ARR = 0x44;

    private const int ADDR1 = 0x14;
    private const int ADDR2 = 0x15;
    public const double CLOCK = 72000000;

    private readonly byte _addr;
    private readonly byte _channel;
    private readonly byte _timer;
    private ushort _pulseWidth;
    private double _freq;
    private ushort _prescaler;
    private int _pulseWidthPercent;
    private readonly I2cBus _bus;
    private readonly I2cDevice _device;

    private static List<ushort> timer = new List<ushort> { 0, 0, 0, 0 };
    public PWM(string channel)
    {

        _bus = CreateI2cBus(1); // Initialize your I2C bus here
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
            throw new ArgumentException($"PWM channel should be between [P0, P15], not {channel}");
        }

        _timer = (byte)(_channel / 4);

        //try
        //{
        //    _device.WriteByte(0x2C);
        //    _device.WriteByte(0);
        //    _device.WriteByte(0);
        //}
        //catch (Exception ex)
        //{
        //    Console.WriteLine(ex.ToString());
        //    _device.Dispose();
        //    _device = _bus.CreateDevice(ADDR2);
        //    _addr = ADDR2;
        //}

        Debug($"PWM address: {_addr:X2}");
        _freq = 50;
        SetFrequency(50);
    }

    public double GetFrequency()
    {
        return _freq;
    }

    public void SetFrequency(double freq)
    {
        _freq = freq;
        var resultAp = new List<(ushort psc, ushort arr)>();
        var resultAcy = new List<double>();
        var st = (ushort)(Math.Sqrt(CLOCK / _freq) - 5);
        if (st <= 0) st = 1;

        for (ushort psc = st; psc < st + 10; psc++)
        {
            ushort arr = (ushort)(CLOCK / _freq / psc);
            resultAp.Add((psc, arr));
            resultAcy.Add(Math.Abs(_freq - (double)CLOCK / psc / arr));
        }

        int i = resultAcy.IndexOf(resultAcy.Min());
        var selectedPsc = resultAp[i].psc;
        var selectedArr = resultAp[i].arr;
        Debug($"{_timer} prescaler: {selectedPsc}, period: {selectedArr}");
        SetPrescaler(selectedPsc);
        SetPeriod(selectedArr);
    }

    public int GetPrescaler()
    {
        return _prescaler;
    }

    public void SetPrescaler(ushort prescaler)
    {
        _prescaler = prescaler;
        _freq = CLOCK / _prescaler / timer[_timer];
        var reg = (byte)(REG_PSC + _timer);
        Debug($"Set prescaler {_timer} to: {_prescaler}");
        _device.WriteWord(reg, (ushort)(_prescaler - 1));
    }

    public int GetPeriod()
    {
        return timer[_timer];
    }

    public void SetPeriod(ushort arr)
    {
        timer[_timer] = (ushort)(arr);
        _freq = CLOCK / _prescaler / timer[_timer];
        var reg = (byte)(REG_ARR + _timer);
        Debug($"Set arr {_timer} to: {timer[_timer]}");
        _device.WriteWord(reg, timer[_timer]);
    }

    public ushort GetPulseWidth()
    {
        return _pulseWidth;
    }

    public void SetPulseWidth(ushort pulseWidth)
    {
        _pulseWidth = pulseWidth;
        var reg = (byte)(REG_CHN + _channel);
        Debug($"Set pulse {_channel} to: {pulseWidth}");
        _device.WriteWord(reg, _pulseWidth);
    }

    public int GetPulseWidthPercent()
    {
        return _pulseWidthPercent;
    }

    public void SetPulseWidthPercent(int pulseWidthPercent)
    {
        _pulseWidthPercent = pulseWidthPercent;
        var temp = _pulseWidthPercent / 100.0;
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
