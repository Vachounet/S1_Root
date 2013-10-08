using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RegawMOD.Android;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Management;
using Microsoft.Win32;
using System.Windows.Forms;

namespace S1_Root
{
    public partial class Form1 : Form
    {
        public const string SystemImageFound = "/storage/sdcard0/system510.img\r\r\n";

        AndroidController l_android;
        Device l_device;
        bool l_haveDriver;
        bool l_alreadyFlash;

        public Form1()
        {
            InitializeComponent();
            InitRoot();
            l_haveDriver = CheckDrivers();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Launch flash process in an other thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartRootClick(object sender, EventArgs e)
        {
            //Refresh device
            bool l_deviceFound = InitRoot();

            if (!l_deviceFound)
                return;

            //TODO : Set a correct message
            if (!l_haveDriver)
                return;

            Thread l_thrd = new Thread(new ThreadStart(Process_Flashing));
            //Dirty
            CheckForIllegalCrossThreadCalls = false;
            //Dirty
            l_thrd.Start();
            l_thrd.IsBackground = true;
        }

        /// <summary>
        /// Set connected device
        /// </summary>
        public bool InitRoot()
        {
            //Init our Controller instance
            if (l_android == null)
                l_android = AndroidController.Instance;

            l_android.UpdateDeviceList();

            if (l_android.HasConnectedDevices &&
                l_android.ConnectedDevices != null)
            {
                if (l_android.ConnectedDevices.Count == 1)
                {
                    //Set current device
                    string serial = l_android.ConnectedDevices[0];
                    l_device = l_android.GetConnectedDevice(serial);

                    //Got root ?
                    if (l_device.HasRoot)
                    {
                        DialogResult l_haveRootResult = MessageBox.Show(@"Your device is already rooted, no need to flash it again." + Environment.NewLine + "Do you still want to flash a new system510.img ?", "Device Rooted", MessageBoxButtons.YesNo);
                        if (l_haveRootResult == DialogResult.Yes)
                        {
                            l_alreadyFlash = true;
                            this.button1.Enabled = true;
                        }
                        else
                        {
                            this.richTextBox1.Text = @"Device already rooted.";
                            this.button1.Enabled = false;
                        }
                    }

                    this.checkBox1.Checked = l_device.HasRoot;
                    return true;
                }
                else if (l_android.ConnectedDevices.Count > 1)
                {
                    this.richTextBox1.Text = @"Error - Too many connected devices";
                }
            }
            else
            {
                this.richTextBox1.Text = @"Error - No Devices Connected";

            }
            return false;
        }

        /// <summary>
        /// Launch flash process
        /// </summary>
        public void Process_Flashing()
        {
            //TODO : Programatically check /proc/dumchar_info android partition info
            //android      0x0000000040000000   0x0000000005d00000   2   /dev/block/mmcblk0p5
            //and generate dynamic count and seek blocks for dd command

            this.richTextBox1.Clear();

            this.richTextBox1.Text = @"Device detected, launching process...
";

            string l_busyboxPath = AppDomain.CurrentDomain.BaseDirectory + "busybox";

            AdbCommand l_adbCommand;

            this.richTextBox1.Text += @"Pushing busybox...
";

            l_adbCommand = Adb.FormAdbCommand("push \"" + l_busyboxPath + "\" /data/local/tmp");
            this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);


            this.richTextBox1.Text += @"Setting perms to busybox...
";

            l_adbCommand = Adb.FormAdbCommand("shell chmod 755 /data/local/tmp/busybox");
            this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);

            l_adbCommand = Adb.FormAdbCommand("shell ls /storage/sdcard0/system510.img");
            if (Adb.ExecuteAdbCommand(l_adbCommand) != SystemImageFound)
            {
                this.richTextBox1.Text += "system510.img can't be found your internal sdcard !";
                return;
            }


            #region ADB Commands
            if (!l_alreadyFlash)
            {
                this.richTextBox1.Text += "Launching telnetd...\r\n";
                l_adbCommand = Adb.FormAdbCommand("shell input keyevent KEYCODE_HOME");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);


                l_adbCommand = Adb.FormAdbCommand("shell input keyevent KEYCODE_HOME");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell am start -n com.mediatek.engineermode/.EngineerMode com.mediatek.connectivity/.CdsInfoActivity");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand) + "\r\n";



                l_adbCommand = Adb.FormAdbCommand("shell input keyevent KEYCODE_DPAD_DOWN");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input keyevent KEYCODE_DPAD_DOWN");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input keyevent KEYCODE_DPAD_DOWN");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input keyevent KEYCODE_DPAD_DOWN");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input keyevent KEYCODE_ENTER");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input text \"/data/local/tmp/busybox\"");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input keyevent KEYCODE_SPACE");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input text \"telnetd\"");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input keyevent KEYCODE_SPACE");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input text \"-l\"");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input keyevent KEYCODE_SPACE");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input text \"/system/bin/sh\"");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input keyevent KEYCODE_SPACE");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input text \"-p\"");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);



                l_adbCommand = Adb.FormAdbCommand("shell input keyevent KEYCODE_SPACE");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);


                l_adbCommand = Adb.FormAdbCommand("shell input text \"1234\"");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);

                l_adbCommand = Adb.FormAdbCommand("shell input tap 50 300");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);

                this.richTextBox1.Text += "Telnetd running...\r\n";
            }
            #endregion

            Thread.Sleep(3000);

            this.richTextBox1.Text += @"Flashing modded system.img (if it freezes more than 10min, kill this app...)
";
            if (!l_alreadyFlash)
                Adb.ExecuteAdbShellCommandInputString(null, "/data/local/tmp/busybox telnet 127.0.0.1 1234", "/data/local/tmp/busybox cat /storage/sdcard0/system510.img | dd of=/dev/block/mmcblk0 bs=4096 seek=23808 count=262144", "exit", "exit", "exit");
            //Adb.ExecuteAdbShellCommandInputString(device, "/data/local/tmp/busybox telnet 127.0.0.1 1234", "/data/local/tmp/busybox touch /data/local/tmp/test2", "exit", "exit", "exit");
            else
            {
                l_adbCommand = Adb.FormAdbCommand("shell su -c \"/data/local/tmp/busybox cat /storage/sdcard0/system510.img | dd of=/dev/block/mmcblk0 bs=4096 seek=23808 count=262144\"");
                this.richTextBox1.Text += Adb.ExecuteAdbCommand(l_adbCommand);
            }


            this.richTextBox1.Text += @"Process finished. Reboot now !";
        }

        /// <summary>
        /// Check that ADB drivers are installed
        /// </summary>
        /// <returns>boolean</returns>
        public bool CheckDrivers()
        {
            //TODO : Check drivers and prompt install message if not present
            return true;
        }

    }
}
