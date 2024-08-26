using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace SmartCar.RobotHat
{

	//public class ADXL345 : I2C
	//   {
	//       private const int ADDR = 0x53;
	//       private const int REG_DATA_X = 0x32;
	//       private const int REG_DATA_Y = 0x34;
	//       private const int REG_DATA_Z = 0x36;
	//       private const int REG_POWER_CTL = 0x2D;
	//       private static readonly int[] AXISES = { REG_DATA_X, REG_DATA_Y, REG_DATA_Z };

	//       public ADXL345(int address = ADDR, int bus = 1) : base(address, bus) { }

	//       public List<float> Read(int? axis = null)
	//       {
	//           if (axis == null)
	//           {
	//               var result = new List<float>();
	//               for (int i = 0; i < 3; i++)
	//               {
	//                   result.Add(ReadAxis(i));
	//               }
	//               return result;
	//           }
	//           else
	//           {
	//               return new List<float> { ReadAxis(axis.Value) };
	//           }
	//       }

	//       private float ReadAxis(int axis)
	//       {
	//           Write((0x08 << 8) + REG_POWER_CTL);
	//           MemWrite(0, 0x31);
	//           MemWrite(8, 0x2D);
	//           var raw = MemRead(2, AXISES[axis]);

	//           MemWrite(0, 0x31);
	//           MemWrite(8, 0x2D);
	//           raw = MemRead(2, AXISES[axis]);

	//           int rawValue;
	//           if ((raw[1] >> 7) == 1)
	//           {
	//               var raw1 = raw[1] ^ 128 ^ 127;
	//               rawValue = (raw1 + 1) * -1;
	//           }
	//           else
	//           {
	//               rawValue = raw[1];
	//           }
	//           var g = (rawValue << 8) | raw[0];
	//           return g / 256.0f;
	//       }
	//   }

	//   public class RGB_LED
	//   {
	//       public const int ANODE = 1;
	//       public const int CATHODE = 0;
	//       private readonly PWM rPin, gPin, bPin;
	//       private readonly int common;

	//       public RGB_LED(PWM rPin, PWM gPin, PWM bPin, int common = ANODE)
	//       {
	//           if (rPin == null || gPin == null || bPin == null)
	//               throw new ArgumentException("rPin, gPin, and bPin must be PWM objects");

	//           if (common != ANODE && common != CATHODE)
	//               throw new ArgumentException("common must be ANODE or CATHODE");

	//           this.rPin = rPin;
	//           this.gPin = gPin;
	//           this.bPin = bPin;
	//           this.common = common;
	//       }

	//       public void SetColor(object color)
	//       {
	//           int r, g, b;

	//           switch (color)
	//           {
	//               case string hexString:
	//                   hexString = hexString.TrimStart('#');
	//                   var hexValue = int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
	//                   r = (hexValue & 0xff0000) >> 16;
	//                   g = (hexValue & 0x00ff00) >> 8;
	//                   b = (hexValue & 0x0000ff);
	//                   break;

	//               case int intValue:
	//                   r = (intValue & 0xff0000) >> 16;
	//                   g = (intValue & 0x00ff00) >> 8;
	//                   b = (intValue & 0x0000ff);
	//                   break;

	//               case Tuple<int, int, int> tupleValue:
	//                   r = tupleValue.Item1;
	//                   g = tupleValue.Item2;
	//                   b = tupleValue.Item3;
	//                   break;

	//               case List<int> listValue:
	//                   if (listValue.Count != 3)
	//                       throw new ArgumentException("List must contain exactly 3 integers");
	//                   r = listValue[0];
	//                   g = listValue[1];
	//                   b = listValue[2];
	//                   break;

	//               default:
	//                   throw new ArgumentException("color must be string, int, tuple, or list");
	//           }

	//           if (common == ANODE)
	//           {
	//               r = 255 - r;
	//               g = 255 - g;
	//               b = 255 - b;
	//           }

	//           rPin.PulseWidthPercent(r / 255.0 * 100);
	//           gPin.PulseWidthPercent(g / 255.0 * 100);
	//           bPin.PulseWidthPercent(b / 255.0 * 100);
	//       }
	//   }

	//   public class Buzzer
	//   {
	//       private readonly object buzzer;

	//       public Buzzer(object buzzer)
	//	{
	//           if (!(buzzer is PWM) && !(buzzer is Pin))
	//               throw new ArgumentException("buzzer must be PWM or Pin object");

	//           this.buzzer = buzzer;
	//           Off();
	//       }

	//       public void On()
	//       {
	//           if (buzzer is PWM pwm)
	//           {
	//               pwm.PulseWidthPercent(50);
	//		}
	//           else if (buzzer is Pin pin)
	//           {
	//               pin.On();
	//           }
	//       }

	//       public void Off()
	//       {
	//           if (buzzer is PWM pwm)
	//           {
	//               pwm.PulseWidthPercent(0);
	//		}
	//           else if (buzzer is Pin pin)
	//           {
	//               pin.Off();
	//           }
	//       }

	//       public void SetFrequency(double freq)
	//	{
	//           if (buzzer is Pin)
	//               throw new InvalidOperationException("SetFrequency is not supported for active buzzer");

	//           if (buzzer is PWM pwm)
	//           {
	//               pwm.Frequency = freq;
	//           }
	//       }

	//       public void Play(double freq, double? duration = null)
	//       {
	//           SetFrequency(freq);
	//           On();
	//           if (duration != null)
	//           {
	//               Task.Delay((int)(duration.Value * 500)).Wait();
	//               Off();
	//               Task.Delay((int)(duration.Value * 500)).Wait();
	//           }
	//       }
	//   }

	//   public class GrayscaleModule
	//   {
	//       public const int LEFT = 0;
	//       public const int MIDDLE = 1;
	//       public const int RIGHT = 2;
	//       private static readonly int[] DEFAULT_REFERENCE = { 1000, 1000, 1000 };

	//       private readonly ADC[] pins;
	//       private int[] reference;

	//       public GrayscaleModule(ADC pin0, ADC pin1, ADC pin2, int[] reference = null)
	//       {
	//           if (pin0 == null || pin1 == null || pin2 == null)
	//               throw new ArgumentException("All pins must be ADC objects");

	//           this.pins = new[] { pin0, pin1, pin2 };
	//           this.reference = reference ?? DEFAULT_REFERENCE;
	//       }

	//       public int[] GetReference(int[] refValue = null)
	//       {
	//           if (refValue != null)
	//           {
	//               if (refValue.Length != 3)
	//                   throw new ArgumentException("Reference value must be an array of 3 integers");

	//               this.reference = refValue;
	//           }
	//           return this.reference;
	//       }

	//       public int[] ReadStatus(int[] data = null)
	//       {
	//           data ??= Read();

	//           var status = new int[3];
	//           for (int i = 0; i < 3; i++)
	//           {
	//               status[i] = data[i] > this.reference[i] ? 0 : 1;
	//           }
	//           return status;
	//       }

	//       public int[] Read(int? channel = null)
	//       {
	//           if (channel == null)
	//           {
	//               var data = new int[3];
	//               for (int i = 0; i < 3; i++)
	//               {
	//                   data[i] = pins[i].Read();
	//               }
	//               return data;
	//           }
	//           else
	//           {
	//               return new[] { pins[channel.Value].Read() };
	//           }
	//       }
	//   }
}
