﻿
namespace EspMon
{
	partial class EspMon
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.PortCombo = new System.Windows.Forms.ComboBox();
			this.RefreshButton = new System.Windows.Forms.Button();
			this.UpdateTimer = new System.Windows.Forms.Timer(this.components);
			this.SuspendLayout();
			// 
			// PortCombo
			// 
			this.PortCombo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.PortCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.PortCombo.FormattingEnabled = true;
			this.PortCombo.Location = new System.Drawing.Point(12, 12);
			this.PortCombo.Name = "PortCombo";
			this.PortCombo.Size = new System.Drawing.Size(214, 28);
			this.PortCombo.TabIndex = 0;
			this.PortCombo.SelectedIndexChanged += new System.EventHandler(this.PortCombo_SelectedIndexChanged);
			// 
			// RefreshButton
			// 
			this.RefreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.RefreshButton.Location = new System.Drawing.Point(232, 7);
			this.RefreshButton.Name = "RefreshButton";
			this.RefreshButton.Size = new System.Drawing.Size(97, 37);
			this.RefreshButton.TabIndex = 2;
			this.RefreshButton.Text = "Refresh";
			this.RefreshButton.UseVisualStyleBackColor = true;
			this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
			// 
			// UpdateTimer
			// 
			this.UpdateTimer.Enabled = true;
			this.UpdateTimer.Tick += new System.EventHandler(this.UpdateTimer_Tick);
			// 
			// EspMon
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(341, 61);
			this.Controls.Add(this.RefreshButton);
			this.Controls.Add(this.PortCombo);
			this.MaximizeBox = false;
			this.Name = "EspMon";
			this.Text = "EspMon";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ComboBox PortCombo;
		private System.Windows.Forms.Button RefreshButton;
		private System.Windows.Forms.Timer UpdateTimer;
	}
}

