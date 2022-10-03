using System;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using OpenHardwareMonitor.Hardware;
using System.Collections.Generic;

namespace EspMon
{
    public enum PartType
    {
        Processor,
        GraphicsCard,
        Memory
    }
    public enum MetricType
    {
        Temperature,
        UsagePercentage,
        PowerDrawWatts,
        FrequencyHz,
        VideoMemoryFrequencyHz
    }
    public class Container
    {
        public Dictionary<PartType, Dictionary<MetricType, float>> Data = new Dictionary<PartType, Dictionary<MetricType, float>>();

        public Container()
        {
            foreach (var part in (PartType[])Enum.GetValues(typeof(PartType)))
            {
                Data.Add(part, GetMetricsForPart(part));
            }
        }

        private static Dictionary<MetricType, float> GetMetricsForPart(PartType part)
        {
            var result = new Dictionary<MetricType, float>
            {
                { MetricType.Temperature, 0 },
                { MetricType.UsagePercentage, 0 },
                { MetricType.FrequencyHz, 0 }
            };

            switch (part)
            {
                case PartType.Processor:
                    result.Add(MetricType.PowerDrawWatts, 0);
                    break;
                case PartType.GraphicsCard:
                    result.Add(MetricType.PowerDrawWatts, 0);
                    result.Add(MetricType.VideoMemoryFrequencyHz, 0);
                    break;
            }

            return result;
        }
    }
    public partial class EspMon : Form
	{
        float cpuTemp;
        float cpuUsage;
        float gpuTemp;
        float gpuUsage;
        SerialPort _port;
        Container _container = new Container();
        Computer _computer = new Computer()
		{
			CPUEnabled = true,
			GPUEnabled = true
		};
		public EspMon()
		{
			InitializeComponent();
            Notify.Icon = System.Drawing.SystemIcons.Information;
            Show();
            RefreshPortList();
            _computer.Open();
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
            _computer.Close();
		}
		void RefreshPortList()
		{
			var p = PortCombo.Text;
			PortCombo.Items.Clear();
			var ports = SerialPort.GetPortNames();
			foreach(var port in ports)
			{
				PortCombo.Items.Add(port);
			}
			var idx = PortCombo.Items.Count-1;
			if(!string.IsNullOrWhiteSpace(p))
			{
				for(var i = 0; i < PortCombo.Items.Count; ++i)
				{
					if(p==(string)PortCombo.Items[i])
					{
						idx = i;
						break;
					}
				}
			}
            var s = new SerialPort((string)PortCombo.Items[idx]);
            if (!s.IsOpen)
            {
                try
                {
                    s.Open();
                    s.Close();
                }
                catch
                {
                    --idx;
                    if (0 > idx)
                    {
                        idx = PortCombo.Items.Count - 1;
                    }
                }
            }
			PortCombo.SelectedIndex = idx;
		}

		private void RefreshButton_Click(object sender, EventArgs e)
		{
			RefreshPortList();
		}

		private void UpdateTimer_Tick(object sender, EventArgs e)
		{
            CollectSystemInfo();
            
		}
        void CollectSystemInfo()
        {
            foreach (var hardware in _computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.CPU)
                {
                    // only fire the update when found
                    hardware.Update();

                    var metrics = _container.Data[PartType.Processor];
                    // loop through the data
                    foreach (var sensor in hardware.Sensors)
                    {
                        var value = sensor.Value.GetValueOrDefault();
                        switch (sensor.SensorType)
                        {
                            case SensorType.Temperature:
                                metrics[MetricType.Temperature] = value;
                                break;
                            case SensorType.Load:
                                metrics[MetricType.UsagePercentage] = value;
                                break;
                            case SensorType.Clock:
                                metrics[MetricType.FrequencyHz] = value;
                                break;
                            case SensorType.Power:
                                metrics[MetricType.PowerDrawWatts] = value;
                                break;
                        }
                    }
                }


                // Targets AMD & Nvidia GPUS
                if (hardware.HardwareType == HardwareType.GpuAti || hardware.HardwareType == HardwareType.GpuNvidia)
                {
                    // only fire the update when found
                    hardware.Update();

                    var metrics = _container.Data[PartType.GraphicsCard];
                    // loop through the data
                    foreach (var sensor in hardware.Sensors)
                    {
                        var value = sensor.Value.GetValueOrDefault();
                        switch (sensor.SensorType)
                        {
                            case SensorType.Temperature:
                                metrics[MetricType.Temperature] = value;
                                break;
                            case SensorType.Load:
                                metrics[MetricType.UsagePercentage] = value;
                                break;
                            case SensorType.Clock:
                                if (sensor.Name.Contains("Core"))
                                {
                                    metrics[MetricType.FrequencyHz] = value;
                                }
                                else if (sensor.Name.Contains("Memory"))
                                {
                                    metrics[MetricType.VideoMemoryFrequencyHz] = value;
                                }
                                break;
                            case SensorType.Power:
                                metrics[MetricType.PowerDrawWatts] = value;
                                break;
                        }
                    }
                }

                // ... you can access any other system information you want here
            }
        }

        private void PortCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
            if(_port!=null && _port.IsOpen)
			{
                _port.Close();
			}
            _port = new SerialPort(((ComboBox)sender).Text,115200);
            _port.Encoding = Encoding.ASCII;
            _port.Open();
			_port.DataReceived += _port_DataReceived;
            
		}

		private void _port_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
            if (_port!=null && _port.IsOpen)
            {
                var cha = new byte[1];
                if (_port.BytesToRead != 1)
                {
                    var ba = new byte[_port.BytesToRead];
                    _port.Read(ba, 0, ba.Length);
                    if (Created && !Disposing)
                    {
                        Invoke(new Action(() =>
                        {
                            Log.AppendText(Encoding.ASCII.GetString(ba));
                        }));
                    }
                }
                else
                {
                    _port.Read(cha, 0, cha.Length);
                    if ((char)cha[0] == '#')
                    {
                        var ba = BitConverter.GetBytes(_container.Data[PartType.Processor][MetricType.UsagePercentage]);
                        if(!BitConverter.IsLittleEndian)
						{
                            Array.Reverse(ba);
						}
                        _port.Write(ba, 0, ba.Length);
                        ba = BitConverter.GetBytes(_container.Data[PartType.Processor][MetricType.Temperature]);
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(ba);
                        }
                        _port.Write(ba, 0, ba.Length);
                        ba = BitConverter.GetBytes(_container.Data[PartType.GraphicsCard][MetricType.UsagePercentage]);
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(ba);
                        }
                        _port.Write(ba, 0, ba.Length);
                        ba = BitConverter.GetBytes(_container.Data[PartType.GraphicsCard][MetricType.Temperature]);
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(ba);
                        }
                        _port.Write(ba, 0, ba.Length);
                        _port.BaseStream.Flush();
                    }
                }
            }
        }

		private void EspMon_Resize(object sender, EventArgs e)
		{
            if(WindowState==FormWindowState.Minimized)
			{
                Hide();
                Notify.Visible = true;
			}
		}

		private void Notify_Click(object sender, EventArgs e)
		{
            Show();
            Size = MinimumSize;
            WindowState = FormWindowState.Normal;
            Activate();
        }
    }
}
