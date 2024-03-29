﻿using System;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using OpenHardwareMonitor.Hardware;
using System.Collections.Generic;
using System.Management;
using System.Diagnostics;
using System.Linq;
using static EspMon.EspMon;

namespace EspMon
{
    public partial class EspMon : Form
	{
		public class UpdateVisitor : IVisitor
		{
			public void VisitComputer(IComputer computer)
			{
				computer.Traverse(this);
			}
			public void VisitHardware(IHardware hardware)
			{
				hardware.Update();
				foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
			}
			public void VisitSensor(ISensor sensor) { }
			public void VisitParameter(IParameter parameter) { }
		}
		float cpuUsage;
        float gpuUsage;
        float cpuTemp;
        float gpuTemp;
        float cpuSpeed;
        float gpuSpeed;

        private SerialPort _port;
        private readonly Computer _computer = new Computer
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
            PortCombo.SelectedIndex = 0;
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
            var updateVisitor = new UpdateVisitor();
			_computer.Accept(updateVisitor);
			for (int i = 0; i < _computer.Hardware.Length; i++)
			{
				if (_computer.Hardware[i].HardwareType == HardwareType.CPU)
				{
					for (int j = 0; j < _computer.Hardware[i].Sensors.Length; j++)
					{
                        var sensor = _computer.Hardware[i].Sensors[j];
						if (sensor.SensorType == SensorType.Temperature)
                        {
							cpuTemp = sensor.Value.GetValueOrDefault();
						}
						else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("CPU Total"))
						{
							// store
							cpuUsage = sensor.Value.GetValueOrDefault();
						}
						else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("CPU Core #1"))
						{
							// store
							cpuSpeed = sensor.Value.GetValueOrDefault();
						}
					}
				}
				if (_computer.Hardware[i].HardwareType == HardwareType.GpuAti || _computer.Hardware[i].HardwareType == HardwareType.GpuNvidia)
				{
					for (int j = 0; j < _computer.Hardware[i].Sensors.Length; j++)
					{
						var sensor = _computer.Hardware[i].Sensors[j];
						if (sensor.SensorType == SensorType.Temperature && sensor.Name.Contains("GPU Core"))
						{
							// store
							gpuTemp = sensor.Value.GetValueOrDefault();
						}
						else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("GPU Core"))
						{
							// store
							gpuUsage = sensor.Value.GetValueOrDefault();
						}
						else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("GPU Core"))
						{
							// store
							gpuSpeed = sensor.Value.GetValueOrDefault();
						}

					}
				}
			}
			/*foreach (var hardware in _computer.Hardware)
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

                        }
                        else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("CPU Total"))
                        {
                            // store
                            cpuUsage = sensor.Value.GetValueOrDefault();
                        }
                        else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("CPU Core #1"))
                        {
                            // store
                            cpuSpeed = sensor.Value.GetValueOrDefault();
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
                         }
                        else if (sensor.SensorType == SensorType.Load && sensor.Name.Contains("GPU Core"))
                        {
                            // store
                            gpuUsage = sensor.Value.GetValueOrDefault();
                        }
                        else if (sensor.SensorType == SensorType.Clock && sensor.Name.Contains("GPU Core"))
                        {
                            // store
                            gpuSpeed = sensor.Value.GetValueOrDefault();
                        }
                        
                }

                // ... you can access any other system information you want here

            }*/
        
     
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
                    } else if((char)cha[0]=='@')
					{
                        var ba = BitConverter.GetBytes(cpuSpeed);
                        
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(ba);
                        }
                        _port.Write(ba, 0, ba.Length);
                        ba = BitConverter.GetBytes(gpuSpeed);
                        if (!BitConverter.IsLittleEndian)
                        {
                            Array.Reverse(ba);
                        }
                        _port.Write(ba, 0, ba.Length);
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
