using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ScadaProject
{
    public partial class ScadaUi : Window
    {
        private Scada scadaSystem = new Scada();
        private Queue<Task> tasks = new Queue<Task>();

        private BackgroundWorker worker = new BackgroundWorker();
        private DispatcherTimer readTimer = new DispatcherTimer();
        private DispatcherTimer actionTimer = new DispatcherTimer();

        public ScadaUi()
        {
            InitializeComponent();
            InitializeBackgroundWorker();
            InitializeTimers();

            ReadSerialPorts();

            Closing += ScadaUiClosing;
        }

        private void InitializeBackgroundWorker()
        {
            worker.DoWork += WorkerDoWork;
            worker.RunWorkerCompleted += WorkerCompleted;
            worker.WorkerSupportsCancellation = true;
        }

        private void InitializeTimers()
        {
            readTimer.Tick += new EventHandler(ReadTimerTick);
            readTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            readTimer.Start();

            actionTimer.Tick += new EventHandler(ActionTimerTick);
            actionTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            actionTimer.Start();
        }

        private void ActionTimerTick(object sender, EventArgs e)
        {
            if (!scadaSystem.IsInitialized) return;

            if (!worker.IsBusy)
                worker.RunWorkerAsync();
        }

        private void ReadTimerTick(object sender, EventArgs e)
        {
            if (tasks.Count > 0) return;
            if (!scadaSystem.IsInitialized) return;

            tasks.Enqueue(new Task(TaskType.Read, 0));
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

        private void ScadaUiClosing(object sender, CancelEventArgs e)
        {
            scadaSystem.CloseSerialPort();
        }

        private void IsEnterPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                Connect(sender, e);
        }

        private void Connect(object sender, RoutedEventArgs e)
        {
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
            try
            {
                scadaSystem.CloseSerialPort();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Disconnecting error");
            }
        }

        private void WriteValue(object sender, RoutedEventArgs e)
        {
            if (!scadaSystem.IsInitialized)
            {
                NonInitializedError();
                return;
            }

            try
            {
                tasks.Clear();
                if (worker.IsBusy)
                    worker.CancelAsync();

                WriteValueWindow wvWindow = new WriteValueWindow();
                wvWindow.ShowDialog();

                int index = wvWindow.GetIndex();
                if (index != -1)
                    tasks.Enqueue(new Task(TaskType.Write, index, wvWindow.GetValue()));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot write! " + ex.Message);
            }
        }

        private void ShowDeviceList(object sender, RoutedEventArgs e)
        {
            DeviceListHelp dlHelp = new DeviceListHelp();
            dlHelp.ShowDialog();
        }

        private void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            if (tasks.Count == 0) return;

            try
            {
                Task task = tasks.Dequeue();
                if (task.type == TaskType.Read)
                {
                    scadaSystem.ReadAllValues();
                }
                else if (task.type == TaskType.Write)
                {
                    scadaSystem.WriteValue(task.index, task.value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception");
            }
        }

        private void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lock (this)
            {
                UpdateAllValues();
            }
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
                if (((SolidColorBrush)thermostatBorder.Background).Color == Colors.Red)
                    return;

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
