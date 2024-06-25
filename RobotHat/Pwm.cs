using PicarX;
using System.Device.I2c;

public class PWM
{
    private const byte REG_CHN = 0x20;
    //private const byte REG_FRE = 0x30;
    private const byte REG_PSC = 0x40;
    private const byte REG_ARR = 0x44;

    public const double CLOCK = 72000000;

    public byte Channel { get; }
    private readonly byte _group;
    private int _pulseWidth;
    private double _freq;
    private int _prescaler;
    private int _pulseWidthPercent;
    private readonly I2cDevice _device;

    private static List<int> _groupPeriod = new List<int> { 0, 0, 0, 0 };
    public PWM(I2cDevice device, string channel)
    {
        _device = device;

        if (channel.StartsWith("P"))
        {
            Channel = byte.Parse(channel.Substring(1));
            if (Channel > 14)
                throw new ArgumentException("channel must be in range of 0-14");
        }
        else
        {
            throw new ArgumentException($"PWM channel should be between [P0, P15], not {channel}");
        }

        _group = (byte)(Channel / 4);

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

        _freq = 50;
        SetFrequency(50);
    }

    public double GetFrequency()
    {
        return _freq;
    }

    public void SetFrequency(double freq)
    {
        Debug($"PWM {Channel} {_group} Set frequency: {freq}");
        _freq = freq;
        var resultAp = new List<(int psc, int arr)>();
        var resultAcy = new List<double>();
        var st = (int)(Math.Sqrt(CLOCK / _freq) - 5);
        if (st <= 0) st = 1;

        for (int psc = st; psc < st + 10; psc++)
        {
            var arr = (int)(CLOCK / _freq / psc);
            resultAp.Add((psc, arr));
            resultAcy.Add(Math.Abs(_freq - CLOCK / psc / arr));
        }

        int i = resultAcy.IndexOf(resultAcy.Min());
        var selectedPsc = resultAp[i].psc;
        var selectedArr = resultAp[i].arr;
        Debug($"PWM {Channel} {_group} prescaler: {selectedPsc}, period: {selectedArr}");
        SetPrescaler(selectedPsc);
        SetPeriod(selectedArr);
    }

    public int GetPrescaler()
    {
        return _prescaler;
    }

    public void SetPrescaler(int prescaler)
    {
        _prescaler = prescaler;
        _freq = CLOCK / _prescaler / _groupPeriod[_group];
        var reg = (byte)(REG_PSC + _group);
        Debug($"PWM {Channel} {_group} Set prescaler to: {_prescaler}");
        _device.WriteWord(reg, _prescaler - 1);
    }

    public int GetPeriod()
    {
        return _groupPeriod[_group];
    }

    public void SetPeriod(int arr)
    {
        _groupPeriod[_group] = arr;
        _freq = CLOCK / _prescaler / _groupPeriod[_group];
        var reg = (byte)(REG_ARR + _group);
        Debug($"PWM {Channel} {_group} Set arr to: {_groupPeriod[_group]}");
        _device.WriteWord(reg, _groupPeriod[_group]);
    }

    public int GetPulseWidth()
    {
        return _pulseWidth;
    }

    public void SetPulseWidth(int pulseWidth)
    {
        _pulseWidth = pulseWidth;
        var reg = (byte)(REG_CHN + Channel);
        Debug($"PWM {Channel} set pulse to: {pulseWidth}");
        _device.WriteWord(reg, _pulseWidth);
    }

    public int GetPulseWidthPercent()
    {
        return _pulseWidthPercent;
    }

    public void SetPulseWidthPercent(int pulseWidthPercent)
    {
        Debug($"Set pulse {Channel} percent to: {pulseWidthPercent}");
        _pulseWidthPercent = pulseWidthPercent;
        var temp = _pulseWidthPercent / 100.0;
        var pulseWidth = (int)(temp * _groupPeriod[_group]);
        SetPulseWidth(pulseWidth);
    }

    private void Debug(string message)
    {
        Console.WriteLine(message);
    }
}
