using System;

namespace ScadaProject
{
    public abstract class IoDevice
    {
        public readonly int address;
        public int value;

        public IoDevice(int address, int value)
        {

            this.address = address;
            this.value = value;
        }

        public void SetValue(int highByte, int lowByte) { value = (highByte << 8) + lowByte; }
        public abstract string GetValue();
    }

    public class AnalogInput : IoDevice
    {
        public AnalogInput(int address, int value) : base(address, value) { }

        public override string GetValue()
        {
            /* 2 bytes
             * first 11 bits contain integer part (with sign)
             * last 5 bits contain fractional part
             */
            try
            {
                short value = (short)this.value;
                string sign = "";
                if (value < 0)
                {
                    sign = "-";
                    value *= -1;
                }

                int integerPart = value >> 5;
                int fractionalPart = value & 0b11111;

                // fractional part takes value from 0 to 31 -> casting to values 0-100 is necessary
                double parsedFractionalPart = fractionalPart / 32.0 * 100;
                int finalFractionalPart =
                    (int)Math.Round(parsedFractionalPart, 0, MidpointRounding.AwayFromZero);

                return sign + integerPart.ToString() + "," + finalFractionalPart.ToString("D2") + "°C";
            }
            catch (Exception ex)
            {
                throw new Exception("Cannot parse fixed-point value: " + ex.Message);
            }
        }
    }

    public class DigitalInput : IoDevice
    {
        public readonly int bit;

        public DigitalInput(int address, int value, int bit) : base(address, value) 
        {
            this.bit = bit;
        }

        public override string GetValue()
        {
            /* 2 bytes
             * only one bit is checking
             */
            try
            {
                int expectedValue = (value & (int)Math.Pow(2, bit)) >> bit;

                return (expectedValue == 0) ? "OFF" : "ON";
            }
            catch (Exception ex)
            {
                throw new Exception("Cannot parse binary value: " + ex.Message);
            }
        }
    }

    class AnalogOutput : IoDevice
    {
        public AnalogOutput(int address, int value) : base(address, value) { }

        public override string GetValue()
        {
            /* 2 bytes, but value is 1 byte
             * cast value to percentages
             */
            try
            {
                short value = (short)this.value;
                string sign = "";
                if (value < 0)
                {
                    sign = "-";
                    value *= -1;
                }

                int byteValue = value & 0xFF;
                int percentValue = (int)Math.Round(byteValue / 255.0 * 100, MidpointRounding.AwayFromZero);
                if (value < 0)
                {
                    percentValue = 100 - percentValue;
                }

                return sign + percentValue.ToString() + "%";
            }
            catch (Exception ex)
            {
                throw new Exception("Cannot parse analog output value: " + ex.Message);
            }
        }
    }

    class DigitalOutput : IoDevice
    {
        public readonly int bit;

        public DigitalOutput(int address, int value, int bit) : base(address, value)
        {
            this.bit = bit;
        }

        public override string GetValue()
        {
            /* 2 bytes
             * only one bit is checking
             */
            try
            {
                int expectedValue = (value & (int)Math.Pow(2, bit)) >> bit;

                return (expectedValue == 0) ? "OFF" : "ON";
            }
            catch (Exception ex)
            {
                throw new Exception("Cannot parse binary value: " + ex.Message);
            }
        }
    }
}
