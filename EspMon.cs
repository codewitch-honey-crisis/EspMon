using System;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using OpenHardwareMonitor.Hardware;
namespace EspMon
{
	public partial class EspMon : Form
	{
        float cpuTemp;
        float cpuUsage;
        float gpuTemp;
        float gpuUsage;
        float cpuPowerDrawPackage;
        float cpuFrequency;
        float gpuCoreFrequency;
        float gpuMemoryFrequency;
        SerialPort _port;
        Computer _computer = new Computer()
		{
			CPUEnabled = true,
			GPUEnabled = true
		};
		public EspMon()
		{
			InitializeComponent();
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

                    // loop through the data
                    foreach (var sensor in hardware.Sensors)
                        if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("CPU Package"))
                        {
                            // store
                            cpuTemp = sensor.Value.GetValueOrDefault();
                            // print to console
                            //System.Diagnostics.Debug.WriteLine("cpuTemp: " + sensor.Value.GetValueOrDefault());

                        }
                        else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("CPU Total"))
                        {
                            // store
                            cpuUsage = sensor.Value.GetValueOrDefault();
                            // print to console
                            //System.Diagnostics.Debug.WriteLine("cpuUsage: " + sensor.Value.GetValueOrDefault());

                        }
                        else if (sensor.SensorType == SensorType.Power && sensor.Name.Contains("CPU Package"))
                        {
                            // store
                            cpuPowerDrawPackage = sensor.Value.GetValueOrDefault();
                            // print to console
                           // System.Diagnostics.Debug.WriteLine("CPU Power Draw - Package: " + sensor.Value.GetValueOrDefault());


                        }
                        else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("CPU Core #1"))
                        {
                            // store
                            cpuFrequency = sensor.Value.GetValueOrDefault();
                            // print to console
                           // System.Diagnostics.Debug.WriteLine("cpuFrequency: " + sensor.Value.GetValueOrDefault());
                        }
                }


                // Targets AMD & Nvidia GPUS
                if (hardware.HardwareType == HardwareType.GpuAti || hardware.HardwareType == HardwareType.GpuNvidia)
                {
                    // only fire the update when found
                    hardware.Update();

                    // loop through the data
                    foreach (var sensor in hardware.Sensors)
                        if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("GPU Core"))
                        {
                            // store
                            gpuTemp = sensor.Value.GetValueOrDefault();
                            // print to console
                            //System.Diagnostics.Debug.WriteLine("gpuTemp: " + sensor.Value.GetValueOrDefault());
                        }
                        else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("GPU Core"))
                        {
                            // store
                            gpuUsage = sensor.Value.GetValueOrDefault();
                            // print to console
                            //System.Diagnostics.Debug.WriteLine("gpuUsage: " + sensor.Value.GetValueOrDefault());
                        }
                        else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("GPU Core"))
                        {
                            // store
                            gpuCoreFrequency = sensor.Value.GetValueOrDefault();
                            // print to console
                           // System.Diagnostics.Debug.WriteLine("gpuCoreFrequency: " + sensor.Value.GetValueOrDefault());
                        }
                        else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("GPU Memory"))
                        {
                            // store
                            gpuMemoryFrequency = sensor.Value.GetValueOrDefault();
                            // print to console
                           // System.Diagnostics.Debug.WriteLine("gpuMemoryFrequency: " + sensor.Value.GetValueOrDefault());
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
                }
                else
                {
                    _port.Read(cha, 0, cha.Length);
                    if ((char)cha[0] == '#')
                    {
                        var ba = BitConverter.GetBytes(cpuUsage);
                        if(!BitConverter.IsLittleEndian)
						{
                            Array.Reverse(ba);
						}
                        _port.Write(ba, 0, ba.Length);
                        ba = BitConverter.GetBytes(cpuTemp);
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(ba);
                        }
                        _port.Write(ba, 0, ba.Length);
                        ba = BitConverter.GetBytes(gpuUsage);
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(ba);
                        }
                        _port.Write(ba, 0, ba.Length);
                        ba = BitConverter.GetBytes(gpuTemp);
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
	}
}
