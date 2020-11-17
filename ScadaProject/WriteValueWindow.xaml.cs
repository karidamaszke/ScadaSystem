using System;
using System.Windows;

namespace ScadaProject
{
    public partial class WriteValueWindow : Window
    {
        private int index = -1;
        private int value = 0;

        public WriteValueWindow()
        {
            InitializeComponent();
        }

        public int GetIndex()
        {
            return index;
        }

        public int GetValue()
        {
            return value;
        }

        private void Write(object sender, RoutedEventArgs e)
        {
            index = deviceComboBox.SelectedIndex + 6; // first 6 devices are inputs
            if(!ReadValue())
                return;

            Close();
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            index = -1;
            Close();
        }

        private bool ReadValue()
        {
            try
            {
                string strValue = valueToWrite.Text;
                if (index < 11)
                {
                    // analog outputs have indexes from 6 to 11
                    strValue = strValue.Replace("%", "");
                    value = Convert.ToInt32(strValue);

                    if (value < 0 || value > 100)
                    {
                        index = -1;
                        MessageBox.Show("Value for analog output should be a percentage value (0% - 100%)!", "Wrong value");
                        return false;
                    }
                }
                else
                {
                    // digital outputs have indexes from 12 to 17
                    strValue = strValue.ToLower();
                    if (strValue == "on")
                    {
                        value = 1;
                    }
                    else if (strValue == "off")
                    {
                        value = 0;
                    }
                    else
                    {
                        index = -1;
                        MessageBox.Show("Value for digital output should be ON or OFF", "Wrong value");
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                index = -1;
                MessageBox.Show(
                  "Cannot parse value! Use percentage value for analog outputs (0% - 100%) or ON/OFF value for digital outputs!");
                return false;
            }
        }
    }
}
