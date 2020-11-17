using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScadaProject
{
    public partial class ScadaUi : Window
    {
        private Scada scadaSystem = new Scada();
        private BackgroundWorker workerReadAll = new BackgroundWorker();
        private Progress progress;

        private bool inProgress = false;

        public ScadaUi()
        {
            InitializeComponent();
            InitializeBackgroundWorker();

            ReadSerialPorts();

            Closing += ScadaUiClosing;
        }

        private void ScadaUiClosing(object sender, CancelEventArgs e)
        {
            scadaSystem.CloseSerialPort();
        }

        private void ReadSerialPorts()
        {
            try
            {
                portNameBox.Items.Clear();
                foreach (string s in SerialPort.GetPortNames())
                {
                    portNameBox.Items.Add(s);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot read available serial ports! " + ex.Message);
            }
        }

        private void InitializeBackgroundWorker()
        {
            workerReadAll.DoWork += WorkerReadAllDoWork;
            workerReadAll.RunWorkerCompleted += WorkerReadAllCompleted;
            workerReadAll.ProgressChanged += WorkerReadAllProgressChanged;
            workerReadAll.WorkerReportsProgress = true;
        }

        private void IsEnterPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Connect(sender, e);
        }

        private void Connect(object sender, RoutedEventArgs e)
        {
            if (inProgress) return;

            try
            {
                string portName = portNameBox.Text;
                int baudRate = Convert.ToInt32(baudRateBox.Text);
                int controllerAddress = Convert.ToInt32(controllerAddrBox.Text);

                scadaSystem.OpenSerialPort(portName, baudRate, controllerAddress);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Connecting error");
            }
        }

        private void Disconnect(object sender, RoutedEventArgs e)
        {
            if (inProgress) return;

            try
            {
                scadaSystem.CloseSerialPort();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Disconnecting error");
            }
        }

        private void ReadAllValues(object sender, RoutedEventArgs e)
        {
            if (inProgress)
                return;
            if (!scadaSystem.IsInitialized)
            { 
                NonInitializedError();
                return;
            }

            if (!workerReadAll.IsBusy)
            {
                workerReadAll.RunWorkerAsync();
                progress = new Progress();
                progress.Show();
                inProgress = true;
            }
        }

        private void ReadValue(object sender, RoutedEventArgs e)
        {
            if (inProgress)
                return;
            if (!scadaSystem.IsInitialized)
            {
                NonInitializedError();
                return;
            }

            try
            {
                ReadValueWindow rvWindow = new ReadValueWindow();
                rvWindow.ShowDialog();

                int index = rvWindow.GetIndex();
                if (index != -1)
                {
                    scadaSystem.ReadValue(index);
                    UpdateAllValues();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot read! " + ex.Message);
            }
        }

        private void WriteValue(object sender, RoutedEventArgs e)
        {
            if (inProgress)
                return;
            if (!scadaSystem.IsInitialized)
            {
                NonInitializedError();
                return;
            }

            try
            {
                WriteValueWindow wvWindow = new WriteValueWindow();
                wvWindow.ShowDialog();

                int index = wvWindow.GetIndex();
                if (index != -1)
                {
                    scadaSystem.WriteValue(index, wvWindow.GetValue());
                    UpdateAllValues();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot write! " + ex.Message);
            }
        }

        private void ShowDeviceList(object sender, RoutedEventArgs e)
        {
            if (inProgress) return;

            DeviceListHelp dlHelp = new DeviceListHelp();
            dlHelp.ShowDialog();

        }

        private void WorkerReadAllDoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                int deviceCount = scadaSystem.GetDeviceCount();
                for (int i = 0; i < deviceCount; i++)
                {
                    scadaSystem.ReadValue(i);
                    workerReadAll.ReportProgress(i * 100 / deviceCount);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception");
            }
        }

        private void WorkerReadAllProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progress.progressBar.Value = e.ProgressPercentage;
        }

        private void WorkerReadAllCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progress.Close();
            UpdateAllValues();
            inProgress = false;
        }

        private void UpdateAllValues()
        {
            try
            {
                ChildControls childControls = new ChildControls();
                foreach (object obj in childControls.GetChildren(deviceList, 5))
                {
                    if (obj.GetType() == typeof(TextBlock))
                    {
                        TextBlock textBlock = (TextBlock)obj;
                        if (textBlock.Name.Contains("deviceTextBlock"))
                        {
                            int index = Convert.ToInt32(textBlock.Name.Split('_')[1]) - 1;
                            textBlock.Text = scadaSystem.GetValue(index);
                        }
                    }
                }

                RaiseThermostatError(deviceTextBlock_4.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception");
            }
        }

        private void RaiseThermostatError(string value)
        {
            // if antifreeze thermostat is open - raise an alarm
            if (value == "ON")
            {
                thermostatBorder.Background = new SolidColorBrush(Colors.Red);
                MessageBox.Show("Error! Antifreeze thermostat is open!");
            }
            else
            {
                thermostatBorder.Background = new SolidColorBrush(Colors.White);
            }
        }

        private void NonInitializedError()
        {
            MessageBox.Show(
                "Connection isn't initialized! Provide transmission parameters and use CONNECT button!",
                "Error");
        }
    }
}
