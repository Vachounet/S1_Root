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

namespace S1_Root
{
    public partial class Form1 : Form
    {
        public const string SystemImageFound = "/storage/sdcard0/system510.img\r\r\n";

        AndroidController l_android;
        Device l_device;
        bool l_haveDriver;
        bool l_alreadyFlash;
        long l_seekBlocks;
        long l_countBlocks;

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


                    DeviceInfo l_devInfo = new DeviceInfo(l_device.BuildProp);

                    this.richTextBox1.Text = l_devInfo.Manufacturer + " " + l_devInfo.Model + " (" + l_devInfo.SKU + ")\r\n";
                    this.richTextBox1.Text += l_devInfo.DisplayID + "\r\n";
                    this.richTextBox1.Text += l_devInfo.CPU + " @ " + l_devInfo.CPUSpeed + "\r\n";

                    //Got root ?
                    if (l_device.HasRoot && !l_alreadyFlash)
                    {
                        this.checkBox1.Text += l_device.Su.Version;

                        if (l_device.BusyBox.IsInstalled)
                        {
                            this.checkBox2.Checked = true;
                        }
                        DialogResult l_haveRootResult = MessageBox.Show(@"Your device is already rooted, no need to flash it again." + Environment.NewLine + @"Do you still want to flash a new system510.img ?", @"Device Rooted", MessageBoxButtons.YesNo);
                        if (l_haveRootResult == DialogResult.Yes)
                        {
                            this.richTextBox1.Text += string.Format("Device detected with ID : {0}", serial);
                            l_alreadyFlash = true;
                            this.button1.Enabled = true;
                        }
                        else
                        {
                            this.richTextBox1.Text += string.Format("Device already rooted. (ID : {0})", serial);
                            this.button1.Enabled = false;
                        }
                    }
                    else
                    {
                        this.button1.Enabled = true;
                    }

                    this.checkBox1.Checked = l_device.HasRoot;
                    return true;
                }
                else if (l_android.ConnectedDevices.Count > 1)
                {
                    this.richTextBox1.Text += @"Error - Too many connected devices";
                }
            }
            else
            {
                MessageBox.Show(@"0 device detected. Please connect your device, set USB Debug on your phone, and/or check ADB drivers");
                this.richTextBox1.Text += @"Error - No Devices Connected";

            }

            return false;
        }

        /// <summary>
        /// Generate blocks size for dd command
        /// </summary>
        /// <param name="startAddr"></param>
        /// <param name="endAddr"></param>
        private void GetPartitionsInfos(out long startAddr, out long endAddr)
        {
            this.richTextBox1.Text += @"Getting system partition infos...
";
            AdbCommand l_adbCommand;
            l_adbCommand = Adb.FormAdbCommand("shell cat /proc/dumchar_info");
            var procInfos = Adb.ExecuteAdbCommand(l_adbCommand);
            var lines = procInfos.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
            var androidSec = string.Empty;
            foreach (var line in lines.Where(line => line.StartsWith("android")))
            {
                androidSec = line;
                break;
            }
            if (string.IsNullOrEmpty(androidSec))
            {
                this.richTextBox1.Text += @"Unable to get system partition infos.
";
                startAddr = 0;
                endAddr = 0;
                return;
            }
            var addrs = androidSec.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
            var startA = addrs[1];
            var endA = addrs[2];
            this.richTextBox1.Text += string.Format(@"System found at {0} - {1}
", startA, endA);

            try
            {
                startAddr = Convert.ToInt64(startA, 16); ;
                endAddr = Convert.ToInt64(endA, 16);
            }
            catch (Exception ex)
            {
                this.richTextBox1.Text += @"Unable to parse system partition infos.
";
                startAddr = 0;
                endAddr = 0;
            }
        }

        /// <summary>
        /// Launch flash process
        /// </summary>
        public void Process_Flashing()
        {
            this.richTextBox1.Text += @"Launching process...
";
            long systemStartAddr;
            long systemEndAddr;
            GetPartitionsInfos(out systemStartAddr, out systemEndAddr);
            if (systemEndAddr == 0 || systemStartAddr == 0)
                return;

            l_countBlocks = systemStartAddr / 4096;
            l_seekBlocks = systemEndAddr / 4096;

            this.richTextBox1.Text += string.Format(@"DD will use @{0} - @{1} => {2} - {3}
", systemStartAddr, systemEndAddr, l_countBlocks, l_seekBlocks);

            //return;

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
                this.richTextBox1.Text += @"system510.img can't be found in your internal sdcard !";
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
            string l_ddCommand = string.Format("/data/local/tmp/busybox cat /storage/sdcard0/system510.img | dd of=/dev/block/mmcblk0 bs=4096 seek={0} count={1}", l_seekBlocks, l_countBlocks);

            if (!l_alreadyFlash)
                Adb.ExecuteAdbShellCommandInputString(null, "/data/local/tmp/busybox telnet 127.0.0.1 1234", l_ddCommand, "exit", "exit", "exit");
            //Adb.ExecuteAdbShellCommandInputString(device, "/data/local/tmp/busybox telnet 127.0.0.1 1234", "/data/local/tmp/busybox touch /data/local/tmp/test2", "exit", "exit", "exit");
            else
            {
                Adb.ExecuteAdbShellCommandInputString(null, "su", l_ddCommand, "exit", "exit");
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

    public class DeviceInfo
    {
        public string Manufacturer;
        public string Model;
        public string SKU;
        public string AndroidVersion;
        public string DisplayID;
        public string CPU;
        public string CPUSpeed;

        public DeviceInfo(BuildProp p_buildProp)
        {
            Manufacturer = !string.IsNullOrEmpty(p_buildProp.GetProp("ro.product.manufacturer")) ? p_buildProp.GetProp("ro.product.manufacturer") : string.Empty;
            Model = !string.IsNullOrEmpty(p_buildProp.GetProp("ro.product.model")) ? p_buildProp.GetProp("ro.product.model") : string.Empty;
            SKU = !string.IsNullOrEmpty(p_buildProp.GetProp("ro.build.sku")) ? p_buildProp.GetProp("ro.build.sku") : string.Empty;
            AndroidVersion = !string.IsNullOrEmpty(p_buildProp.GetProp("ro.build.version.release")) ? p_buildProp.GetProp("ro.build.version.release") : string.Empty;
            DisplayID = !string.IsNullOrEmpty(p_buildProp.GetProp("ro.build.display.id")) ? p_buildProp.GetProp("ro.build.display.id") : string.Empty;
            CPU = !string.IsNullOrEmpty(p_buildProp.GetProp("ro.cpu.version")) ? p_buildProp.GetProp("ro.cpu.version") : string.Empty;
            CPUSpeed = !string.IsNullOrEmpty(p_buildProp.GetProp("ro.cpu.speed")) ? p_buildProp.GetProp("ro.cpu.speed") : string.Empty;
        }
    }
}
