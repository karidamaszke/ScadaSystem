using System;
using System.Linq;
using System.IO.Ports;
using System.Threading;
using System.Windows;

namespace ScadaProject
{
    public enum FunctionCode
    {
        READ = 3,
        WRITE = 6,
    }

    class Scada
    {
        private readonly SerialPort serialPort = new SerialPort();
        private ControlSystem controlSystem = new ControlSystem();

        private const int MESSAGE_LENGTH = 8;
        private const int REGISTERS_TO_READ = 1;

        public bool IsInitialized { get; private set; }
        private int controllerAddress = 0;

        public void OpenSerialPort(string portName, int baudRate, int controllerAddress)
        {
            this.IsInitialized = false;
            this.controllerAddress = controllerAddress;

            // close port if was already open
            if (serialPort.IsOpen)
                serialPort.Close();

            // open serial port
            serialPort.PortName = portName;
            serialPort.BaudRate = baudRate;
            serialPort.Parity = Parity.None;
            serialPort.StopBits = StopBits.One;
            serialPort.DataBits = 8;

            serialPort.Open();
            MessageBox.Show("Serial port opened properly!", "Success");

            this.IsInitialized = true;
        }

        public void CloseSerialPort()
        {
            IsInitialized = false;
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                MessageBox.Show("Serial port closed properly!", "Success");
            }
        }

        public int GetDeviceCount()
        {
            return controlSystem.devices.Count;
        }

        public string GetValue(int index)
        {
            if (!IsInitialized)
                throw new Exception("Set transmission parameters first!");

            if (index >= controlSystem.devices.Count)
                throw new Exception("Unrecognized device!");

            return controlSystem.devices[index].GetValue();
        }

        public void ReadAllValues()
        {
            var analogInputs = ReadValuesInRange(0, 9);
            controlSystem.devices[0].SetValue(analogInputs[0], analogInputs[1]);
            controlSystem.devices[1].SetValue(analogInputs[4], analogInputs[5]);
            controlSystem.devices[2].SetValue(analogInputs[16], analogInputs[17]);

            var digitalInputs = ReadValuesInRange(3, 2);
            controlSystem.devices[3].SetValue(digitalInputs[0], digitalInputs[1]);
            controlSystem.devices[4].SetValue(digitalInputs[2], digitalInputs[3]);
            controlSystem.devices[5].SetValue(digitalInputs[2], digitalInputs[3]);

            var analogOutputs = ReadValuesInRange(6, 24);
            controlSystem.devices[6].SetValue(analogOutputs[0], analogOutputs[1]);
            controlSystem.devices[7].SetValue(analogOutputs[2], analogOutputs[3]);
            controlSystem.devices[8].SetValue(analogOutputs[20], analogOutputs[21]);
            controlSystem.devices[9].SetValue(analogOutputs[22], analogOutputs[23]);
            controlSystem.devices[10].SetValue(analogOutputs[46], analogOutputs[47]);

            var digitalOutputs = ReadValuesInRange(11, 3);
            controlSystem.devices[11].SetValue(digitalOutputs[0], digitalOutputs[1]);
            controlSystem.devices[12].SetValue(digitalOutputs[0], digitalOutputs[1]);
            controlSystem.devices[13].SetValue(digitalOutputs[2], digitalOutputs[3]);
            controlSystem.devices[14].SetValue(digitalOutputs[2], digitalOutputs[3]);
            controlSystem.devices[15].SetValue(digitalOutputs[3], digitalOutputs[4]);
            controlSystem.devices[16].SetValue(digitalOutputs[3], digitalOutputs[4]);
        }

        public byte[] ReadValuesInRange(int startIndex, int count)
        {
            if (!IsInitialized)
                throw new Exception("Set transmission parameters first!");

            if (startIndex >= controlSystem.devices.Count)
                throw new Exception("Unrecognized device!");

            // build and send request
            byte[] message = new byte[8];
            BuildMessage(ref message, FunctionCode.READ.GetHashCode(), controlSystem.devices[startIndex].address, count);
            SendMessage(message);

            // check if there is response
            int bytesReceived = serialPort.BytesToRead;
            if (bytesReceived > 0)
            {
                // read message
                byte[] receivedMessage = new byte[bytesReceived];
                for (int i = 0; i < bytesReceived; i++)
                    receivedMessage[i] = (byte)serialPort.ReadByte();

                if (message[0] != receivedMessage[0])
                    throw new Exception("Received message from unexpected controller address!");

                if (receivedMessage[1] == FunctionCode.READ.GetHashCode())
                {
                    uint checksum = CalculateChecksum(receivedMessage, receivedMessage.Length - 2);
                    if (receivedMessage[bytesReceived - 1] != (checksum >> 8) || receivedMessage[bytesReceived - 2] != (checksum & 0xFF))
                        throw new Exception("Received wrong checksum!");

                    int bytesToRead = receivedMessage[2];

                    return receivedMessage.Skip(3).Take(bytesToRead).ToArray();
                }
                else
                {
                    HandleError(receivedMessage[2]);
                    return new byte[0];
                }
            }
            else
            {
                throw new Exception("Empty response! Make sure you provided proper controller address.");
            }
        }

        public void ReadValue(int index)
        {
            if (!IsInitialized)
                throw new Exception("Set transmission parameters first!");

            if (index >= controlSystem.devices.Count)
                throw new Exception("Unrecognized device!");

            // build and send request
            byte[] message = new byte[8];
            BuildMessage(ref message, FunctionCode.READ.GetHashCode(),
                controlSystem.devices[index].address, REGISTERS_TO_READ);
            SendMessage(message);

            // check if there is response
            int bytesReceived = serialPort.BytesToRead;
            if (bytesReceived > 0)
            {
                // read message
                byte[] receivedMessage = new byte[bytesReceived];
                for (int i = 0; i < bytesReceived; i++)
                    receivedMessage[i] = (byte)serialPort.ReadByte();

                if (message[0] != receivedMessage[0])
                    throw new Exception("Received message from unexpected controller address!");

                if (receivedMessage[1] == FunctionCode.READ.GetHashCode())
                {
                    uint checksum = CalculateChecksum(receivedMessage, receivedMessage.Length - 2);
                    if (receivedMessage[bytesReceived - 1] != (checksum >> 8) || receivedMessage[bytesReceived - 2] != (checksum & 0xFF))
                        throw new Exception("Received wrong checksum!");

                    int bytesToRead = receivedMessage[2];
                    for (int i = 0; i < bytesToRead; i += 2)
                    {
                        controlSystem.devices[index].SetValue(receivedMessage[3 + i], receivedMessage[4 + i]);
                    }
                }
                else
                {
                    HandleError(receivedMessage[2]);
                }
            }
            else
            {
                throw new Exception("Empty response! Make sure you provided proper controller address.");
            }
        }

        public void WriteValue(int index, int value)
        {
            if (!IsInitialized)
                throw new Exception("Set transmission parameters first!");

            if (index >= controlSystem.devices.Count)
                throw new Exception("Unrecognized device!");

            int castedValue;
            if (controlSystem.devices[index].GetType() == typeof(AnalogOutput))
            {
                castedValue = (int)Math.Round(value / 100.0 * 255, MidpointRounding.AwayFromZero);
            }
            else if (controlSystem.devices[index].GetType() == typeof(DigitalOutput))
            {
                ReadValue(index);
                int currentValue = controlSystem.devices[index].value;
                int mask = 1 << ((DigitalOutput)controlSystem.devices[index]).bit;
                if (value == 0)
                    castedValue = currentValue & ~mask;
                else
                    castedValue = currentValue | mask;
            }
            else
            {
                throw new Exception("Cannot write value to non-output device!");
            }

            // build and send request
            byte[] message = new byte[8];
            BuildMessage(ref message, FunctionCode.WRITE.GetHashCode(),
                controlSystem.devices[index].address, castedValue);
            SendMessage(message);

            // check if there is response
            int bytesReceived = serialPort.BytesToRead;
            if (bytesReceived > 0)
            {
                // read message
                byte[] receivedMessage = new byte[bytesReceived];
                for (int i = 0; i < bytesReceived; i++)
                    receivedMessage[i] = (byte)serialPort.ReadByte();

                if (message[0] != receivedMessage[0])
                    throw new Exception("Received message from unexpected controller address!");

                if (receivedMessage[1] == FunctionCode.WRITE.GetHashCode())
                {
                    for (int i = 0; i < message.Length - 2; i++)
                    {
                        if (message[i] != receivedMessage[i])
                            throw new Exception("Unexpected response!");
                    }

                    uint checksum = CalculateChecksum(receivedMessage, receivedMessage.Length - 2);
                    if (receivedMessage[MESSAGE_LENGTH - 1] != (checksum >> 8) || receivedMessage[MESSAGE_LENGTH - 2] != (checksum & 0xFF))
                        throw new Exception("Received wrong checksum!");

                    controlSystem.devices[index].value = castedValue;
                }
                else
                {
                    HandleError(receivedMessage[2]);
                }
            }
            else
            {
                throw new Exception("Empty response! Make sure you provided proper controller address.");
            }
        }

        uint CalculateChecksum(byte[] message, int length)
        {
            uint crc16 = 0xffff;
            uint temp;
            uint flag;

            for (int i = 0; i < length; i++)
            {
                temp = message[i];
                temp &= 0x00ff;
                crc16 ^= temp;

                for (uint c = 0; c < 8; c++)
                {
                    flag = crc16 & 0x01;
                    crc16 >>= 1;
                    if (flag != 0)
                        crc16 ^= 0x0a001;
                }
            }

            return crc16;
        }

        private void BuildMessage(ref byte[] message, int request, int firstValue, int secondValue)
        {
            if (message.Length != 8)
                throw new Exception("BuildMessage - message doesn't contain 8 bytes!");

            message[0] = (byte)controllerAddress;              // modbus address
            message[1] = (byte)request;                        // request
            message[2] = (byte)((firstValue & 0xFF00) >> 8);   // first value - high byte
            message[3] = (byte)(firstValue & 0xFF);            // first value - low  byte
            message[4] = (byte)((secondValue & 0xFF00) >> 8);  // second value - high byte
            message[5] = (byte)(secondValue & 0xFF);           // second value - low byte
            message[6] = 0xd5;                                 // CRC
            message[7] = 0xac;                                 // CRC
        }

        private void SendMessage(byte[] message)
        {
            if (serialPort.IsOpen)
            {
                // clean read buffer
                int bytesToRead = serialPort.BytesToRead;
                if (bytesToRead > 0)
                {
                    for (int i = 0; i < bytesToRead; i++) serialPort.ReadByte();
                }

                try
                {
                    serialPort.Write(message, 0, message.Length);

                    int counter = 0;
                    while (serialPort.BytesToRead == 0)
                    {
                        counter++;
                        Thread.Sleep(1);
                        if (counter >= 500)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Cannot send request: " + ex.Message);
                }
            }
            else
            {
                throw new Exception("Cannot open port " + serialPort.PortName);
            }
        }

        private void HandleError(int errorCode)
        {
            switch (errorCode)
            {
                case 1:
                    throw new Exception("Received error: banned function code!");
                case 2:
                    throw new Exception("Received error: banned register address!");
                case 3:
                    throw new Exception("Received error: banned value!");
                case 4:
                    throw new Exception("Received error: device is broken!");
                case 6:
                    throw new Exception("Received error: controller not ready!");
                case 8:
                    throw new Exception("Received error: parity error!");
                default:
                    throw new Exception("Received error: unknown error code!");
            }
        }
    }
}
