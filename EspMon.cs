﻿using System;
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
        SerialPort _port;
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
                    hardware.Update();
                    foreach (var sensor in hardware.Sensors)
                        if (sensor.SensorType == SensorType.Temperature &&
                            sensor.Name.Contains("CPU Package"))
                        {
                            cpuTemp = sensor.Value.GetValueOrDefault();
                        }
                        else if (sensor.SensorType == SensorType.Load && 
                            sensor.Name.Contains("CPU Total"))
                        {
                            cpuUsage = sensor.Value.GetValueOrDefault();
                        }
                }

                // Targets AMD & Nvidia GPUS
                if (hardware.HardwareType == HardwareType.GpuAti ||
                    hardware.HardwareType == HardwareType.GpuNvidia)
                {
                    hardware.Update();
                    foreach (var sensor in hardware.Sensors)
                        if (sensor.SensorType == SensorType.Temperature && 
                            sensor.Name.Contains("GPU Core"))
                        {
                            gpuTemp = sensor.Value.GetValueOrDefault();
                        }
                        else if (sensor.SensorType == SensorType.Load && 
                            sensor.Name.Contains("GPU Core"))
                        {
                            gpuUsage = sensor.Value.GetValueOrDefault();
                        }
                }
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
